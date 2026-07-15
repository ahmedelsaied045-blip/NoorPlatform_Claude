using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;

/// <summary>
/// Stores the starter questions.
/// </summary>
public partial class SuggestedQuestionService : ISuggestedQuestionService
{
    #region Fields

    private readonly IRepository<SuggestedQuestion> _questionRepository;
    private readonly IStaticCacheManager _staticCacheManager;

    #endregion

    #region Ctor

    public SuggestedQuestionService(IRepository<SuggestedQuestion> questionRepository,
        IStaticCacheManager staticCacheManager)
    {
        _questionRepository = questionRepository;
        _staticCacheManager = staticCacheManager;
    }

    #endregion

    #region Methods

    public virtual async Task<SuggestedQuestion> GetQuestionByIdAsync(int questionId)
    {
        return await _questionRepository.GetByIdAsync(questionId, cache => default);
    }

    /// <summary>
    /// Gets questions for the admin grid
    /// </summary>
    public virtual async Task<IPagedList<SuggestedQuestion>> GetAllQuestionsAsync(string keywords = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _questionRepository.GetAllPagedAsync(query =>
        {
            if (!string.IsNullOrWhiteSpace(keywords))
                query = query.Where(question => question.Text.Contains(keywords));

            return query.OrderBy(question => question.DisplayOrder).ThenBy(question => question.Id);
        }, pageIndex, pageSize);
    }

    /// <summary>
    /// Gets the chips to show a shopper
    /// </summary>
    public virtual async Task<IList<SuggestedQuestion>> GetPublishedQuestionsAsync(int storeId, int languageId)
    {
        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
            NoorAiAssistantDefaults.SuggestedQuestionsCacheKey, storeId, languageId);

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
            await _questionRepository.Table
                .Where(question => question.Published)
                .Where(question => question.StoreId == 0 || question.StoreId == storeId)
                .Where(question => question.LanguageId == 0 || question.LanguageId == languageId)
                .OrderBy(question => question.DisplayOrder)
                .ThenBy(question => question.Id)
                .ToListAsync());
    }

    public virtual async Task InsertQuestionAsync(SuggestedQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        await _questionRepository.InsertAsync(question);
        await _staticCacheManager.RemoveByPrefixAsync(NoorAiAssistantDefaults.SuggestedQuestionsPrefix);
    }

    public virtual async Task UpdateQuestionAsync(SuggestedQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        await _questionRepository.UpdateAsync(question);
        await _staticCacheManager.RemoveByPrefixAsync(NoorAiAssistantDefaults.SuggestedQuestionsPrefix);
    }

    public virtual async Task DeleteQuestionAsync(SuggestedQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        await _questionRepository.DeleteAsync(question);
        await _staticCacheManager.RemoveByPrefixAsync(NoorAiAssistantDefaults.SuggestedQuestionsPrefix);
    }

    #endregion
}
