using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;

/// <summary>
/// One FAQ entry in the admin.
/// </summary>
public record FaqModel : BaseNopEntityModel
{
    public FaqModel()
    {
        AvailableTopics = new List<SelectListItem>();
        AvailableLanguages = new List<SelectListItem>();
        AvailableStores = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.Question")]
    public string Question { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.Answer")]
    public string Answer { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.Keywords")]
    public string Keywords { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.Topic")]
    public int TopicId { get; set; }

    /// <summary>
    /// Gets or sets the topic name, for the grid — the grid shows names, the form edits ids.
    /// </summary>
    public string TopicName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.Language")]
    public int LanguageId { get; set; }

    public string LanguageName { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.Store")]
    public int StoreId { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.Published")]
    public bool Published { get; set; }

    public IList<SelectListItem> AvailableTopics { get; set; }

    public IList<SelectListItem> AvailableLanguages { get; set; }

    public IList<SelectListItem> AvailableStores { get; set; }
}

/// <summary>
/// The filters above the FAQ grid.
/// </summary>
public record FaqSearchModel : BaseSearchModel
{
    public FaqSearchModel()
    {
        AvailableTopics = new List<SelectListItem>();
    }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.SearchKeywords")]
    public string SearchKeywords { get; set; }

    [NopResourceDisplayName("Plugins.Misc.NoorAiAssistant.Faq.Fields.SearchTopic")]
    public int SearchTopicId { get; set; }

    public IList<SelectListItem> AvailableTopics { get; set; }
}

/// <summary>
/// A page of the FAQ grid.
/// </summary>
public record FaqListModel : BasePagedListModel<FaqModel>;
