using Nop.Core;

namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents an analytics fact. Written on every answer and never read on the hot path, so the
/// analytics reports are plain aggregate queries over this one table.
/// </summary>
public partial class ChatEvent : BaseEntity
{
    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public int ChatSessionId { get; set; }

    /// <summary>
    /// Gets or sets the event type identifier.
    /// </summary>
    public int EventTypeId { get; set; }

    /// <summary>
    /// Gets or sets the resolved intent identifier.
    /// </summary>
    public int IntentId { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the store identifier.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the product this event is about, for the product-level reports. 0 when not applicable.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Gets or sets the normalised text this event is grouped by: the question for
    /// <see cref="ChatEventType.QuestionAsked"/>, the keyword for <see cref="ChatEventType.KeywordSearched"/>.
    /// Lower-cased and trimmed on write so the "most asked" report can GROUP BY it directly.
    /// </summary>
    public string EventKey { get; set; }

    /// <summary>
    /// Gets or sets how long the answer took, in milliseconds. Only meaningful on
    /// <see cref="ChatEventType.QuestionAsked"/>.
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the date and time of creation.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the event type.
    /// </summary>
    public ChatEventType EventType
    {
        get => (ChatEventType)EventTypeId;
        set => EventTypeId = (int)value;
    }

    /// <summary>
    /// Gets or sets the resolved intent.
    /// </summary>
    public ChatIntent Intent
    {
        get => (ChatIntent)IntentId;
        set => IntentId = (int)value;
    }
}
