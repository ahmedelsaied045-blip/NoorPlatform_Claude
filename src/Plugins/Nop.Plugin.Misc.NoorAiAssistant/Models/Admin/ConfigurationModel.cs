using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;

/// <summary>
/// The AI Assistant settings page.
/// </summary>
public record ConfigurationModel : BaseNopModel, ISettingsModel
{
    #region Ctor

    public ConfigurationModel()
    {
        AvailableProviders = new List<SelectListItem>();
    }

    #endregion

    #region Properties

    public int ActiveStoreScopeConfiguration { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.Enabled")]
    public bool Enabled { get; set; }
    public bool Enabled_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.ActiveProviderSystemName")]
    public string ActiveProviderSystemName { get; set; }
    public bool ActiveProviderSystemName_OverrideForStore { get; set; }

    /// <summary>
    /// Gets or sets every IChatProvider currently registered. Today that is the built-in one; the moment an
    /// OpenAI provider is added to DI it appears here with no change to this page.
    /// </summary>
    public IList<SelectListItem> AvailableProviders { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.ThemeColor")]
    public string ThemeColor { get; set; }
    public bool ThemeColor_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.WelcomeMessage")]
    public string WelcomeMessage { get; set; }
    public bool WelcomeMessage_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.WelcomeMessageAr")]
    public string WelcomeMessageAr { get; set; }
    public bool WelcomeMessageAr_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.MaxHistoryMessages")]
    public int MaxHistoryMessages { get; set; }
    public bool MaxHistoryMessages_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.MaxProductsPerAnswer")]
    public int MaxProductsPerAnswer { get; set; }
    public bool MaxProductsPerAnswer_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.AllowProductRecommendation")]
    public bool AllowProductRecommendation { get; set; }
    public bool AllowProductRecommendation_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.AllowProductComparison")]
    public bool AllowProductComparison { get; set; }
    public bool AllowProductComparison_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.AllowQuantityEstimation")]
    public bool AllowQuantityEstimation { get; set; }
    public bool AllowQuantityEstimation_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.TrackAnalytics")]
    public bool TrackAnalytics { get; set; }
    public bool TrackAnalytics_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.RateLimitMessages")]
    public int RateLimitMessages { get; set; }
    public bool RateLimitMessages_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.RateLimitWindowSeconds")]
    public int RateLimitWindowSeconds { get; set; }
    public bool RateLimitWindowSeconds_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.MaxMessageLength")]
    public int MaxMessageLength { get; set; }
    public bool MaxMessageLength_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.DefaultLuxTarget")]
    public int DefaultLuxTarget { get; set; }
    public bool DefaultLuxTarget_OverrideForStore { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Fields.DefaultLumensPerWatt")]
    public int DefaultLumensPerWatt { get; set; }
    public bool DefaultLumensPerWatt_OverrideForStore { get; set; }

    #endregion
}
