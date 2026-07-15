using Nop.Core;

namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents a question the assistant can answer from the store's own knowledge base rather than from
/// the catalogue.
/// </summary>
public partial class ChatFaq : BaseEntity
{
    /// <summary>
    /// Gets or sets the question as a shopper would ask it.
    /// </summary>
    public string Question { get; set; }

    /// <summary>
    /// Gets or sets the answer. Markdown is supported and sanitised before it reaches the browser.
    /// </summary>
    public string Answer { get; set; }

    /// <summary>
    /// Gets or sets comma-separated keywords that should route a shopper question to this entry, in either
    /// language (e.g. "shipping,delivery,شحن,توصيل"). Matching against these is what makes the FAQ work
    /// without an LLM.
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// Gets or sets the topic identifier.
    /// </summary>
    public int TopicId { get; set; }

    /// <summary>
    /// Gets or sets the language this entry answers in. 0 makes it available in every language.
    /// </summary>
    public int LanguageId { get; set; }

    /// <summary>
    /// Gets or sets the store this entry belongs to. 0 makes it available in every store.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the entry can be matched.
    /// </summary>
    public bool Published { get; set; }

    /// <summary>
    /// Gets or sets the date and time of creation.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the date and time of the last update.
    /// </summary>
    public DateTime UpdatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the topic.
    /// </summary>
    public FaqTopic Topic
    {
        get => (FaqTopic)TopicId;
        set => TopicId = (int)value;
    }
}
