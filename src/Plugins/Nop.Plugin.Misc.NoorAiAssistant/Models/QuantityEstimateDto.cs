using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models;

/// <summary>
/// Represents "how many do I need?" for a single product kind, when the shopper has not given enough
/// information for a full <see cref="LightingPlanDto"/>.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class QuantityEstimateDto
{
    /// <summary>
    /// Gets or sets what is being counted, e.g. "spotlights", "LED strip".
    /// </summary>
    public string ItemLabel { get; set; }

    /// <summary>
    /// Gets or sets the recommended quantity.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Gets or sets the unit, e.g. "pcs" or "m".
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Gets or sets the plain-language explanation of how the number was reached, so the shopper can sanity
    /// check it rather than being handed a bare figure.
    /// </summary>
    public string Explanation { get; set; }

    /// <summary>
    /// Gets or sets the assumptions the estimate rests on. Shown as a caveat list; if the shopper's room is
    /// different they will know which number to correct.
    /// </summary>
    public IList<string> Assumptions { get; set; } = new List<string>();
}
