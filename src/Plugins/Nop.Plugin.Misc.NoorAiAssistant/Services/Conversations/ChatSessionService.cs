using Nop.Data;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Conversations;

/// <summary>
/// Stores conversations and their messages.
/// </summary>
public partial class ChatSessionService : IChatSessionService
{
    #region Fields

    private readonly IRepository<ChatMessage> _messageRepository;
    private readonly IRepository<ChatSession> _sessionRepository;

    #endregion

    #region Ctor

    public ChatSessionService(IRepository<ChatMessage> messageRepository,
        IRepository<ChatSession> sessionRepository)
    {
        _messageRepository = messageRepository;
        _sessionRepository = sessionRepository;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a conversation by its public identifier, checking it belongs to this customer
    /// </summary>
    public virtual async Task<ChatSession> GetSessionAsync(Guid sessionGuid, int customerId)
    {
        if (sessionGuid == Guid.Empty || customerId == 0)
            return null;

        return await _sessionRepository.Table
            .Where(session => session.SessionGuid == sessionGuid &&
                              session.CustomerId == customerId &&
                              !session.Deleted)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets the customer's open conversation, creating one when they have none
    /// </summary>
    public virtual async Task<ChatSession> GetOrCreateSessionAsync(Guid? sessionGuid, int customerId, int storeId, int languageId)
    {
        if (sessionGuid.HasValue)
        {
            var existing = await GetSessionAsync(sessionGuid.Value, customerId);
            if (existing is not null)
                return existing;

            //the browser sent a GUID we do not recognise — its cookie outlived the row, or it belongs to
            //someone else. Either way, start a fresh conversation rather than handing back another
            //customer's; never trust a client-supplied identity.
        }

        var session = new ChatSession
        {
            SessionGuid = Guid.NewGuid(),
            CustomerId = customerId,
            StoreId = storeId,
            LanguageId = languageId,
            CreatedOnUtc = DateTime.UtcNow,
            LastActivityOnUtc = DateTime.UtcNow
        };

        await _sessionRepository.InsertAsync(session);

        return session;
    }

    /// <summary>
    /// Gets a customer's conversations, newest first
    /// </summary>
    public virtual async Task<IList<ChatSession>> GetCustomerSessionsAsync(int customerId, int storeId, int maxCount = 20)
    {
        if (customerId == 0)
            return [];

        return await _sessionRepository.Table
            .Where(session => session.CustomerId == customerId &&
                              session.StoreId == storeId &&
                              !session.Deleted &&
                              //an empty conversation is one the shopper opened and never used; it would
                              //show as a blank row in the history panel
                              session.MessageCount > 0)
            .OrderByDescending(session => session.LastActivityOnUtc)
            .Take(maxCount)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the messages of a conversation, oldest first
    /// </summary>
    public virtual async Task<IList<ChatMessage>> GetMessagesAsync(int chatSessionId, int maxCount = 50)
    {
        if (chatSessionId == 0)
            return [];

        //take the newest N, then flip: a long conversation should be truncated at the start, not the end
        var messages = await _messageRepository.Table
            .Where(message => message.ChatSessionId == chatSessionId)
            .OrderByDescending(message => message.Id)
            .Take(maxCount)
            .ToListAsync();

        return messages.OrderBy(message => message.Id).ToList();
    }

    /// <summary>
    /// Appends a message and bumps the conversation's activity stamp and message count
    /// </summary>
    public virtual async Task AddMessageAsync(ChatSession session, ChatMessage message)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(message);

        message.ChatSessionId = session.Id;
        message.CreatedOnUtc = DateTime.UtcNow;

        await _messageRepository.InsertAsync(message);

        session.MessageCount++;
        session.LastActivityOnUtc = DateTime.UtcNow;

        //name the conversation after the shopper's opening line, the way ChatGPT does
        if (string.IsNullOrEmpty(session.Title) && message.Role == ChatMessageRole.User)
            session.Title = TextNormalizer.ToTitle(message.Message);

        await _sessionRepository.UpdateAsync(session);
    }

    /// <summary>
    /// Soft-deletes every conversation a customer has
    /// </summary>
    public virtual async Task ClearCustomerSessionsAsync(int customerId, int storeId)
    {
        if (customerId == 0)
            return;

        var sessions = await _sessionRepository.Table
            .Where(session => session.CustomerId == customerId &&
                              session.StoreId == storeId &&
                              !session.Deleted)
            .ToListAsync();

        if (sessions.Count == 0)
            return;

        foreach (var session in sessions)
            session.Deleted = true;

        await _sessionRepository.UpdateAsync(sessions);
    }

    public virtual async Task UpdateSessionAsync(ChatSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        await _sessionRepository.UpdateAsync(session);
    }

    #endregion
}
