using Nop.Core.Configuration;

namespace Nop.Plugin.Misc.NoorAiAssistant;

/// <summary>
/// Represents the plugin settings. This is the "ChatSettings" store — nopCommerce persists ISettings in
/// the core Setting table (key "nooraiassistantsettings.*"), which gives per-store overrides and cache
/// invalidation for free, so a dedicated table would only duplicate that machinery.
/// </summary>
public class NoorAiAssistantSettings : ISettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the assistant is shown on the storefront.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the system name of the <see cref="Services.Providers.IChatProvider"/> that answers
    /// questions. Swapping this to "OpenAI" once such a provider exists is the entire migration path off
    /// the dummy provider.
    /// </summary>
    public string ActiveProviderSystemName { get; set; }

    /// <summary>
    /// Gets or sets the accent colour of the launcher and the shopper's message bubbles.
    /// </summary>
    public string ThemeColor { get; set; }

    /// <summary>
    /// Gets or sets the greeting shown in an empty conversation (English).
    /// </summary>
    public string WelcomeMessage { get; set; }

    /// <summary>
    /// Gets or sets the greeting shown in an empty conversation (Arabic).
    /// </summary>
    public string WelcomeMessageAr { get; set; }

    /// <summary>
    /// Gets or sets how many past messages are replayed into a conversation and handed to the provider.
    /// </summary>
    public int MaxHistoryMessages { get; set; }

    /// <summary>
    /// Gets or sets how many product cards a single answer may contain.
    /// </summary>
    public int MaxProductsPerAnswer { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the assistant may recommend products.
    /// </summary>
    public bool AllowProductRecommendation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the assistant may compare two products.
    /// </summary>
    public bool AllowProductComparison { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the assistant may estimate quantities and produce lighting
    /// plans.
    /// </summary>
    public bool AllowQuantityEstimation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether analytics facts are recorded.
    /// </summary>
    public bool TrackAnalytics { get; set; }

    /// <summary>
    /// Gets or sets how many messages one customer may send within <see cref="RateLimitWindowSeconds"/>.
    /// </summary>
    public int RateLimitMessages { get; set; }

    /// <summary>
    /// Gets or sets the rate limit window, in seconds.
    /// </summary>
    public int RateLimitWindowSeconds { get; set; }

    /// <summary>
    /// Gets or sets the longest message a shopper may send, in characters.
    /// </summary>
    public int MaxMessageLength { get; set; }

    /// <summary>
    /// Gets or sets the illuminance target used by the lighting planner, in lux. 150 lux suits a living
    /// room; a workshop would want 300+.
    /// </summary>
    public int DefaultLuxTarget { get; set; }

    /// <summary>
    /// Gets or sets the assumed efficacy of the fixtures sold by the store, in lumens per watt. Modern LED
    /// spotlights land around 90-110 lm/W.
    /// </summary>
    public int DefaultLumensPerWatt { get; set; }
}
