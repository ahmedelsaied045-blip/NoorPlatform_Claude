using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Analytics;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;

/// <summary>
/// The analytics dashboard.
/// </summary>
public record AnalyticsModel : BaseNopModel
{
    public AnalyticsModel()
    {
        Summary = new ChatAnalyticsSummary();
        TopQuestions = new List<ChatTermStat>();
        TopKeywords = new List<ChatTermStat>();
        TopRecommended = new List<ChatProductStat>();
        TopViewed = new List<ChatProductStat>();
        AvailableStores = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Analytics.From")]
    [UIHint("DateNullable")]
    public DateTime? FromUtc { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Analytics.To")]
    [UIHint("DateNullable")]
    public DateTime? ToUtc { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Analytics.Store")]
    public int StoreId { get; set; }

    public ChatAnalyticsSummary Summary { get; set; }

    public IList<ChatTermStat> TopQuestions { get; set; }

    public IList<ChatTermStat> TopKeywords { get; set; }

    public IList<ChatProductStat> TopRecommended { get; set; }

    public IList<ChatProductStat> TopViewed { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; }
}
