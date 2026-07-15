using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Api;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Analytics;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Providers;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Security;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Conversations;

/// <summary>
/// Runs one turn of a conversation end to end.
/// </summary>
public partial class ChatService : IChatService
{
    #region Fields

    private readonly IChatAnalyticsService _chatAnalyticsService;
    private readonly IChatRateLimiter _chatRateLimiter;
    private readonly IChatSessionService _chatSessionService;
    private readonly ILanguageService _languageService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<ChatService> _logger;
    private readonly IEnumerable<IChatProvider> _chatProviders;
    private readonly IProductSearchService _productSearchService;
    private readonly IStoreContext _storeContext;
    private readonly ISuggestedQuestionService _suggestedQuestionService;
    private readonly IWorkContext _workContext;
    private readonly NoorAiAssistantSettings _settings;

    /// <summary>
    /// Newtonsoft, not System.Text.Json, and deliberately so: nopCommerce serialises MVC responses with
    /// Newtonsoft, so using it here too means the payload stored in the database and the payload sent over
    /// the wire are produced by the same serialiser under the same naming strategy. A message replayed from
    /// history therefore reaches the browser in exactly the shape a fresh one does.
    /// </summary>
    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        NullValueHandling = NullValueHandling.Ignore
    };

    #endregion

    #region Ctor

    public ChatService(IChatAnalyticsService chatAnalyticsService,
        IChatRateLimiter chatRateLimiter,
        IChatSessionService chatSessionService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ILogger<ChatService> logger,
        IEnumerable<IChatProvider> chatProviders,
        IProductSearchService productSearchService,
        IStoreContext storeContext,
        ISuggestedQuestionService suggestedQuestionService,
        IWorkContext workContext,
        NoorAiAssistantSettings settings)
    {
        _chatAnalyticsService = chatAnalyticsService;
        _chatRateLimiter = chatRateLimiter;
        _chatSessionService = chatSessionService;
        _languageService = languageService;
        _localizationService = localizationService;
        _logger = logger;
        _chatProviders = chatProviders;
        _productSearchService = productSearchService;
        _storeContext = storeContext;
        _suggestedQuestionService = suggestedQuestionService;
        _workContext = workContext;
        _settings = settings;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Selects the provider the store owner configured.
    /// </summary>
    /// <remarks>
    /// Every IChatProvider registered in DI is a candidate, and the setting picks one by system name. This
    /// is the entire switching mechanism: adding OpenAI means registering one more implementation and
    /// typing its name into the admin. Falls back to the built-in provider rather than failing, so a
    /// mistyped or uninstalled provider name degrades to canned answers instead of a broken chat box.
    /// </remarks>
    protected virtual async Task<IChatProvider> GetActiveProviderAsync()
    {
        var providers = _chatProviders.ToList();

        var configured = providers.FirstOrDefault(provider =>
            string.Equals(provider.SystemName, _settings.ActiveProviderSystemName, StringComparison.OrdinalIgnoreCase));

        if (configured is not null && await configured.IsConfiguredAsync())
            return configured;

        if (configured is not null)
        {
            _logger.LogWarning(
                "Noor AI assistant: provider '{Provider}' is selected but not configured; falling back to the built-in provider",
                _settings.ActiveProviderSystemName);
        }

        return providers.FirstOrDefault(provider => provider.SystemName == "Dummy") ?? providers.FirstOrDefault();
    }

    /// <summary>
    /// Turns a stored message into what the browser expects.
    /// </summary>
    protected virtual ChatMessageDto ToDto(ChatMessage message)
    {
        ChatAnswerPayload payload = null;

        if (!string.IsNullOrEmpty(message.PayloadJson))
        {
            try
            {
                payload = JsonConvert.DeserializeObject<ChatAnswerPayload>(message.PayloadJson, _jsonSettings);
            }
            catch (JsonException exception)
            {
                //a payload whose shape changed across a plugin upgrade must not take the whole conversation
                //down with it — drop the rich content and keep the prose
                _logger.LogWarning(exception,
                    "Noor AI assistant: could not read the payload of message {MessageId}; showing text only", message.Id);
            }
        }

        return new ChatMessageDto
        {
            Id = message.Id,
            Role = message.Role == ChatMessageRole.User ? "user" : "assistant",
            Text = message.Message,
            Payload = payload,
            CreatedOnUtc = message.CreatedOnUtc
        };
    }

    /// <summary>
    /// Records what happened, for the analytics reports.
    /// </summary>
    protected virtual async Task TrackAsync(ChatSession session, ChatProviderResponse answer, string question, int responseTimeMs)
    {
        if (!_settings.TrackAnalytics)
            return;

        var now = DateTime.UtcNow;

        ChatEvent Event(ChatEventType type) => new()
        {
            ChatSessionId = session.Id,
            CustomerId = session.CustomerId,
            StoreId = session.StoreId,
            EventType = type,
            Intent = answer.Intent,
            CreatedOnUtc = now
        };

        var events = new List<ChatEvent>();

        //the question itself, normalised so that "Do you have chandeliers?" and "do you have chandeliers"
        //are one row in the "most asked" report rather than two
        var questionEvent = Event(ChatEventType.QuestionAsked);
        questionEvent.EventKey = TextNormalizer.Normalize(question);
        questionEvent.ResponseTimeMs = responseTimeMs;
        events.Add(questionEvent);

        foreach (var keyword in answer.Keywords ?? [])
        {
            var keywordEvent = Event(ChatEventType.KeywordSearched);
            keywordEvent.EventKey = keyword;
            events.Add(keywordEvent);
        }

        foreach (var product in answer.Payload?.Products ?? [])
        {
            var productEvent = Event(ChatEventType.ProductRecommended);
            productEvent.ProductId = product.Id;
            events.Add(productEvent);
        }

        //a comparison surfaces two products just as a recommendation does; count them the same way
        if (answer.Payload?.Comparison is not null)
        {
            foreach (var productId in new[] { answer.Payload.Comparison.ProductA.Id, answer.Payload.Comparison.ProductB.Id })
            {
                var comparedEvent = Event(ChatEventType.ProductRecommended);
                comparedEvent.ProductId = productId;
                events.Add(comparedEvent);
            }
        }

        if (answer.MatchedFaqId > 0)
        {
            var faqEvent = Event(ChatEventType.FaqMatched);
            faqEvent.EventKey = answer.MatchedFaqId.ToString();
            events.Add(faqEvent);
        }

        await _chatAnalyticsService.TrackAsync(events);
    }

    /// <summary>
    /// Determines whether the working language is written right-to-left.
    /// </summary>
    protected virtual async Task<bool> IsRightToLeftAsync(int languageId)
    {
        var language = await _languageService.GetLanguageByIdAsync(languageId);

        return language?.Rtl ?? false;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Answers a shopper's message and records the turn
    /// </summary>
    public virtual async Task<ChatSendResult> SendAsync(SendMessageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_settings.Enabled)
        {
            return ChatSendResult.Fail(StatusCodes.Status503ServiceUnavailable,
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Public.Disabled"));
        }

        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();

        //strip markup before the message is stored or echoed. The browser escapes on render as well; this
        //is the second of the two, not the only one.
        var message = TextNormalizer.StripHtml(request.Message ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(message))
        {
            return ChatSendResult.Fail(StatusCodes.Status400BadRequest,
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Public.EmptyMessage"));
        }

        var maxLength = _settings.MaxMessageLength > 0 ? _settings.MaxMessageLength : 1000;
        if (message.Length > maxLength)
            message = message[..maxLength];

        if (!await _chatRateLimiter.TryAcquireAsync(customer.Id))
        {
            return ChatSendResult.Fail(StatusCodes.Status429TooManyRequests,
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Public.RateLimited"));
        }

        var session = await _chatSessionService.GetOrCreateSessionAsync(request.SessionId, customer.Id, store.Id, language.Id);

        await _chatSessionService.AddMessageAsync(session, new ChatMessage
        {
            Role = ChatMessageRole.User,
            Message = message
        });

        //the provider sees the turns before this one, capped at the configured depth. +1 because the
        //message just stored is included in the fetch and then dropped below.
        var historyDepth = _settings.MaxHistoryMessages > 0 ? _settings.MaxHistoryMessages : 10;
        var history = await _chatSessionService.GetMessagesAsync(session.Id, historyDepth + 1);

        //the last row is the question itself, which is passed separately as Message — a provider handed it
        //twice would see the shopper ask the same thing twice
        var priorTurns = history
            .Take(Math.Max(0, history.Count - 1))
            .Select(m => new ChatTurn { Role = m.Role, Message = m.Message })
            .ToList();

        var providerRequest = new ChatProviderRequest
        {
            Message = message,
            CustomerId = customer.Id,
            StoreId = store.Id,
            LanguageId = language.Id,
            IsRightToLeft = await IsRightToLeftAsync(language.Id),
            AllowProductRecommendation = _settings.AllowProductRecommendation,
            AllowProductComparison = _settings.AllowProductComparison,
            AllowQuantityEstimation = _settings.AllowQuantityEstimation,
            MaxProducts = _settings.MaxProductsPerAnswer > 0 ? _settings.MaxProductsPerAnswer : 3,
            History = priorTurns
        };

        var provider = await GetActiveProviderAsync();
        if (provider is null)
        {
            _logger.LogError("Noor AI assistant: no chat provider is registered");

            return ChatSendResult.Fail(StatusCodes.Status503ServiceUnavailable,
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Public.Error"));
        }

        var stopwatch = Stopwatch.StartNew();

        ChatProviderResponse answer;
        try
        {
            answer = await provider.GetResponseAsync(providerRequest);
        }
        catch (Exception exception)
        {
            //a provider blowing up (a future one will be talking to a network service) must produce an
            //apology in the chat window, not a 500 and an empty bubble
            _logger.LogError(exception, "Noor AI assistant: provider '{Provider}' failed to answer", provider.SystemName);

            answer = ChatProviderResponse.FromText(
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Public.Error"),
                ChatIntent.Unknown);
        }

        stopwatch.Stop();
        var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

        var payload = answer.Payload is { IsEmpty: false } ? answer.Payload : null;

        var assistantMessage = new ChatMessage
        {
            Role = ChatMessageRole.Assistant,
            Message = answer.Text,
            Intent = answer.Intent,
            PayloadJson = payload is not null ? JsonConvert.SerializeObject(payload, _jsonSettings) : null,
            ProviderSystemName = provider.SystemName,
            ResponseTimeMs = responseTimeMs
        };

        await _chatSessionService.AddMessageAsync(session, assistantMessage);

        await TrackAsync(session, answer, message, responseTimeMs);

        return ChatSendResult.Ok(new SendMessageResponse
        {
            SessionId = session.SessionGuid,
            Reply = ToDto(assistantMessage)
        });
    }

    /// <summary>
    /// Replays a conversation, and lists the shopper's other conversations
    /// </summary>
    public virtual async Task<ChatHistoryResponse> GetHistoryAsync(Guid? sessionId)
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        var sessions = await _chatSessionService.GetCustomerSessionsAsync(customer.Id, store.Id);

        var response = new ChatHistoryResponse
        {
            Sessions = sessions.Select(session => new ChatSessionSummaryDto
            {
                SessionId = session.SessionGuid,
                Title = session.Title,
                MessageCount = session.MessageCount,
                LastActivityOnUtc = session.LastActivityOnUtc
            }).ToList()
        };

        //an explicit id must belong to this customer (GetSessionAsync enforces that); with no id, open the
        //most recent conversation, which is what a returning shopper expects to see
        var session = sessionId.HasValue
            ? await _chatSessionService.GetSessionAsync(sessionId.Value, customer.Id)
            : sessions.FirstOrDefault();

        if (session is null)
            return response;

        var messages = await _chatSessionService.GetMessagesAsync(session.Id,
            _settings.MaxHistoryMessages > 0 ? _settings.MaxHistoryMessages : 10);

        response.SessionId = session.SessionGuid;
        response.Messages = messages.Select(ToDto).ToList();

        return response;
    }

    /// <summary>
    /// Clears the shopper's conversations
    /// </summary>
    public virtual async Task ClearHistoryAsync()
    {
        var customer = await _workContext.GetCurrentCustomerAsync();
        var store = await _storeContext.GetCurrentStoreAsync();

        await _chatSessionService.ClearCustomerSessionsAsync(customer.Id, store.Id);
    }

    /// <summary>
    /// Gets the greeting and the starter chips for an empty conversation
    /// </summary>
    public virtual async Task<SuggestionsResponse> GetSuggestionsAsync()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();

        var questions = await _suggestedQuestionService.GetPublishedQuestionsAsync(store.Id, language.Id);

        var rtl = await IsRightToLeftAsync(language.Id);
        var welcome = rtl ? _settings.WelcomeMessageAr : _settings.WelcomeMessage;

        return new SuggestionsResponse
        {
            Welcome = welcome,
            Suggestions = questions.Select(question => question.Text).ToList()
        };
    }

    /// <summary>
    /// Searches the catalogue directly
    /// </summary>
    public virtual async Task<IList<ProductCardDto>> SearchProductsAsync(string query, int pageSize)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();

        var products = await _productSearchService.SearchAsync(new ProductSearchQuery
        {
            Keywords = TextNormalizer.StripHtml(query),
            StoreId = store.Id,
            LanguageId = language.Id,
            PageSize = Math.Clamp(pageSize, 1, 24)
        });

        return await _productSearchService.PrepareCardsAsync(products);
    }

    /// <summary>
    /// Records that a shopper clicked through to a product from a card
    /// </summary>
    public virtual async Task TrackProductViewAsync(Guid sessionId, int productId)
    {
        if (!_settings.TrackAnalytics || productId <= 0)
            return;

        var customer = await _workContext.GetCurrentCustomerAsync();

        var session = await _chatSessionService.GetSessionAsync(sessionId, customer.Id);
        if (session is null)
            return;

        await _chatAnalyticsService.TrackAsync(
        [
            new ChatEvent
            {
                ChatSessionId = session.Id,
                CustomerId = session.CustomerId,
                StoreId = session.StoreId,
                EventType = ChatEventType.ProductViewed,
                ProductId = productId,
                CreatedOnUtc = DateTime.UtcNow
            }
        ]);
    }

    #endregion
}
