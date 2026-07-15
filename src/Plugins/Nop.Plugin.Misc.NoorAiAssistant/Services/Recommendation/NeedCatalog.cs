using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;

/// <summary>
/// The assistant's product knowledge: every need it can recognise, and how each maps onto the catalogue.
/// </summary>
/// <remarks>
/// This is the only place with hard-coded domain vocabulary, and it is deliberately data rather than code
/// — adding "I need track lighting" is one entry here, not a new branch anywhere. When a real LLM provider
/// is added, these same profiles become the arguments of its product-search function, so the knowledge is
/// not thrown away.
///
/// Every Arabic term below is written in the form produced by <see cref="TextNormalizer.Normalize"/>
/// (bare alef, ه for ة, no diacritics), because that is what it is compared against.
/// </remarks>
public static class NeedCatalog
{
    /// <summary>
    /// Gets every recognised need. Ordered most specific first: "led strip" must win over the bare "led",
    /// and "outdoor lighting" over "lighting", because <see cref="Match"/> returns the first hit.
    /// </summary>
    public static IReadOnlyList<NeedProfile> All { get; } =
    [
        new()
        {
            Slug = "cctv",
            NameEn = "CCTV & security cameras",
            NameAr = "كاميرات المراقبة",
            Terms = ["cctv", "security camera", "surveillance", "camera", "cameras", "ip camera", "dvr", "nvr",
                     "كاميرا", "كاميرات", "مراقبه", "كاميرات مراقبه", "حمايه", "امن"],
            CategoryHints = ["cctv", "camera", "security", "surveillance", "كاميرا", "مراقبه", "امن"],
            SearchKeywords = ["cctv", "camera", "كاميرا"]
        },
        new()
        {
            Slug = "led-strip",
            NameEn = "LED strips",
            NameAr = "شرائط الليد",
            Terms = ["led strip", "led strips", "strip light", "strip lights", "light strip", "tape light",
                     "flexible strip", "شريط ليد", "شرائط ليد", "شريط اضاءه", "سترip", "ليد شريط"],
            CategoryHints = ["strip", "شريط", "شرائط"],
            SearchKeywords = ["led strip", "strip", "شريط ليد"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "chandelier",
            NameEn = "chandeliers",
            NameAr = "الثريات",
            Terms = ["chandelier", "chandeliers", "pendant", "pendant light", "hanging light",
                     "ثريا", "ثريات", "نجفه", "نجف", "دلايه", "معلقه", "مدلاه"],
            CategoryHints = ["chandelier", "pendant", "ثريا", "ثريات", "نجف"],
            //نجف (not نجفه) is deliberate: as a raw substring it matches نجف / نجفة / نجفات in the catalogue,
            //which the folded form نجفه would not, since the stored product text is not normalised
            SearchKeywords = ["chandelier", "pendant", "ثريا", "نجف"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "outdoor",
            NameEn = "outdoor lighting",
            NameAr = "الإنارة الخارجية",
            Terms = ["outdoor light", "outdoor lighting", "outdoor", "exterior", "garden light", "garden lighting",
                     "landscape", "facade", "bollard", "street light", "waterproof light",
                     "اناره خارجيه", "اضاءه خارجيه", "خارجي", "خارجيه", "حديقه", "اناره حديقه", "واجهه", "شارع"],
            CategoryHints = ["outdoor", "garden", "exterior", "landscape", "خارجي", "خارجيه", "حديقه"],
            SearchKeywords = ["outdoor", "garden", "خارجي"],
            IsLightFixture = true,
            //outdoor and facade lighting is conventionally cooler than interior lighting
            ColorTemperatureKelvin = 4000
        },
        new()
        {
            Slug = "villa",
            NameEn = "villa lighting",
            NameAr = "إنارة الفلل",
            Terms = ["villa", "villa lighting", "whole house", "full house", "home lighting package",
                     "فيلا", "فله", "اناره فيلا", "اضاءه فيلا", "منزل", "بيت كامل"],
            CategoryHints = ["villa", "indoor", "interior", "فيلا", "داخلي"],
            SearchKeywords = ["villa", "indoor", "فيلا"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "spotlight",
            NameEn = "spotlights",
            NameAr = "السبوت لايت",
            Terms = ["spotlight", "spotlights", "spot light", "spot lights", "spot", "downlight", "downlights",
                     "recessed", "gu10", "mr16", "cob",
                     "سبوت", "سبوت لايت", "سبوتات", "داون لايت", "غاطس", "غاطسه"],
            CategoryHints = ["spot", "downlight", "recessed", "سبوت", "داون"],
            SearchKeywords = ["spotlight", "spot", "downlight", "سبوت"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "switch",
            NameEn = "switches",
            NameAr = "المفاتيح",
            Terms = ["switch", "switches", "light switch", "dimmer", "toggle", "rocker",
                     "مفتاح", "مفاتيح", "مفتاح اناره", "دimmer", "ديمر"],
            CategoryHints = ["switch", "dimmer", "مفتاح", "مفاتيح"],
            SearchKeywords = ["switch", "مفتاح"]
        },
        new()
        {
            Slug = "socket",
            NameEn = "sockets",
            NameAr = "الأفياش والمقابس",
            Terms = ["socket", "sockets", "outlet", "outlets", "power point", "plug point", "wall socket",
                     "فيشه", "افياش", "مقبس", "مقابس", "بريزه", "بريز"],
            CategoryHints = ["socket", "outlet", "فيشه", "افياش", "مقبس", "مقابس", "بريزه"],
            SearchKeywords = ["socket", "outlet", "مقبس"]
        },
        new()
        {
            Slug = "floodlight",
            NameEn = "floodlights",
            NameAr = "الكشافات",
            Terms = ["floodlight", "floodlights", "flood light", "projector light", "high bay",
                     "كشاف", "كشافات", "بروجيكتور"],
            CategoryHints = ["flood", "projector", "high bay", "كشاف", "كشافات"],
            SearchKeywords = ["floodlight", "flood", "كشاف"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 5000
        },
        new()
        {
            Slug = "panel",
            NameEn = "panel lights",
            NameAr = "البانل",
            Terms = ["panel light", "panel lights", "panel", "ceiling panel", "flat panel",
                     "بانل", "بانلات", "لوحه اضاءه"],
            CategoryHints = ["panel", "بانل"],
            SearchKeywords = ["panel", "بانل"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 4000
        },
        new()
        {
            Slug = "wall-light",
            NameEn = "wall lights",
            NameAr = "الإنارة الجدارية",
            Terms = ["wall light", "wall lights", "wall lamp", "sconce", "applique",
                     "ابليك", "اباليك", "اناره جداريه", "جداري"],
            CategoryHints = ["wall", "sconce", "ابليك", "جداري"],
            SearchKeywords = ["wall light", "sconce", "ابليك"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 2700
        },
        new()
        {
            Slug = "ceiling-light",
            NameEn = "ceiling lights",
            NameAr = "إنارة السقف",
            Terms = ["ceiling light", "ceiling lights", "ceiling lamp", "flush mount",
                     "اناره سقف", "اضاءه سقف", "سقفيه", "نجفه سقف"],
            CategoryHints = ["ceiling", "سقف", "سقفيه"],
            SearchKeywords = ["ceiling light", "سقف"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 3000
        },
        new()
        {
            Slug = "bulb",
            NameEn = "bulbs",
            NameAr = "اللمبات",
            Terms = ["bulb", "bulbs", "lamp", "lamps", "e27", "e14", "globe",
                     "لمبه", "لمبات", "مصباح", "مصابيح"],
            CategoryHints = ["bulb", "lamp", "لمبه", "لمبات", "مصباح"],
            SearchKeywords = ["bulb", "lamp", "لمبه"],
            IsLightFixture = true,
            ColorTemperatureKelvin = 3000
        }
    ];

    /// <summary>
    /// Finds the need a message is about, or null when it is about none of them.
    /// </summary>
    /// <param name="normalizedMessage">A message already put through <see cref="TextNormalizer.Normalize"/></param>
    public static NeedProfile Match(string normalizedMessage)
    {
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return null;

        //ordered most-specific-first, so the first hit is the right one
        return All.FirstOrDefault(profile =>
            profile.Terms.Any(term => TextNormalizer.ContainsTerm(normalizedMessage, term)));
    }

    /// <summary>
    /// Finds a need by its slug.
    /// </summary>
    public static NeedProfile FindBySlug(string slug)
    {
        return All.FirstOrDefault(profile => string.Equals(profile.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
