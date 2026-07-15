using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Intent;

/// <summary>
/// Everything the intent engine could work out about a message from the text alone.
/// </summary>
/// <remarks>
/// This is deliberately the *structural* reading only — it never touches the database. Whether a question
/// is really a FAQ depends on what FAQ entries the store has written, which is a lookup, so the provider
/// makes that call. Keeping the recogniser pure keeps it synchronous, cheap and testable.
/// </remarks>
public class IntentAnalysis
{
    /// <summary>
    /// Gets or sets the message in normalised form.
    /// </summary>
    public string NormalizedMessage { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the shopper wrote in Arabic. Drives which language the
    /// answer comes back in, regardless of the storefront language — someone who types Arabic into an
    /// English store gets Arabic back.
    /// </summary>
    public bool IsArabic { get; set; }

    /// <summary>
    /// Gets or sets the structural intent. <see cref="ChatIntent.Faq"/> is never set here; the provider
    /// promotes an intent to FAQ when the knowledge base actually matches.
    /// </summary>
    public ChatIntent Intent { get; set; }

    /// <summary>
    /// Gets or sets the product kind the shopper named, if any.
    /// </summary>
    public NeedProfile Need { get; set; }

    /// <summary>
    /// Gets or sets the room the shopper named, if any.
    /// </summary>
    public RoomProfile Room { get; set; }

    /// <summary>
    /// Gets or sets the room size the shopper gave, if any.
    /// </summary>
    public RoomDimensions Dimensions { get; set; }

    /// <summary>
    /// Gets or sets the first thing being compared.
    /// </summary>
    public string ComparisonTermA { get; set; }

    /// <summary>
    /// Gets or sets the second thing being compared.
    /// </summary>
    public string ComparisonTermB { get; set; }

    /// <summary>
    /// Gets or sets the meaningful terms, stop words removed. Used both as the catalogue search query and
    /// as the "most searched keywords" analytics fact.
    /// </summary>
    public IList<string> Keywords { get; set; } = new List<string>();

    /// <summary>
    /// Gets the search query handed to the catalogue: the keywords joined back into a phrase.
    /// </summary>
    public string SearchQuery => string.Join(' ', Keywords);
}
