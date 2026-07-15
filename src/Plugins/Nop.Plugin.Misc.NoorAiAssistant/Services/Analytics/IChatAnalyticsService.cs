using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Analytics;

/// <summary>
/// Records what shoppers ask and reports on it.
/// </summary>
public interface IChatAnalyticsService
{
    /// <summary>
    /// Records facts about one answered question. Never throws: analytics failing must not cost the
    /// shopper their answer.
    /// </summary>
    Task TrackAsync(IList<ChatEvent> events);

    /// <summary>
    /// Gets the questions shoppers ask most.
    /// </summary>
    Task<IList<ChatTermStat>> GetTopQuestionsAsync(DateTime? fromUtc, DateTime? toUtc, int storeId, int count = 20);

    /// <summary>
    /// Gets the keywords shoppers search most.
    /// </summary>
    Task<IList<ChatTermStat>> GetTopKeywordsAsync(DateTime? fromUtc, DateTime? toUtc, int storeId, int count = 20);

    /// <summary>
    /// Gets the products the assistant surfaced most, or that shoppers clicked through to most.
    /// </summary>
    /// <param name="eventType">
    /// <see cref="ChatEventType.ProductRecommended"/> for what the assistant offered,
    /// <see cref="ChatEventType.ProductViewed"/> for what shoppers actually opened
    /// </param>
    Task<IList<ChatProductStat>> GetTopProductsAsync(ChatEventType eventType, DateTime? fromUtc, DateTime? toUtc,
        int storeId, int count = 20);

    /// <summary>
    /// Gets the headline numbers: conversations, messages, average conversation length, average response time.
    /// </summary>
    Task<ChatAnalyticsSummary> GetSummaryAsync(DateTime? fromUtc, DateTime? toUtc, int storeId);
}

/// <summary>
/// Represents "this term, this many times".
/// </summary>
public class ChatTermStat
{
    public string Term { get; set; }

    public int Count { get; set; }
}

/// <summary>
/// Represents "this product, this many times".
/// </summary>
public class ChatProductStat
{
    public int ProductId { get; set; }

    public string ProductName { get; set; }

    public int Count { get; set; }
}

/// <summary>
/// Represents the headline numbers.
/// </summary>
public class ChatAnalyticsSummary
{
    public int TotalConversations { get; set; }

    public int TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the average number of messages in a conversation. A conversation that stays at 2 means
    /// shoppers ask once and leave; a rising number means the assistant is holding their attention.
    /// </summary>
    public double AverageConversationLength { get; set; }

    /// <summary>
    /// Gets or sets the average time the provider took to answer, in milliseconds.
    /// </summary>
    public double AverageResponseTimeMs { get; set; }

    public int TotalProductsRecommended { get; set; }

    public int TotalFaqMatches { get; set; }

    /// <summary>
    /// Gets or sets how many questions the assistant could not answer. The single most useful number here:
    /// each one is a missing FAQ entry or a missing product.
    /// </summary>
    public int UnansweredQuestions { get; set; }
}
