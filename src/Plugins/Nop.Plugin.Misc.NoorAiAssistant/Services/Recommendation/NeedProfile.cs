namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;

/// <summary>
/// Represents one thing a shopper can ask for, and everything needed to turn that ask into a catalogue
/// query and a sensible reply.
/// </summary>
public class NeedProfile
{
    /// <summary>
    /// Gets or sets the stable identifier, used in analytics and follow-up chips.
    /// </summary>
    public string Slug { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>
    /// Gets or sets the phrases that select this need, in either language. Matched against the normalised
    /// message, so they must themselves be written in normal form (no diacritics, bare alef, ه for ة).
    /// </summary>
    public string[] Terms { get; set; } = [];

    /// <summary>
    /// Gets or sets fragments matched against the store's own category names. This is what keeps the
    /// plugin working against whatever Noor actually calls its categories: rather than hard-coding
    /// category ids that would break on any catalogue edit, the need is resolved to live categories at
    /// query time and simply falls through to a keyword search when nothing matches.
    /// </summary>
    public string[] CategoryHints { get; set; } = [];

    /// <summary>
    /// Gets or sets the terms handed to the catalogue full-text search when no category matches.
    /// </summary>
    public string[] SearchKeywords { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether this need is a light fixture, and so can carry a lighting
    /// plan (count / wattage / colour temperature).
    /// </summary>
    public bool IsLightFixture { get; set; }

    /// <summary>
    /// Gets or sets the colour temperature that suits this fixture, in kelvin. 0 when not applicable.
    /// </summary>
    public int ColorTemperatureKelvin { get; set; }

    /// <summary>
    /// Gets the localised display name.
    /// </summary>
    public string GetName(bool arabic)
    {
        return arabic ? NameAr : NameEn;
    }
}
