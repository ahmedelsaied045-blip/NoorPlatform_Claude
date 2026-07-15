using Nop.Core;

namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents a starter question shown as a chip in an empty conversation.
/// </summary>
public partial class SuggestedQuestion : BaseEntity
{
    /// <summary>
    /// Gets or sets the question text.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the language this chip is shown in. 0 shows it in every language.
    /// </summary>
    public int LanguageId { get; set; }

    /// <summary>
    /// Gets or sets the store this chip is shown in. 0 shows it in every store.
    /// </summary>
    public int StoreId { get; set; }

    /// <summary>
    /// Gets or sets the display order.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the chip is shown.
    /// </summary>
    public bool Published { get; set; }
}
