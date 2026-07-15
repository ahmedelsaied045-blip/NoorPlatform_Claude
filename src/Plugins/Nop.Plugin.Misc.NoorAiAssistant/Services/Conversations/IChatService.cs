using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Api;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Conversations;

/// <summary>
/// The application service behind the REST API: everything that must happen around answering a message.
/// </summary>
/// <remarks>
/// This owns validation, rate limiting, session resolution, persistence and analytics, and calls the
/// active <see cref="Providers.IChatProvider"/> in the middle to do the actual answering. Keeping all of
/// it here is what makes swapping the provider a one-class change: none of this has to be reimplemented by
/// whoever writes the OpenAI provider.
/// </remarks>
public interface IChatService
{
    /// <summary>
    /// Answers a shopper's message and records the turn.
    /// </summary>
    Task<ChatSendResult> SendAsync(SendMessageRequest request);

    /// <summary>
    /// Replays a conversation, and lists the shopper's other conversations.
    /// </summary>
    /// <param name="sessionId">The conversation to replay; omit for the most recent one</param>
    Task<ChatHistoryResponse> GetHistoryAsync(Guid? sessionId);

    /// <summary>
    /// Clears the shopper's conversations.
    /// </summary>
    Task ClearHistoryAsync();

    /// <summary>
    /// Gets the greeting and the starter chips for an empty conversation.
    /// </summary>
    Task<SuggestionsResponse> GetSuggestionsAsync();

    /// <summary>
    /// Searches the catalogue directly, for the search box inside the chat window.
    /// </summary>
    Task<IList<ProductCardDto>> SearchProductsAsync(string query, int pageSize);

    /// <summary>
    /// Records that a shopper clicked through to a product from a card, which is what the "most viewed
    /// products" report counts.
    /// </summary>
    Task TrackProductViewAsync(Guid sessionId, int productId);
}
