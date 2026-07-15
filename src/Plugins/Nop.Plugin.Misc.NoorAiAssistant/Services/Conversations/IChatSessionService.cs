using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Conversations;

/// <summary>
/// The conversation store: sessions and the messages in them. This is the "conversation history" the API
/// replays and the context a future LLM provider will be handed.
/// </summary>
public interface IChatSessionService
{
    /// <summary>
    /// Gets a conversation by its public identifier, checking it belongs to this customer.
    /// </summary>
    /// <remarks>
    /// The ownership check lives here rather than in the controller so that no caller can forget it: the
    /// GUID is the only handle the browser has, and without this check a shopper who guessed one could
    /// read someone else's conversation.
    /// </remarks>
    /// <param name="sessionGuid">The public identifier</param>
    /// <param name="customerId">The customer who must own it</param>
    /// <returns>The conversation, or null when it does not exist, is deleted, or belongs to someone else</returns>
    Task<ChatSession> GetSessionAsync(Guid sessionGuid, int customerId);

    /// <summary>
    /// Gets the customer's open conversation, creating one when they have none.
    /// </summary>
    Task<ChatSession> GetOrCreateSessionAsync(Guid? sessionGuid, int customerId, int storeId, int languageId);

    /// <summary>
    /// Gets a customer's conversations, newest first, for the history panel.
    /// </summary>
    Task<IList<ChatSession>> GetCustomerSessionsAsync(int customerId, int storeId, int maxCount = 20);

    /// <summary>
    /// Gets the messages of a conversation, oldest first.
    /// </summary>
    /// <param name="chatSessionId">Conversation identifier</param>
    /// <param name="maxCount">How many of the most recent messages to return; they come back in chronological order</param>
    Task<IList<ChatMessage>> GetMessagesAsync(int chatSessionId, int maxCount = 50);

    /// <summary>
    /// Appends a message and bumps the conversation's activity stamp and message count.
    /// </summary>
    Task AddMessageAsync(ChatSession session, ChatMessage message);

    /// <summary>
    /// Soft-deletes every conversation a customer has, which is what "clear conversation" does.
    /// </summary>
    /// <remarks>
    /// Soft, not hard: the analytics facts reference these conversations, and a shopper clearing their
    /// chat window should not silently rewrite the store's reports.
    /// </remarks>
    Task ClearCustomerSessionsAsync(int customerId, int storeId);

    Task UpdateSessionAsync(ChatSession session);
}
