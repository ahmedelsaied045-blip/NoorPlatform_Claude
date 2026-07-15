using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models;

/// <summary>
/// Represents a side-by-side comparison of two products.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ProductComparisonDto
{
    public ProductCardDto ProductA { get; set; }

    public ProductCardDto ProductB { get; set; }

    /// <summary>
    /// Gets or sets the comparison rows. Every row is present for both products, with an empty string
    /// where a product does not declare that attribute, so the UI can render a rectangular table.
    /// </summary>
    public IList<ComparisonRowDto> Rows { get; set; } = new List<ComparisonRowDto>();

    /// <summary>
    /// Gets or sets what product A is better at.
    /// </summary>
    public IList<string> AdvantagesA { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets what product A is worse at.
    /// </summary>
    public IList<string> DisadvantagesA { get; set; } = new List<string>();

    public IList<string> AdvantagesB { get; set; } = new List<string>();

    public IList<string> DisadvantagesB { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the closing recommendation.
    /// </summary>
    public string Recommendation { get; set; }

    /// <summary>
    /// Gets or sets the id of the product the recommendation favours, so the UI can highlight its column.
    /// </summary>
    public int RecommendedProductId { get; set; }
}

/// <summary>
/// Represents one attribute compared across both products.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ComparisonRowDto
{
    /// <summary>
    /// Gets or sets the localised attribute name, e.g. "Price", "Brand", "Wattage".
    /// </summary>
    public string Label { get; set; }

    public string ValueA { get; set; }

    public string ValueB { get; set; }

    /// <summary>
    /// Gets or sets which side wins this row: -1 for A, 1 for B, 0 for a tie or a row that cannot be
    /// ranked (brand, for instance).
    /// </summary>
    public int Winner { get; set; }
}
