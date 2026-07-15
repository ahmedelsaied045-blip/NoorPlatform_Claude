using Nop.Core.Domain.Customers;
using Nop.Services.Security;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services;

/// <summary>
/// Declares the plugin's permissions. nopCommerce discovers every IPermissionConfigManager at startup and
/// seeds the missing PermissionRecord rows, so nothing here needs registering.
/// </summary>
public class NoorAiPermissionConfigManager : IPermissionConfigManager
{
    /// <summary>
    /// Manage settings, FAQ entries and suggested questions
    /// </summary>
    public const string MANAGE_AI_ASSISTANT = "Misc.NoorAiAssistant.Manage";

    /// <summary>
    /// Read the analytics reports. Split from MANAGE so a marketing role can see the reports without being
    /// able to rewrite the FAQ.
    /// </summary>
    public const string VIEW_AI_ANALYTICS = "Misc.NoorAiAssistant.ViewAnalytics";

    public IList<PermissionConfig> AllConfigs => new List<PermissionConfig>
    {
        new("Admin area. Manage the Noor AI sales assistant",
            MANAGE_AI_ASSISTANT,
            nameof(StandardPermission.Configuration),
            NopCustomerDefaults.AdministratorsRoleName),

        new("Admin area. View the Noor AI sales assistant analytics",
            VIEW_AI_ANALYTICS,
            nameof(StandardPermission.Configuration),
            NopCustomerDefaults.AdministratorsRoleName)
    };
}
