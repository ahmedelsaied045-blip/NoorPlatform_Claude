namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents what the shopper is trying to do. Resolved by the intent engine and persisted on the
/// message so analytics can report on it without re-parsing the text.
/// </summary>
public enum ChatIntent
{
    /// <summary>
    /// Nothing matched; fall back to a generic reply.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// "hi", "مرحبا"
    /// </summary>
    Greeting = 10,

    /// <summary>
    /// Free-text product lookup: "do you have GU10 spotlights?"
    /// </summary>
    ProductSearch = 20,

    /// <summary>
    /// Needs-based lookup: "I need a chandelier", "أحتاج إنارة خارجية"
    /// </summary>
    ProductRecommendation = 30,

    /// <summary>
    /// "compare A and B"
    /// </summary>
    ProductComparison = 40,

    /// <summary>
    /// "how many spotlights do I need for a room?"
    /// </summary>
    QuantityEstimation = 50,

    /// <summary>
    /// "my room is 5 x 4" — full lighting plan (count, wattage, colour temperature, category).
    /// </summary>
    LightingPlan = 60,

    /// <summary>
    /// Shipping, returns, warranty, payment, branches, working hours, ...
    /// </summary>
    Faq = 70,

    /// <summary>
    /// "thanks", "شكرا"
    /// </summary>
    SmallTalk = 80
}
