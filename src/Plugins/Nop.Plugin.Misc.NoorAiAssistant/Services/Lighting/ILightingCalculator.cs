using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;

/// <summary>
/// Turns a room into a shopping list. Pure arithmetic over the store's settings — no database, no HTTP —
/// which is what makes it the one piece of the assistant that is trivially unit-testable and equally
/// callable as a tool by a future LLM provider.
/// </summary>
public interface ILightingCalculator
{
    /// <summary>
    /// Reads room dimensions out of a message: "5 x 4", "5*4", "5 by 4", "5 في 4", "٥ في ٤", "20 sqm".
    /// </summary>
    /// <param name="normalizedMessage">A message already put through TextNormalizer.Normalize</param>
    /// <returns>The dimensions, or null when the message states none</returns>
    RoomDimensions ParseDimensions(string normalizedMessage);

    /// <summary>
    /// Produces a full lighting plan: how many fixtures, at what wattage, at what colour temperature.
    /// </summary>
    /// <param name="dimensions">The room</param>
    /// <param name="room">What the room is used for; null falls back to a living room</param>
    /// <param name="fixture">The fixture the shopper asked about; null falls back to spotlights</param>
    LightingPlanDto CalculatePlan(RoomDimensions dimensions, RoomProfile room, NeedProfile fixture);

    /// <summary>
    /// Answers "how many do I need?" when the shopper has given no dimensions, by assuming a typical room
    /// and saying so.
    /// </summary>
    /// <param name="fixture">What is being counted</param>
    /// <param name="room">What the room is used for; null falls back to a living room</param>
    /// <param name="dimensions">The room, when known; null assumes a typical 4x4 room</param>
    /// <param name="arabic">Whether to phrase the explanation in Arabic</param>
    QuantityEstimateDto EstimateQuantity(NeedProfile fixture, RoomProfile room, RoomDimensions dimensions, bool arabic);
}

/// <summary>
/// Represents the size of a room.
/// </summary>
public class RoomDimensions
{
    public double LengthMeters { get; set; }

    public double WidthMeters { get; set; }

    /// <summary>
    /// Gets or sets the floor area. Set directly when the shopper gave an area rather than two sides, in
    /// which case <see cref="LengthMeters"/> and <see cref="WidthMeters"/> are 0.
    /// </summary>
    public double AreaSquareMeters { get; set; }

    /// <summary>
    /// Gets a value indicating whether both sides are known, so a grid layout can be suggested.
    /// </summary>
    public bool HasBothSides => LengthMeters > 0 && WidthMeters > 0;
}
