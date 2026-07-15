using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;

/// <summary>
/// Stores and matches FAQ entries.
/// </summary>
/// <remarks>
/// Matching is scored rather than first-hit, because a store's FAQ entries overlap: "how long does
/// delivery take" and "how much does shipping cost" share the word "delivery", and returning whichever
/// happened to be inserted first would be a coin toss. Scoring, plus a floor below which the assistant
/// says it does not know, is what stops the FAQ from confidently answering the wrong question.
/// </remarks>
public partial class ChatFaqService : IChatFaqService
{
    #region Constants

    /// <summary>
    /// The score a match must reach to be used. One keyword hit clears it; a single incidental word shared
    /// with the question does not.
    /// </summary>
    private const int MATCH_THRESHOLD = 10;

    /// <summary>
    /// What an explicit keyword hit is worth. Keywords are curated by the store owner, so they are the
    /// strongest signal available.
    /// </summary>
    private const int KEYWORD_HIT_SCORE = 10;

    /// <summary>
    /// What a word shared with the entry's question text is worth.
    /// </summary>
    private const int QUESTION_WORD_SCORE = 3;

    #endregion

    #region Fields

    private readonly ILanguageService _languageService;
    private readonly IRepository<ChatFaq> _faqRepository;
    private readonly IStaticCacheManager _staticCacheManager;

    #endregion

    #region Ctor

    public ChatFaqService(ILanguageService languageService,
        IRepository<ChatFaq> faqRepository,
        IStaticCacheManager staticCacheManager)
    {
        _languageService = languageService;
        _faqRepository = faqRepository;
        _staticCacheManager = staticCacheManager;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Determines whether an entry is written in Arabic, from the language it is scoped to. An entry
    /// scoped to "all languages" (0) is neutral and fits either.
    /// </summary>
    protected virtual async Task<bool?> IsArabicEntryAsync(int languageId)
    {
        if (languageId == 0)
            return null;

        var language = await _languageService.GetLanguageByIdAsync(languageId);

        return language?.LanguageCulture?.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ?? false;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets a FAQ entry by identifier
    /// </summary>
    public virtual async Task<ChatFaq> GetFaqByIdAsync(int faqId)
    {
        return await _faqRepository.GetByIdAsync(faqId, cache => default);
    }

    /// <summary>
    /// Gets FAQ entries for the admin grid
    /// </summary>
    public virtual async Task<IPagedList<ChatFaq>> GetAllFaqsAsync(string keywords = null, int? topicId = null,
        int pageIndex = 0, int pageSize = int.MaxValue)
    {
        return await _faqRepository.GetAllPagedAsync(query =>
        {
            if (!string.IsNullOrWhiteSpace(keywords))
            {
                query = query.Where(faq =>
                    faq.Question.Contains(keywords) ||
                    faq.Answer.Contains(keywords) ||
                    faq.Keywords.Contains(keywords));
            }

            if (topicId.HasValue && topicId > 0)
                query = query.Where(faq => faq.TopicId == topicId);

            return query.OrderBy(faq => faq.TopicId).ThenBy(faq => faq.DisplayOrder).ThenBy(faq => faq.Id);
        }, pageIndex, pageSize);
    }

    /// <summary>
    /// Gets every entry a shopper could be shown in this store, in any language
    /// </summary>
    public virtual async Task<IList<ChatFaq>> GetPublishedFaqsAsync(int storeId)
    {
        var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(
            NoorAiAssistantDefaults.FaqAllCacheKey, storeId);

        return await _staticCacheManager.GetAsync(cacheKey, async () =>
            await _faqRepository.Table
                .Where(faq => faq.Published)
                //0 means "every store", so an entry is in scope if it is unscoped or scoped to this store
                .Where(faq => faq.StoreId == 0 || faq.StoreId == storeId)
                .OrderBy(faq => faq.DisplayOrder)
                .ThenBy(faq => faq.Id)
                .ToListAsync());
    }

    /// <summary>
    /// Finds the entry that best answers a question
    /// </summary>
    public virtual async Task<ChatFaq> MatchAsync(string normalizedMessage, int storeId, bool preferArabic)
    {
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return null;

        var faqs = await GetPublishedFaqsAsync(storeId);
        if (faqs.Count == 0)
            return null;

        var messageWords = TextNormalizer.ExtractKeywords(normalizedMessage, max: 20).ToHashSet(StringComparer.Ordinal);

        var candidates = new List<(ChatFaq faq, int score)>();
        foreach (var faq in faqs)
        {
            var score = 0;

            //curated keywords: an entry lists "shipping,delivery,شحن,توصيل" and any of them firing is a
            //deliberate signal from the store owner that this entry answers this kind of question. The same
            //keyword list is shared by the Arabic and English copies of an entry, which is exactly why a
            //keyword hit alone cannot decide *which* copy to return — hence the language pass below.
            foreach (var keyword in (faq.Keywords ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (TextNormalizer.ContainsTerm(normalizedMessage, keyword))
                    score += KEYWORD_HIT_SCORE;
            }

            //words shared with the entry's own question, as a weaker backstop for entries whose keywords
            //were left blank. This also breaks the tie between the two language copies of an entry, since
            //only the copy written in the shopper's language shares words with their question.
            foreach (var word in TextNormalizer.ExtractKeywords(faq.Question, max: 20))
            {
                if (messageWords.Contains(word))
                    score += QUESTION_WORD_SCORE;
            }

            if (score >= MATCH_THRESHOLD)
                candidates.Add((faq, score));
        }

        if (candidates.Count == 0)
            return null;

        //answer in the language the shopper wrote in, not the one the storefront happens to be set to
        var preferred = new List<(ChatFaq faq, int score)>();
        foreach (var candidate in candidates)
        {
            var isArabicEntry = await IsArabicEntryAsync(candidate.faq.LanguageId);

            //null = the entry is scoped to "all languages" and so fits either
            if (isArabicEntry is null || isArabicEntry == preferArabic)
                preferred.Add(candidate);
        }

        //if the store has no entry in the right language, a right answer in the wrong language still beats
        //"I don't know"
        var pool = preferred.Count > 0 ? preferred : candidates;

        return pool.OrderByDescending(candidate => candidate.score).First().faq;
    }

    public virtual async Task InsertFaqAsync(ChatFaq faq)
    {
        ArgumentNullException.ThrowIfNull(faq);

        faq.CreatedOnUtc = DateTime.UtcNow;
        faq.UpdatedOnUtc = DateTime.UtcNow;

        await _faqRepository.InsertAsync(faq);
        await _staticCacheManager.RemoveByPrefixAsync(NoorAiAssistantDefaults.FaqPrefix);
    }

    public virtual async Task UpdateFaqAsync(ChatFaq faq)
    {
        ArgumentNullException.ThrowIfNull(faq);

        faq.UpdatedOnUtc = DateTime.UtcNow;

        await _faqRepository.UpdateAsync(faq);
        await _staticCacheManager.RemoveByPrefixAsync(NoorAiAssistantDefaults.FaqPrefix);
    }

    public virtual async Task DeleteFaqAsync(ChatFaq faq)
    {
        ArgumentNullException.ThrowIfNull(faq);

        await _faqRepository.DeleteAsync(faq);
        await _staticCacheManager.RemoveByPrefixAsync(NoorAiAssistantDefaults.FaqPrefix);
    }

    #endregion
}
