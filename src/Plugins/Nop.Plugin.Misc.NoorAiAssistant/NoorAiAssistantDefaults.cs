using Nop.Core.Caching;

namespace Nop.Plugin.Misc.NoorAiAssistant;

/// <summary>
/// Represents plugin constants
/// </summary>
public static class NoorAiAssistantDefaults
{
    /// <summary>
    /// Gets the plugin system name
    /// </summary>
    public const string SYSTEM_NAME = "Misc.NoorAiAssistant";

    /// <summary>
    /// Gets the prefix of every locale resource this plugin owns, so uninstall can remove them in one call
    /// </summary>
    public const string LOCALE_RESOURCE_PREFIX = "Plugins.Misc.NoorAiAssistant";

    /// <summary>
    /// Gets the name of the cookie holding the current conversation for guests
    /// </summary>
    public const string SESSION_COOKIE_NAME = ".Nop.NoorAi.Session";

    #region Route names

    public const string CONFIGURE_ROUTE_NAME = "Plugin.Misc.NoorAiAssistant.Configure";
    public const string API_ROUTE_PREFIX = "api/chat";

    #endregion

    #region Admin menu

    public const string ADMIN_MENU_ROOT_SYSTEM_NAME = "Nop.Plugin.Misc.NoorAiAssistant.Root";
    public const string ADMIN_MENU_SETTINGS_SYSTEM_NAME = "Nop.Plugin.Misc.NoorAiAssistant.Settings";
    public const string ADMIN_MENU_FAQ_SYSTEM_NAME = "Nop.Plugin.Misc.NoorAiAssistant.Faq";
    public const string ADMIN_MENU_QUESTIONS_SYSTEM_NAME = "Nop.Plugin.Misc.NoorAiAssistant.SuggestedQuestions";
    public const string ADMIN_MENU_ANALYTICS_SYSTEM_NAME = "Nop.Plugin.Misc.NoorAiAssistant.Analytics";

    #endregion

    #region Caching

    /// <summary>
    /// Gets a key for caching the published FAQ entries. Not keyed by language: the matcher needs every
    /// language in scope so it can answer in the one the shopper actually typed.
    /// </summary>
    /// <remarks>
    /// {0} : store id
    /// </remarks>
    public static CacheKey FaqAllCacheKey => new("Nop.Plugin.Misc.NoorAiAssistant.faq.all.{0}");

    /// <summary>
    /// Gets the prefix every FAQ cache key starts with, so one RemoveByPrefixAsync clears the lot on edit
    /// </summary>
    public static string FaqPrefix => "Nop.Plugin.Misc.NoorAiAssistant.faq.";

    /// <summary>
    /// Gets a key for caching the published suggested questions
    /// </summary>
    /// <remarks>
    /// {0} : store id
    /// {1} : language id
    /// </remarks>
    public static CacheKey SuggestedQuestionsCacheKey => new("Nop.Plugin.Misc.NoorAiAssistant.suggestion.all.{0}-{1}");

    public static string SuggestedQuestionsPrefix => "Nop.Plugin.Misc.NoorAiAssistant.suggestion.";

    #endregion

    #region Rate limiting

    /// <summary>
    /// Gets a key for the per-customer request counter
    /// </summary>
    /// <remarks>
    /// {0} : customer id
    /// </remarks>
    public static CacheKey RateLimitCacheKey => new("Nop.Plugin.Misc.NoorAiAssistant.ratelimit.{0}");

    #endregion
}
