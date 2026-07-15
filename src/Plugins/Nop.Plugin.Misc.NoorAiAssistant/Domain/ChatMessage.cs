using Nop.Core;

namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents one turn in a conversation. Together with <see cref="ChatSession"/> this is the durable
/// conversation history: it is what the history endpoint replays and what a future LLM provider will be
/// handed as context.
/// </summary>
public partial class ChatMessage : BaseEntity
{
    /// <summary>
    /// Gets or sets the conversation identifier.
    /// </summary>
    public int ChatSessionId { get; set; }

    /// <summary>
    /// Gets or sets the author identifier.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Gets or sets the resolved intent identifier (assistant messages only).
    /// </summary>
    public int IntentId { get; set; }

    /// <summary>
    /// Gets or sets the message body. Assistant messages are markdown; user messages are plain text.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the structured part of an assistant answer (product cards, comparison table, lighting
    /// plan) as JSON. Kept separate from <see cref="Message"/> so the UI can re-render rich content when
    /// replaying history without parsing prose.
    /// </summary>
    public string PayloadJson { get; set; }

    /// <summary>
    /// Gets or sets the system name of the provider that produced this answer, so a store that switches
    /// from the dummy provider to a real model can still tell the two apart in analytics.
    /// </summary>
    public string ProviderSystemName { get; set; }

    /// <summary>
    /// Gets or sets how long the provider took to answer, in milliseconds.
    /// </summary>
    public int ResponseTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the date and time of creation.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the author.
    /// </summary>
    public ChatMessageRole Role
    {
        get => (ChatMessageRole)RoleId;
        set => RoleId = (int)value;
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
