using Nop.Core;

namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents a single conversation between a shopper and the assistant.
/// </summary>
public partial class ChatSession : BaseEntity
{
    /// <summary>
    /// Gets or sets the customer identifier. Guests get a customer record in nopCommerce too, so this is
    /// always populated.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the store identifier.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the language the conversation was started in.
    /// </summary>
    public int LanguageId { get; set; }

    /// <summary>
    /// Gets or sets the public identifier. The browser only ever sees this, never the integer id, so a
    /// shopper cannot enumerate other people's conversations.
    /// </summary>
    public Guid SessionGuid { get; set; }

    /// <summary>
    /// Gets or sets the conversation title, derived from the first shopper message.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the number of messages, denormalised so the history list does not need a join.
    /// </summary>
    public int MessageCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the conversation is still open. Cleared instead of deleted
    /// when a shopper clears their history, so analytics stay intact.
    /// </summary>
    public bool Deleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time of creation.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the last message.
    /// </summary>
    public DateTime LastActivityOnUtc { get; set; }
}
