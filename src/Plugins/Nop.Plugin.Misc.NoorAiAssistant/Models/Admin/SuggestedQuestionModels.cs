using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;

/// <summary>
/// One starter chip in the admin.
/// </summary>
public record SuggestedQuestionModel : BaseNopEntityModel
{
    public SuggestedQuestionModel()
    {
        AvailableLanguages = new List<SelectListItem>();
        AvailableStores = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.SuggestedQuestions.Fields.Text")]
    public string Text { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.SuggestedQuestions.Fields.Language")]
    public int LanguageId { get; set; }

    public string LanguageName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.SuggestedQuestions.Fields.Store")]
    public int StoreId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.SuggestedQuestions.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.SuggestedQuestions.Fields.Published")]
    public bool Published { get; set; }

    public IList<SelectListItem> AvailableLanguages { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; }
}

/// <summary>
/// The filters above the suggested-questions grid.
/// </summary>
public record SuggestedQuestionSearchModel : BaseSearchModel
{
    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.SuggestedQuestions.Fields.SearchKeywords")]
    public string SearchKeywords { get; set; }
}

/// <summary>
/// A page of the suggested-questions grid.
/// </summary>
public record SuggestedQuestionListModel : BasePagedListModel<SuggestedQuestionModel>;
