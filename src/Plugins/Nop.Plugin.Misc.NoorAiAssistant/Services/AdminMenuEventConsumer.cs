using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Menu;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services;

/// <summary>
/// Adds the "AI Assistant" section to the admin menu.
/// </summary>
/// <remarks>
/// Done by consuming AdminMenuCreatedEvent rather than by editing AdminMenu.cs, which keeps the promise
/// that this plugin touches no nopCommerce file. The event consumer is discovered automatically — no DI
/// registration needed.
/// </remarks>
public class AdminMenuEventConsumer : IConsumer<AdminMenuCreatedEvent>
{
    #region Fields

    private readonly ILocalizationService _localizationService;

    #endregion

    #region Ctor

    public AdminMenuEventConsumer(ILocalizationService localizationService)
    {
        _localizationService = localizationService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Handle the admin menu created event
    /// </summary>
    /// <param name="eventMessage">Event message</param>
    public async Task HandleEventAsync(AdminMenuCreatedEvent eventMessage)
    {
        var configuration = eventMessage.RootMenuItem.GetItemBySystemName("Configuration");
        if (configuration is null)
            return;

        string Resource(string key) => $"{NoorAiAssistantDefaults.LOCALE_RESOURCE_PREFIX}.{key}";

        var root = new AdminMenuItem
        {
            Visible = true,
            SystemName = NoorAiAssistantDefaults.ADMIN_MENU_ROOT_SYSTEM_NAME,
            Title = await _localizationService.GetResourceAsync(Resource("Menu.Root")),
            IconClass = "far fa-dot-circle",
            //visible to anyone who holds either permission: an analytics-only role still needs to see the
            //parent in order to reach the report underneath it
            PermissionNames =
            [
                NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT,
                NoorAiPermissionConfigManager.VIEW_AI_ANALYTICS
            ],
            ChildNodes =
            [
                new AdminMenuItem
                {
                    Visible = true,
                    SystemName = NoorAiAssistantDefaults.ADMIN_MENU_SETTINGS_SYSTEM_NAME,
                    Title = await _localizationService.GetResourceAsync(Resource("Menu.Settings")),
                    Url = eventMessage.GetMenuItemUrl("NoorAiAssistantAdmin", "Configure"),
                    IconClass = "far fa-circle",
                    PermissionNames = [NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT]
                },
                new AdminMenuItem
                {
                    Visible = true,
                    SystemName = NoorAiAssistantDefaults.ADMIN_MENU_FAQ_SYSTEM_NAME,
                    Title = await _localizationService.GetResourceAsync(Resource("Menu.Faq")),
                    Url = eventMessage.GetMenuItemUrl("NoorAiAssistantAdmin", "FaqList"),
                    IconClass = "far fa-circle",
                    PermissionNames = [NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT]
                },
                new AdminMenuItem
                {
                    Visible = true,
                    SystemName = NoorAiAssistantDefaults.ADMIN_MENU_QUESTIONS_SYSTEM_NAME,
                    Title = await _localizationService.GetResourceAsync(Resource("Menu.SuggestedQuestions")),
                    Url = eventMessage.GetMenuItemUrl("NoorAiAssistantAdmin", "QuestionList"),
                    IconClass = "far fa-circle",
                    PermissionNames = [NoorAiPermissionConfigManager.MANAGE_AI_ASSISTANT]
                },
                new AdminMenuItem
                {
                    Visible = true,
                    SystemName = NoorAiAssistantDefaults.ADMIN_MENU_ANALYTICS_SYSTEM_NAME,
                    Title = await _localizationService.GetResourceAsync(Resource("Menu.Analytics")),
                    Url = eventMessage.GetMenuItemUrl("NoorAiAssistantAdmin", "Analytics"),
                    IconClass = "far fa-circle",
                    PermissionNames = [NoorAiPermissionConfigManager.VIEW_AI_ANALYTICS]
                }
            ]
        };

        configuration.ChildNodes.Add(root);
    }

    #endregion
}
