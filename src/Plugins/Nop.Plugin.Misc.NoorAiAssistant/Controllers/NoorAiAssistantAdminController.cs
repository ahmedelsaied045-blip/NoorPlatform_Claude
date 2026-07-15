using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Factories;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;
using Nop.Plugin.Misc.NoorAiAssistant.Services;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Web.Areas.Admin.Controllers;
using Nop.Web.Areas.Admin.Infrastructure.Mapper.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Misc.NoorAiAssistant.Controllers;

/// <summary>
/// The admin area: settings, FAQ, suggested questions and analytics.
/// </summary>
/// <remarks>
/// BaseAdminController already carries [AuthorizeAdmin], [Area(Admin)] and antiforgery validation, so each
/// action only has to declare the permission it needs. Managing the knowledge base and reading the reports
/// are separate permissions, so a marketing role can be given the analytics without also being able to
/// rewrite the FAQ.
/// </remarks>
public class NoorAiAssistantAdminController : BaseAdminController
{
    #region Fields

    private readonly IChatFaqService _chatFaqService;
    private readonly ILocalizationService _localizationService;
    private readonly INoorAiModelFactory _modelFactory;
    private readonly INotificationService _notificationService;
    private readonly ISettingService _settingService;
    private readonly IStoreContext _storeContext;
    private readonly ISuggestedQuestionService _suggestedQuestionService;

    #endregion

    #region Ctor

    public NoorAiAssistantAdminController(IChatFaqService chatFaqService,
        ILocalizationService localizationService,
        INoorAiModelFactory modelFactory,
        INotificationService notificationService,
        ISettingService settingService,
        IStoreContext storeContext,
        ISuggestedQuestionService suggestedQuestionService)
    {
        _chatFaqService = chatFaqService;
        _localizationService = localizationService;
        _modelFactory = modelFactory;
        _notificationService = notificationService;
        _settingService = settingService;
        _storeContext = storeContext;
        _suggestedQuestionService = suggestedQuestionService;
    }

    #endregion

    #region Settings

    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> Configure()
    {
        var model = await _modelFactory.PrepareConfigurationModelAsync();

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/Configure.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
        var settings = await _settingService.LoadSettingAsync<NoorAiAssistantSettings>(storeScope);

        settings = model.ToSettings(settings);

        //clearCache: false on each, then one flush at the end — sixteen cache clears would be sixteen
        //round trips to Redis for no benefit
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.Enabled, model.Enabled_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.ActiveProviderSystemName, model.ActiveProviderSystemName_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.ThemeColor, model.ThemeColor_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.WelcomeMessage, model.WelcomeMessage_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.WelcomeMessageAr, model.WelcomeMessageAr_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.MaxHistoryMessages, model.MaxHistoryMessages_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.MaxProductsPerAnswer, model.MaxProductsPerAnswer_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.AllowProductRecommendation, model.AllowProductRecommendation_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.AllowProductComparison, model.AllowProductComparison_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.AllowQuantityEstimation, model.AllowQuantityEstimation_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.TrackAnalytics, model.TrackAnalytics_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.RateLimitMessages, model.RateLimitMessages_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.RateLimitWindowSeconds, model.RateLimitWindowSeconds_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.MaxMessageLength, model.MaxMessageLength_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.DefaultLuxTarget, model.DefaultLuxTarget_OverrideForStore, storeScope, false);
        await _settingService.SaveSettingOverridablePerStoreAsync(settings, x => x.DefaultLumensPerWatt, model.DefaultLumensPerWatt_OverrideForStore, storeScope, false);

        await _settingService.ClearCacheAsync();

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Saved"));

        return RedirectToAction(nameof(Configure));
    }

    #endregion

    #region FAQ

    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> FaqList()
    {
        var model = await _modelFactory.PrepareFaqSearchModelAsync(new FaqSearchModel());

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/FaqList.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> FaqList(FaqSearchModel searchModel)
    {
        var model = await _modelFactory.PrepareFaqListModelAsync(searchModel);

        return Json(model);
    }

    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> FaqCreate()
    {
        var model = await _modelFactory.PrepareFaqModelAsync(null, null);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/FaqCreate.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> FaqCreate(FaqModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var faq = model.ToEntity<ChatFaq>();
            await _chatFaqService.InsertFaqAsync(faq);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Faq.Added"));

            return continueEditing
                ? RedirectToAction(nameof(FaqEdit), new { id = faq.Id })
                : RedirectToAction(nameof(FaqList));
        }

        model = await _modelFactory.PrepareFaqModelAsync(model, null);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/FaqCreate.cshtml", model);
    }

    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> FaqEdit(int id)
    {
        var faq = await _chatFaqService.GetFaqByIdAsync(id);
        if (faq is null)
            return RedirectToAction(nameof(FaqList));

        var model = await _modelFactory.PrepareFaqModelAsync(null, faq);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/FaqEdit.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> FaqEdit(FaqModel model, bool continueEditing)
    {
        var faq = await _chatFaqService.GetFaqByIdAsync(model.Id);
        if (faq is null)
            return RedirectToAction(nameof(FaqList));

        if (ModelState.IsValid)
        {
            faq = model.ToEntity(faq);
            await _chatFaqService.UpdateFaqAsync(faq);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Faq.Updated"));

            return continueEditing
                ? RedirectToAction(nameof(FaqEdit), new { id = faq.Id })
                : RedirectToAction(nameof(FaqList));
        }

        model = await _modelFactory.PrepareFaqModelAsync(model, faq);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/FaqEdit.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> FaqDelete(int id)
    {
        var faq = await _chatFaqService.GetFaqByIdAsync(id);
        if (faq is null)
            return RedirectToAction(nameof(FaqList));

        await _chatFaqService.DeleteFaqAsync(faq);

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.Faq.Deleted"));

        return RedirectToAction(nameof(FaqList));
    }

    #endregion

    #region Suggested questions

    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> QuestionList()
    {
        var model = await _modelFactory.PrepareQuestionSearchModelAsync(new SuggestedQuestionSearchModel());

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/QuestionList.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> QuestionList(SuggestedQuestionSearchModel searchModel)
    {
        var model = await _modelFactory.PrepareQuestionListModelAsync(searchModel);

        return Json(model);
    }

    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> QuestionCreate()
    {
        var model = await _modelFactory.PrepareQuestionModelAsync(null, null);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/QuestionCreate.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> QuestionCreate(SuggestedQuestionModel model, bool continueEditing)
    {
        if (ModelState.IsValid)
        {
            var question = model.ToEntity<SuggestedQuestion>();
            await _suggestedQuestionService.InsertQuestionAsync(question);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.SuggestedQuestions.Added"));

            return continueEditing
                ? RedirectToAction(nameof(QuestionEdit), new { id = question.Id })
                : RedirectToAction(nameof(QuestionList));
        }

        model = await _modelFactory.PrepareQuestionModelAsync(model, null);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/QuestionCreate.cshtml", model);
    }

    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> QuestionEdit(int id)
    {
        var question = await _suggestedQuestionService.GetQuestionByIdAsync(id);
        if (question is null)
            return RedirectToAction(nameof(QuestionList));

        var model = await _modelFactory.PrepareQuestionModelAsync(null, question);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/QuestionEdit.cshtml", model);
    }

    [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> QuestionEdit(SuggestedQuestionModel model, bool continueEditing)
    {
        var question = await _suggestedQuestionService.GetQuestionByIdAsync(model.Id);
        if (question is null)
            return RedirectToAction(nameof(QuestionList));

        if (ModelState.IsValid)
        {
            question = model.ToEntity(question);
            await _suggestedQuestionService.UpdateQuestionAsync(question);

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.SuggestedQuestions.Updated"));

            return continueEditing
                ? RedirectToAction(nameof(QuestionEdit), new { id = question.Id })
                : RedirectToAction(nameof(QuestionList));
        }

        model = await _modelFactory.PrepareQuestionModelAsync(model, question);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/QuestionEdit.cshtml", model);
    }

    [HttpPost]
    [CheckPermission(NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT)]
    public virtual async Task<IActionResult> QuestionDelete(int id)
    {
        var question = await _suggestedQuestionService.GetQuestionByIdAsync(id);
        if (question is null)
            return RedirectToAction(nameof(QuestionList));

        await _suggestedQuestionService.DeleteQuestionAsync(question);

        _notificationService.SuccessNotification(
            await _localizationService.GetResourceAsync($"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.SuggestedQuestions.Deleted"));

        return RedirectToAction(nameof(QuestionList));
    }

    #endregion

    #region Analytics

    [CheckPermission(NoorAiPermissionConfigManager.VIEW_AI_ANALYTICS)]
    public virtual async Task<IActionResult> Analytics(AnalyticsModel model)
    {
        model = await _modelFactory.PrepareAnalyticsModelAsync(model);

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Admin/Analytics.cshtml", model);
    }

    #endregion
}
