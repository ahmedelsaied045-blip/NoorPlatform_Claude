namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;

/// <summary>
/// Represents a kind of room and the light it wants. The lux targets follow the usual interior-lighting
/// guidance (EN 12464-1 for the work spaces, common residential practice for the rest): a corridor needs
/// far less light than a kitchen worktop, and lighting both to the same level is the single most common
/// way a lighting plan goes wrong.
/// </summary>
public class RoomProfile
{
    public string Slug { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    /// <summary>
    /// Gets or sets the phrases that select this room, in either language, in normalised form.
    /// </summary>
    public string[] Terms { get; set; } = [];

    /// <summary>
    /// Gets or sets the illuminance target, in lux.
    /// </summary>
    public int LuxTarget { get; set; }

    /// <summary>
    /// Gets or sets the colour temperature that suits the room, in kelvin. Warm (2700-3000K) where people
    /// relax, neutral-to-cool (4000K+) where they work.
    /// </summary>
    public int ColorTemperatureKelvin { get; set; }

    public string GetName(bool arabic)
    {
        return arabic ? NameAr : NameEn;
    }
}
