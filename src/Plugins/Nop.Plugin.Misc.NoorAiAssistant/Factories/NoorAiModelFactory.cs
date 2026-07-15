using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Analytics;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Providers;
using Nop.Services.Configuration;
using Nop.Services.Helpers;
using Nop.Services.Localization;
using Nop.Services.Stores;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Models.Extensions;

namespace Nop.Plugin.Misc.NoorAiAssistant.Factories;

/// <summary>
/// Builds the admin view models.
/// </summary>
public partial class NoorAiModelFactory : INoorAiModelFactory
{
    #region Fields

    private readonly IChatAnalyticsService _chatAnalyticsService;
    private readonly IChatFaqService _chatFaqService;
    private readonly IDateTimeHelper _dateTimeHelper;
    private readonly IEnumerable<IChatProvider> _chatProviders;
    private readonly ILanguageService _languageService;
    private readonly ILocalizationService _localizationService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly IStoreService _storeService;
    private readonly ISuggestedQuestionService _suggestedQuestionService;

    #endregion

    #region Ctor

    public NoorAiModelFactory(IChatAnalyticsService chatAnalyticsService,
        IChatFaqService chatFaqService,
        IDateTimeHelper dateTimeHelper,
        IEnumerable<IChatProvider> chatProviders,
        ILanguageService languageService,
        ILocalizationService localizationService,
        ISettingService settingService,
        IStoreContext storeContext,
        IStoreService storeService,
        ISuggestedQuestionService suggestedQuestionService)
    {
        _chatAnalyticsService = chatAnalyticsService;
        _chatFaqService = chatFaqService;
        _dateTimeHelper = dateTimeHelper;
        _chatProviders = chatProviders;
        _languageService = languageService;
        _localizationService = localizationService;
        _settingService = settingService;
        _storeContext = storeContext;
        _storeService = storeService;
        _suggestedQuestionService = suggestedQuestionService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the "All" option used by the language and store dropdowns, where 0 means unscoped.
    /// </summary>
    protected virtual async Task<SelectListItem> AllOptionAsync()
    {
        return new SelectListItem
        {
            Text = await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Common.All"),
            Value = "0"
        };
    }

    protected virtual async Task<IList<SelectListItem>> PrepareLanguageOptionsAsync()
    {
        var options = new List<SelectListItem> { await AllOptionAsync() };

        var languages = await _languageService.GetAllLanguagesAsync(showHidden: true);
        options.AddRange(languages.Select(language => new SelectListItem
        {
            Text = language.Name,
            Value = language.Id.ToString()
        }));

        return options;
    }

    protected virtual async Task<IList<SelectListItem>> PrepareStoreOptionsAsync()
    {
        var options = new List<SelectListItem> { await AllOptionAsync() };

        var stores = await _storeService.GetAllStoresAsync();
        options.AddRange(stores.Select(store => new SelectListItem
        {
            Text = store.Name,
            Value = store.Id.ToString()
        }));

        return options;
    }

    /// <summary>
    /// Gets the FAQ topics, named from the enum so a new topic needs no locale work to show up.
    /// </summary>
    protected virtual IList<SelectListItem> PrepareTopicOptions(bool includeAll)
    {
        var options = new List<SelectListItem>();

        if (includeAll)
            options.Add(new SelectListItem { Text = "All", Value = "0" });

        options.AddRange(Enum.GetValues<FaqTopic>()
            .Where(topic => !includeAll || topic != FaqTopic.Other)
            .Select(topic => new SelectListItem
            {
                //"PaymentMethods" -> "Payment Methods"
                Text = System.Text.RegularExpressions.Regex.Replace(topic.ToString(), "(\\B[A-Z])", " $1"),
                Value = ((int)topic).ToString()
            }));

        return options;
    }

    /// <summary>
    /// Names a language id for the grid, where 0 means every language.
    /// </summary>
    protected virtual async Task<string> GetLanguageNameAsync(int languageId)
    {
        if (languageId == 0)
            return await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Common.All");

        var language = await _languageService.GetLanguageByIdAsync(languageId);

        return language?.Name ?? "—";
    }

    #endregion

    #region Methods

    /// <summary>
    /// Prepares the settings model
    /// </summary>
    public virtual async Task<ConfigurationModel> PrepareConfigurationModelAsync()
    {
        var storeId = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<NoorAiAssistantSettings>(storeId);

        var model = settings.ToSettingsModel<ConfigurationModel>();
        model.ActiveStoreScopeConfiguration = storeId;

        //every provider registered in DI shows up here; that is the whole "add a new AI provider" story
        foreach (var provider in _chatProviders)
        {
            model.AvailableProviders.Add(new SelectListItem
            {
                Text = provider.FriendlyName,
                Value = provider.SystemName,
                Selected = string.Equals(provider.SystemName, settings.ActiveProviderSystemName, StringComparison.OrdinalIgnoreCase)
            });
        }

        if (storeId <= 0)
            return model;

        model.Enabled_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.Enabled, storeId);
        model.ActiveProviderSystemName_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ActiveProviderSystemName, storeId);
        model.ThemeColor_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.ThemeColor, storeId);
        model.WelcomeMessage_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.WelcomeMessage, storeId);
        model.WelcomeMessageAr_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.WelcomeMessageAr, storeId);
        model.MaxHistoryMessages_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.MaxHistoryMessages, storeId);
        model.MaxProductsPerAnswer_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.MaxProductsPerAnswer, storeId);
        model.AllowProductRecommendation_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AllowProductRecommendation, storeId);
        model.AllowProductComparison_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AllowProductComparison, storeId);
        model.AllowQuantityEstimation_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.AllowQuantityEstimation, storeId);
        model.TrackAnalytics_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.TrackAnalytics, storeId);
        model.RateLimitMessages_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.RateLimitMessages, storeId);
        model.RateLimitWindowSeconds_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.RateLimitWindowSeconds, storeId);
        model.MaxMessageLength_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.MaxMessageLength, storeId);
        model.DefaultLuxTarget_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.DefaultLuxTarget, storeId);
        model.DefaultLumensPerWatt_OverrideForStore = await _settingService.SettingExistsAsync(settings, x => x.DefaultLumensPerWatt, storeId);

        return model;
    }

    /// <summary>
    /// Prepares the FAQ search model
    /// </summary>
    public virtual Task<FaqSearchModel> PrepareFaqSearchModelAsync(FaqSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.AvailableTopics = PrepareTopicOptions(includeAll: true);
        searchModel.SetGridPageSize();

        return Task.FromResult(searchModel);
    }

    /// <summary>
    /// Prepares a page of the FAQ grid
    /// </summary>
    public virtual async Task<FaqListModel> PrepareFaqListModelAsync(FaqSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var faqs = await _chatFaqService.GetAllFaqsAsync(
            keywords: searchModel.SearchKeywords,
            topicId: searchModel.SearchTopicId > 0 ? searchModel.SearchTopicId : null,
            pageIndex: searchModel.Page - 1,
            pageSize: searchModel.PageSize);

        return await new FaqListModel().PrepareToGridAsync(searchModel, faqs, () =>
            faqs.SelectAwait(async faq =>
            {
                var model = faq.ToModel<FaqModel>();
                model.TopicName = System.Text.RegularExpressions.Regex.Replace(faq.Topic.ToString(), "(\\B[A-Z])", " $1");
                model.LanguageName = await GetLanguageNameAsync(faq.LanguageId);

                return model;
            }));
    }

    /// <summary>
    /// Prepares the FAQ create/edit model
    /// </summary>
    public virtual async Task<FaqModel> PrepareFaqModelAsync(FaqModel model, ChatFaq faq)
    {
        if (faq is not null)
            model ??= faq.ToModel<FaqModel>();

        //a new entry starts published and English-first, which is what an admin almost always wants
        model ??= new FaqModel { Published = true };

        model.AvailableTopics = PrepareTopicOptions(includeAll: false);
        model.AvailableLanguages = await PrepareLanguageOptionsAsync();
        model.AvailableStores = await PrepareStoreOptionsAsync();

        return model;
    }

    /// <summary>
    /// Prepares the suggested-questions search model
    /// </summary>
    public virtual Task<SuggestedQuestionSearchModel> PrepareQuestionSearchModelAsync(SuggestedQuestionSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        searchModel.SetGridPageSize();

        return Task.FromResult(searchModel);
    }

    /// <summary>
    /// Prepares a page of the suggested-questions grid
    /// </summary>
    public virtual async Task<SuggestedQuestionListModel> PrepareQuestionListModelAsync(SuggestedQuestionSearchModel searchModel)
    {
        ArgumentNullException.ThrowIfNull(searchModel);

        var questions = await _suggestedQuestionService.GetAllQuestionsAsync(
            keywords: searchModel.SearchKeywords,
            pageIndex: searchModel.Page - 1,
            pageSize: searchModel.PageSize);

        return await new SuggestedQuestionListModel().PrepareToGridAsync(searchModel, questions, () =>
            questions.SelectAwait(async question =>
            {
                var model = question.ToModel<SuggestedQuestionModel>();
                model.LanguageName = await GetLanguageNameAsync(question.LanguageId);

                return model;
            }));
    }

    /// <summary>
    /// Prepares the suggested-question create/edit model
    /// </summary>
    public virtual async Task<SuggestedQuestionModel> PrepareQuestionModelAsync(SuggestedQuestionModel model, SuggestedQuestion question)
    {
        if (question is not null)
            model ??= question.ToModel<SuggestedQuestionModel>();

        model ??= new SuggestedQuestionModel { Published = true };

        model.AvailableLanguages = await PrepareLanguageOptionsAsync();
        model.AvailableStores = await PrepareStoreOptionsAsync();

        return model;
    }

    /// <summary>
    /// Prepares the analytics dashboard
    /// </summary>
    public virtual async Task<AnalyticsModel> PrepareAnalyticsModelAsync(AnalyticsModel model)
    {
        model ??= new AnalyticsModel();

        //default to the last 30 days rather than all time: an all-time report is dominated by whatever was
        //asked in the first week after launch and stops telling the owner anything about now
        model.FromUtc ??= DateTime.UtcNow.AddDays(-30);

        //the admin picks dates in their own time zone; the events are stamped in UTC
        var timeZone = await _dateTimeHelper.GetCurrentTimeZoneAsync();

        var fromUtc = model.FromUtc.HasValue
            ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.FromUtc.Value, timeZone)
            : null;

        var toUtc = model.ToUtc.HasValue
            //the "to" date is inclusive of that whole day, which is what an admin means when they pick it
            ? (DateTime?)_dateTimeHelper.ConvertToUtcTime(model.ToUtc.Value.AddDays(1), timeZone)
            : null;

        model.Summary = await _chatAnalyticsService.GetSummaryAsync(fromUtc, toUtc, model.StoreId);
        model.TopQuestions = await _chatAnalyticsService.GetTopQuestionsAsync(fromUtc, toUtc, model.StoreId);
        model.TopKeywords = await _chatAnalyticsService.GetTopKeywordsAsync(fromUtc, toUtc, model.StoreId);
        model.TopRecommended = await _chatAnalyticsService.GetTopProductsAsync(ChatEventType.ProductRecommended, fromUtc, toUtc, model.StoreId);
        model.TopViewed = await _chatAnalyticsService.GetTopProductsAsync(ChatEventType.ProductViewed, fromUtc, toUtc, model.StoreId);

        model.AvailableStores = (await PrepareStoreOptionsAsync()).ToList();

        return model;
    }

    #endregion
}
