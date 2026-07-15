using Nop.Web.Framework.Models;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models;

/// <summary>
/// What the chat window needs to boot. Everything else it fetches from the API.
/// </summary>
public record NoorAiWidgetModel : BaseNopModel
{
    /// <summary>
    /// Gets or sets the accent colour, injected as a CSS custom property so a store owner can rebrand the
    /// widget without touching the stylesheet.
    /// </summary>
    public string ThemeColor { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the storefront is currently right-to-left.
    /// </summary>
    public bool IsRtl { get; set; }
}
