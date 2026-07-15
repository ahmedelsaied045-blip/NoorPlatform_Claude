using Nop.Core.Domain.Cms;
using Nop.Plugin.Misc.NoorAiAssistant.Components;
using Nop.Plugin.Misc.NoorAiAssistant.Data;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;
using Nop.Services.Cms;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Stores;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.NoorAiAssistant;

/// <summary>
/// The Noor AI sales assistant.
/// </summary>
/// <remarks>
/// A widget plugin rather than a theme change: it renders into the BodyEndHtmlTagBefore zone, which the
/// base _Root.cshtml emits on every storefront page. NoorTheme1 does not override _Root.cshtml, so the
/// assistant appears site-wide without a single edit to the theme — and survives a theme change.
/// </remarks>
public class NoorAiAssistantPlugin : BasePlugin, IWidgetPlugin
{
    #region Fields

    private readonly IChatFaqService _chatFaqService;
    private readonly ILanguageService _languageService;
    private readonly ILocalizationService _localizationService;
    private readonly INopUrlHelper _nopUrlHelper;
    private readonly ISettingService _settingService;
    private readonly IStoreService _storeService;
    private readonly ISuggestedQuestionService _suggestedQuestionService;
    private readonly WidgetSettings _widgetSettings;

    #endregion

    #region Ctor

    public NoorAiAssistantPlugin(IChatFaqService chatFaqService,
        ILanguageService languageService,
        ILocalizationService localizationService,
        INopUrlHelper nopUrlHelper,
        ISettingService settingService,
        IStoreService storeService,
        ISuggestedQuestionService suggestedQuestionService,
        WidgetSettings widgetSettings)
    {
        _chatFaqService = chatFaqService;
        _languageService = languageService;
        _localizationService = localizationService;
        _nopUrlHelper = nopUrlHelper;
        _settingService = settingService;
        _storeService = storeService;
        _suggestedQuestionService = suggestedQuestionService;
        _widgetSettings = widgetSettings;
    }

    #endregion

    #region Properties

    /// <summary>
    /// The assistant is not something a store owner would want to place in an arbitrary widget zone, so it
    /// is hidden from the widget list; it is switched on and off from its own settings page instead.
    /// </summary>
    public bool HideInWidgetList => true;

    #endregion

    #region Methods

    /// <summary>
    /// Gets a configuration page URL
    /// </summary>
    public override string GetConfigurationPageUrl()
    {
        return _nopUrlHelper.RouteUrl(NoorAiAssistantDefaults.CONFIGURE_ROUTE_NAME);
    }

    /// <summary>
    /// Gets widget zones where this widget should be rendered
    /// </summary>
    public Task<IList<string>> GetWidgetZonesAsync()
    {
        return Task.FromResult<IList<string>>([PublicWidgetZones.BodyEndHtmlTagBefore]);
    }

    /// <summary>
    /// Gets a type of a view component for displaying widget
    /// </summary>
    /// <param name="widgetZone">Name of the widget zone</param>
    public Type GetWidgetViewComponent(string widgetZone)
    {
        ArgumentNullException.ThrowIfNull(widgetZone);

        return typeof(NoorAiAssistantViewComponent);
    }

    /// <summary>
    /// Install plugin
    /// </summary>
    public override async Task InstallAsync()
    {
        await _settingService.SaveSettingAsync(new NoorAiAssistantSettings
        {
            Enabled = true,
            ActiveProviderSystemName = "Dummy",
            //the theme's gold accent, so the launcher looks like it belongs to Noor out of the box
            ThemeColor = "#C9A227",
            WelcomeMessage = "Hello! I'm the Noor lighting assistant. Tell me what you're looking for, or give me your room size and I'll work out exactly what you need.",
            WelcomeMessageAr = "أهلًا بك! أنا مساعد نور للإنارة. أخبرني بما تبحث عنه، أو أعطني مقاس غرفتك وسأحسب لك ما تحتاجه بالضبط.",
            MaxHistoryMessages = 20,
            MaxProductsPerAnswer = 3,
            AllowProductRecommendation = true,
            AllowProductComparison = true,
            AllowQuantityEstimation = true,
            TrackAnalytics = true,
            RateLimitMessages = 20,
            RateLimitWindowSeconds = 60,
            MaxMessageLength = 1000,
            DefaultLuxTarget = 150,
            DefaultLumensPerWatt = 100
        });

        //render the widget without making the store owner hunt for the widget list first
        if (!_widgetSettings.ActiveWidgetSystemNames.Contains(NoorAiAssistantDefaults.SYSTEM_NAME))
        {
            _widgetSettings.ActiveWidgetSystemNames.Add(NoorAiAssistantDefaults.SYSTEM_NAME);
            await _settingService.SaveSettingAsync(_widgetSettings);
        }

        await SeedKnowledgeBaseAsync();

        await _localizationService.AddOrUpdateLocaleResourceAsync(NoorAiLocaleResources.All);

        await base.InstallAsync();
    }

    /// <summary>
    /// Uninstall plugin
    /// </summary>
    public override async Task UninstallAsync()
    {
        await _settingService.DeleteSettingAsync<NoorAiAssistantSettings>();

        var stores = await _storeService.GetAllStoresAsync();
        var storeIds = new List<int> { 0 }.Union(stores.Select(store => store.Id));
        foreach (var storeId in storeIds)
        {
            var widgetSettings = await _settingService.LoadSettingAsync<WidgetSettings>(storeId);
            widgetSettings.ActiveWidgetSystemNames.Remove(NoorAiAssistantDefaults.SYSTEM_NAME);
            await _settingService.SaveSettingAsync(widgetSettings, storeId: storeId);
        }

        await _localizationService.DeleteLocaleResourcesAsync(NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX);

        //the tables themselves are dropped by the schema migration being reversed, so the FAQ entries and
        //conversations go with them — no explicit cleanup needed here

        await base.UninstallAsync();
    }

    /// <summary>
    /// Writes the starter FAQ entries and suggested questions, in both languages, so the assistant can
    /// answer something useful the moment it is installed rather than needing a data-entry session first.
    /// </summary>
    protected virtual async Task SeedKnowledgeBaseAsync()
    {
        //a re-install must not double the knowledge base
        var existing = await _chatFaqService.GetAllFaqsAsync(pageIndex: 0, pageSize: 1);
        if (existing.TotalCount > 0)
            return;

        //seed against the languages this store actually has. Tagging every entry as "all languages" would
        //show Arabic answers to English shoppers and vice versa; a store with no Arabic installed simply
        //gets no Arabic entries.
        var languages = await _languageService.GetAllLanguagesAsync(showHidden: true);

        int LanguageIdFor(string prefix) => languages
            .FirstOrDefault(language => language.LanguageCulture.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;

        var englishId = LanguageIdFor("en");
        var arabicId = LanguageIdFor("ar");

        foreach (var faq in NoorAiSeedData.Faqs(englishId, arabicId))
            await _chatFaqService.InsertFaqAsync(faq);

        foreach (var question in NoorAiSeedData.SuggestedQuestions(englishId, arabicId))
            await _suggestedQuestionService.InsertQuestionAsync(question);
    }

    #endregion
}
