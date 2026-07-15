using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.NoorAiAssistant.Infrastructure;

/// <summary>
/// Maps the plugin's routes.
/// </summary>
/// <remarks>
/// The API sits at a fixed /api/chat/... path with no language SEO prefix. That is deliberate: the chat
/// window is on every page of the site, and a language-prefixed API would mean the browser had to know
/// which prefix the current page is under before it could call it. The working language is taken from the
/// request context server-side instead.
///
/// /api/chat/history is mapped twice — once for GET and once for DELETE. Both endpoints share the path;
/// the [HttpGet] / [HttpDelete] attributes on the actions are what routing uses to tell them apart.
/// </remarks>
public class RouteProvider : IRouteProvider
{
    /// <summary>
    /// Register routes
    /// </summary>
    /// <param name="endpointRouteBuilder">Route builder</param>
    public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
    {
        var prefix = NoorAiAssistantDefaults.API_ROUTE_PREFIX;

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Misc.NoorAiAssistant.Send",
            pattern: $"{prefix}/send",
            defaults: new { controller = "Chat", action = "Send" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Misc.NoorAiAssistant.History",
            pattern: $"{prefix}/history",
            defaults: new { controller = "Chat", action = "History" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Misc.NoorAiAssistant.ClearHistory",
            pattern: $"{prefix}/history",
            defaults: new { controller = "Chat", action = "ClearHistory" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Misc.NoorAiAssistant.Suggestions",
            pattern: $"{prefix}/suggestions",
            defaults: new { controller = "Chat", action = "Suggestions" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Misc.NoorAiAssistant.SearchProducts",
            pattern: $"{prefix}/search-products",
            defaults: new { controller = "Chat", action = "SearchProducts" });

        endpointRouteBuilder.MapControllerRoute(
            name: "Plugin.Misc.NoorAiAssistant.TrackView",
            pattern: $"{prefix}/track-view",
            defaults: new { controller = "Chat", action = "TrackView" });

        //admin
        endpointRouteBuilder.MapControllerRoute(
            name: NoorAiAssistantDefaults.CONFIGURE_ROUTE_NAME,
            pattern: "Admin/NoorAiAssistant/Configure",
            defaults: new { controller = "NoorAiAssistantAdmin", action = "Configure", area = AreaNames.ADMIN });
    }

    /// <summary>
    /// Gets a priority of route provider
    /// </summary>
    public int Priority => 0;
}
