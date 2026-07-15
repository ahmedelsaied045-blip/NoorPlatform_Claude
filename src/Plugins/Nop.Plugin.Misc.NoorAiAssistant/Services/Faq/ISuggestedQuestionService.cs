using Nop.Core;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;

/// <summary>
/// The starter questions shown as chips in an empty conversation.
/// </summary>
public interface ISuggestedQuestionService
{
    Task<SuggestedQuestion> GetQuestionByIdAsync(int questionId);

    /// <summary>
    /// Gets questions for the admin grid.
    /// </summary>
    Task<IPagedList<SuggestedQuestion>> GetAllQuestionsAsync(string keywords = null,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    /// Gets the chips to show a shopper: published, and scoped to their store and language. Cached.
    /// </summary>
    Task<IList<SuggestedQuestion>> GetPublishedQuestionsAsync(int storeId, int languageId);

    Task InsertQuestionAsync(SuggestedQuestion question);

    Task UpdateQuestionAsync(SuggestedQuestion question);

    Task DeleteQuestionAsync(SuggestedQuestion question);
}
