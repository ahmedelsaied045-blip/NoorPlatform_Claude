using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Misc.NoorAiAssistant.Components;

/// <summary>
/// Renders the chat window into the body-end widget zone of every storefront page.
/// </summary>
public class NoorAiAssistantViewComponent : NopViewComponent
{
    #region Fields

    private readonly IWorkContext _workContext;
    private readonly NoorAiAssistantSettings _settings;

    #endregion

    #region Ctor

    public NoorAiAssistantViewComponent(IWorkContext workContext,
        NoorAiAssistantSettings settings)
    {
        _workContext = workContext;
        _settings = settings;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Invoke view component
    /// </summary>
    /// <param name="widgetZone">Widget zone name</param>
    /// <param name="additionalData">Additional data</param>
    public async Task<IViewComponentResult> InvokeAsync(string widgetZone, object additionalData)
    {
        //a disabled assistant renders nothing at all: no markup, no stylesheet, no script. Hiding it with
        //CSS would still cost every visitor the download.
        if (!_settings.Enabled)
            return Content(string.Empty);

        var language = await _workContext.GetWorkingLanguageAsync();

        var model = new NoorAiWidgetModel
        {
            ThemeColor = !string.IsNullOrWhiteSpace(_settings.ThemeColor) ? _settings.ThemeColor : "#C9A227",
            IsRtl = language.Rtl
        };

        return View("~/Plugins/Misc.NoorAiAssistant/Views/Components/NoorAiAssistant.cshtml", model);
    }

    #endregion
}
