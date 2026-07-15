using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Models.Admin;

namespace Nop.Plugin.Misc.NoorAiAssistant.Factories;

/// <summary>
/// Builds the admin view models. Keeps the controller free of dropdown-population and paging code.
/// </summary>
public interface INoorAiModelFactory
{
    Task<ConfigurationModel> PrepareConfigurationModelAsync();

    Task<FaqSearchModel> PrepareFaqSearchModelAsync(FaqSearchModel searchModel);

    Task<FaqListModel> PrepareFaqListModelAsync(FaqSearchModel searchModel);

    Task<FaqModel> PrepareFaqModelAsync(FaqModel model, ChatFaq faq);

    Task<SuggestedQuestionSearchModel> PrepareQuestionSearchModelAsync(SuggestedQuestionSearchModel searchModel);

    Task<SuggestedQuestionListModel> PrepareQuestionListModelAsync(SuggestedQuestionSearchModel searchModel);

    Task<SuggestedQuestionModel> PrepareQuestionModelAsync(SuggestedQuestionModel model, SuggestedQuestion question);

    Task<AnalyticsModel> PrepareAnalyticsModelAsync(AnalyticsModel model);
}
