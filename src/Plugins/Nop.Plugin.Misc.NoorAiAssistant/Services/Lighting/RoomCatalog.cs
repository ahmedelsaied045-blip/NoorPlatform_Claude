using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Lighting;

/// <summary>
/// Every room the lighting planner knows how to light.
/// </summary>
public static class RoomCatalog
{
    /// <summary>
    /// Gets the room used when the shopper gives dimensions but does not say what the room is. A living
    /// room is the safest default: its 150 lux target sits in the middle of the range, so the estimate is
    /// never wildly wrong in either direction.
    /// </summary>
    public static RoomProfile Default => All.First(r => r.Slug == "living-room");

    public static IReadOnlyList<RoomProfile> All { get; } =
    [
        new()
        {
            Slug = "kitchen",
            NameEn = "kitchen", NameAr = "المطبخ",
            Terms = ["kitchen", "worktop", "counter", "مطبخ"],
            LuxTarget = 300, ColorTemperatureKelvin = 4000
        },
        new()
        {
            Slug = "bathroom",
            NameEn = "bathroom", NameAr = "الحمام",
            Terms = ["bathroom", "bath", "shower", "washroom", "toilet", "حمام", "دوره مياه"],
            LuxTarget = 200, ColorTemperatureKelvin = 4000
        },
        new()
        {
            Slug = "office",
            NameEn = "office", NameAr = "المكتب",
            Terms = ["office", "study", "desk", "workspace", "work room", "مكتب", "دراسه", "مكتبه"],
            LuxTarget = 400, ColorTemperatureKelvin = 4000
        },
        new()
        {
            Slug = "bedroom",
            NameEn = "bedroom", NameAr = "غرفة النوم",
            Terms = ["bedroom", "bed room", "master bedroom", "غرفه نوم", "نوم", "غرفه النوم"],
            LuxTarget = 120, ColorTemperatureKelvin = 2700
        },
        new()
        {
            Slug = "corridor",
            NameEn = "corridor", NameAr = "الممر",
            Terms = ["corridor", "hallway", "hall way", "passage", "stairs", "staircase", "ممر", "مدخل", "درج"],
            LuxTarget = 100, ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "majlis",
            NameEn = "majlis", NameAr = "المجلس",
            Terms = ["majlis", "reception", "guest room", "مجلس", "استقبال", "ضيوف"],
            LuxTarget = 200, ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "dining-room",
            NameEn = "dining room", NameAr = "غرفة الطعام",
            Terms = ["dining", "dining room", "dining table", "سفره", "غرفه طعام", "طعام"],
            LuxTarget = 150, ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "garage",
            NameEn = "garage", NameAr = "الجراج",
            Terms = ["garage", "storage", "warehouse", "workshop", "جراج", "مستودع", "ورشه", "مخزن"],
            LuxTarget = 300, ColorTemperatureKelvin = 5000
        },
        new()
        {
            Slug = "living-room",
            NameEn = "living room", NameAr = "غرفة المعيشة",
            Terms = ["living room", "living", "lounge", "salon", "sitting room", "family room",
                     "غرفه معيشه", "معيشه", "صاله", "صالون", "جلوس"],
            LuxTarget = 150, ColorTemperatureKelvin = 3000
        }
    ];

    /// <summary>
    /// Finds the room a message is about, or null when it names none.
    /// </summary>
    /// <param name="normalizedMessage">A message already put through <see cref="TextNormalizer.Normalize"/></param>
    public static RoomProfile Match(string normalizedMessage)
    {
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return null;

        return All.FirstOrDefault(room =>
            room.Terms.Any(term => TextNormalizer.ContainsTerm(normalizedMessage, term)));
    }
}
