using Microsoft.Extensions.Logging;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Analytics;

/// <summary>
/// Records and reports on chat activity. Every report is a plain GROUP BY over the one event table, which
/// is why the table is written wide (product id, keyword and intent all denormalised onto each row) and
/// never joined on the hot path.
/// </summary>
public partial class ChatAnalyticsService : IChatAnalyticsService
{
    #region Fields

    private readonly ILogger<ChatAnalyticsService> _logger;
    private readonly IRepository<ChatEvent> _eventRepository;
    private readonly IRepository<ChatSession> _sessionRepository;
    private readonly IRepository<Product> _productRepository;

    #endregion

    #region Ctor

    public ChatAnalyticsService(ILogger<ChatAnalyticsService> logger,
        IRepository<ChatEvent> eventRepository,
        IRepository<ChatSession> sessionRepository,
        IRepository<Product> productRepository)
    {
        _logger = logger;
        _eventRepository = eventRepository;
        _sessionRepository = sessionRepository;
        _productRepository = productRepository;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Restricts a query to a store and a date window.
    /// </summary>
    protected virtual IQueryable<ChatEvent> FilterEvents(ChatEventType eventType, DateTime? fromUtc, DateTime? toUtc, int storeId)
    {
        var query = _eventRepository.Table.Where(e => e.EventTypeId == (int)eventType);

        if (storeId > 0)
            query = query.Where(e => e.StoreId == storeId);

        if (fromUtc.HasValue)
            query = query.Where(e => e.CreatedOnUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            query = query.Where(e => e.CreatedOnUtc <= toUtc.Value);

        return query;
    }

    /// <summary>
    /// Groups an event stream by its key and returns the most frequent.
    /// </summary>
    protected virtual async Task<IList<ChatTermStat>> GetTopTermsAsync(ChatEventType eventType,
        DateTime? fromUtc, DateTime? toUtc, int storeId, int count)
    {
        return await FilterEvents(eventType, fromUtc, toUtc, storeId)
            .Where(e => e.EventKey != null && e.EventKey != string.Empty)
            .GroupBy(e => e.EventKey)
            .Select(group => new ChatTermStat { Term = group.Key, Count = group.Count() })
            .OrderByDescending(stat => stat.Count)
            .Take(count)
            .ToListAsync();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Records facts about one answered question
    /// </summary>
    public virtual async Task TrackAsync(IList<ChatEvent> events)
    {
        if (events is null || events.Count == 0)
            return;

        try
        {
            await _eventRepository.InsertAsync(events, publishEvent: false);
        }
        catch (Exception exception)
        {
            //analytics are a side effect of answering, not part of it. A shopper must still get their reply
            //if the reporting table is locked, full, or otherwise unhappy.
            _logger.LogError(exception, "Noor AI assistant: failed to record {Count} analytics events", events.Count);
        }
    }

    /// <summary>
    /// Gets the questions shoppers ask most
    /// </summary>
    public virtual async Task<IList<ChatTermStat>> GetTopQuestionsAsync(DateTime? fromUtc, DateTime? toUtc, int storeId, int count = 20)
    {
        return await GetTopTermsAsync(ChatEventType.QuestionAsked, fromUtc, toUtc, storeId, count);
    }

    /// <summary>
    /// Gets the keywords shoppers search most
    /// </summary>
    public virtual async Task<IList<ChatTermStat>> GetTopKeywordsAsync(DateTime? fromUtc, DateTime? toUtc, int storeId, int count = 20)
    {
        return await GetTopTermsAsync(ChatEventType.KeywordSearched, fromUtc, toUtc, storeId, count);
    }

    /// <summary>
    /// Gets the products the assistant surfaced most
    /// </summary>
    public virtual async Task<IList<ChatProductStat>> GetTopProductsAsync(ChatEventType eventType,
        DateTime? fromUtc, DateTime? toUtc, int storeId, int count = 20)
    {
        //join to Product for the name, so a deleted product drops out of the report rather than showing as
        //a bare id
        var stats = await (from e in FilterEvents(eventType, fromUtc, toUtc, storeId)
                           where e.ProductId > 0
                           join product in _productRepository.Table on e.ProductId equals product.Id
                           where !product.Deleted
                           group product by new { product.Id, product.Name } into grouped
                           select new ChatProductStat
                           {
                               ProductId = grouped.Key.Id,
                               ProductName = grouped.Key.Name,
                               Count = grouped.Count()
                           })
            .OrderByDescending(stat => stat.Count)
            .Take(count)
            .ToListAsync();

        return stats;
    }

    /// <summary>
    /// Gets the headline numbers
    /// </summary>
    public virtual async Task<ChatAnalyticsSummary> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, int storeId)
    {
        var sessions = _sessionRepository.Table.Where(session => !session.Deleted);

        if (storeId > 0)
            sessions = sessions.Where(session => session.StoreId == storeId);

        if (fromUtc.HasValue)
            sessions = sessions.Where(session => session.CreatedOnUtc >= fromUtc.Value);

        if (toUtc.HasValue)
            sessions = sessions.Where(session => session.CreatedOnUtc <= toUtc.Value);

        var conversationCount = await sessions.CountAsync();

        //SUM over an empty set is NULL in SQL, which will not materialise into an int; go through int? and
        //default it here rather than letting the query blow up on a store with no conversations yet
        var messageCount = conversationCount == 0
            ? 0
            : await sessions.SumAsync(session => (int?)session.MessageCount) ?? 0;

        var questions = FilterEvents(ChatEventType.QuestionAsked, fromUtc, toUtc, storeId);

        var answeredCount = await questions.CountAsync();
        var averageResponseTime = answeredCount == 0
            ? 0
            : await questions.AverageAsync(e => (double?)e.ResponseTimeMs) ?? 0;

        return new ChatAnalyticsSummary
        {
            TotalConversations = conversationCount,
            TotalMessages = messageCount,
            AverageConversationLength = conversationCount == 0
                ? 0
                : Math.Round((double)messageCount / conversationCount, 1),
            AverageResponseTimeMs = Math.Round(averageResponseTime, 0),
            TotalProductsRecommended = await FilterEvents(ChatEventType.ProductRecommended, fromUtc, toUtc, storeId).CountAsync(),
            TotalFaqMatches = await FilterEvents(ChatEventType.FaqMatched, fromUtc, toUtc, storeId).CountAsync(),
            UnansweredQuestions = await questions.Where(e => e.IntentId == (int)ChatIntent.Unknown).CountAsync()
        };
    }

    #endregion
}
