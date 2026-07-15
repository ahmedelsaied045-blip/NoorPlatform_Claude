using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models;

/// <summary>
/// Represents the output of the lighting planner: what to buy for a room of a given size.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class LightingPlanDto
{
    public double RoomLengthMeters { get; set; }

    public double RoomWidthMeters { get; set; }

    public double AreaSquareMeters { get; set; }

    /// <summary>
    /// Gets or sets the room type the plan was calculated for (living room, kitchen, ...), which sets the
    /// lux target.
    /// </summary>
    public string RoomType { get; set; }

    /// <summary>
    /// Gets or sets the illuminance target used, in lux.
    /// </summary>
    public int LuxTarget { get; set; }

    /// <summary>
    /// Gets or sets the total light output the room needs, in lumens (area x lux target).
    /// </summary>
    public int TotalLumensRequired { get; set; }

    /// <summary>
    /// Gets or sets how many fixtures to install.
    /// </summary>
    public int FixtureCount { get; set; }

    /// <summary>
    /// Gets or sets the recommended wattage per fixture.
    /// </summary>
    public int WattsPerFixture { get; set; }

    /// <summary>
    /// Gets or sets the total load, in watts.
    /// </summary>
    public int TotalWatts { get; set; }

    /// <summary>
    /// Gets or sets the recommended colour temperature, in kelvin (e.g. 3000).
    /// </summary>
    public int ColorTemperatureKelvin { get; set; }

    /// <summary>
    /// Gets or sets the human-readable colour temperature, e.g. "Warm white (3000K)".
    /// </summary>
    public string ColorTemperatureLabel { get; set; }

    /// <summary>
    /// Gets or sets the suggested grid layout, e.g. "3 x 2".
    /// </summary>
    public string SuggestedLayout { get; set; }

    /// <summary>
    /// Gets or sets the name of the catalogue category the shopper should buy from.
    /// </summary>
    public string RecommendedCategory { get; set; }

    /// <summary>
    /// Gets or sets the id of that category, so the UI can link to it. 0 when the store has no matching
    /// category.
    /// </summary>
    public int RecommendedCategoryId { get; set; }
}
