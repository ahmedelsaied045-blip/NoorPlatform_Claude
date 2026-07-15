using Nop.Core;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Faq;

/// <summary>
/// The store's own knowledge base: everything the assistant can answer without looking at the catalogue.
/// </summary>
public interface IChatFaqService
{
    /// <summary>
    /// Gets a FAQ entry by identifier.
    /// </summary>
    Task<ChatFaq> GetFaqByIdAsync(int faqId);

    /// <summary>
    /// Gets FAQ entries for the admin grid.
    /// </summary>
    /// <param name="keywords">Filter on question, answer or keywords; pass null for all</param>
    /// <param name="topicId">Filter on topic; pass null for all</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    Task<IPagedList<ChatFaq>> GetAllFaqsAsync(string keywords = null, int? topicId = null,
        int pageIndex = 0, int pageSize = int.MaxValue);

    /// <summary>
    /// Gets every entry a shopper could be shown in this store, in any language. Cached, because it is
    /// read on every message.
    /// </summary>
    /// <remarks>
    /// Deliberately not filtered by language. The assistant answers in the language the shopper *typed*,
    /// which is not necessarily the language the storefront is set to — someone typing English into the
    /// Arabic store must get an English answer. Filtering here would leave only the Arabic entries in
    /// scope and make that impossible; <see cref="MatchAsync"/> picks the language instead.
    /// </remarks>
    Task<IList<ChatFaq>> GetPublishedFaqsAsync(int storeId);

    /// <summary>
    /// Finds the entry that best answers a question, or null when none is a good enough fit.
    /// </summary>
    /// <param name="normalizedMessage">A message already put through TextNormalizer.Normalize</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="preferArabic">
    /// Whether the assistant is answering in Arabic. Among the entries that match on keywords, one written
    /// in this language wins; an entry scoped to "all languages" fits either.
    /// </param>
    Task<ChatFaq> MatchAsync(string normalizedMessage, int storeId, bool preferArabic);

    Task InsertFaqAsync(ChatFaq faq);

    Task UpdateFaqAsync(ChatFaq faq);

    Task DeleteFaqAsync(ChatFaq faq);
}
