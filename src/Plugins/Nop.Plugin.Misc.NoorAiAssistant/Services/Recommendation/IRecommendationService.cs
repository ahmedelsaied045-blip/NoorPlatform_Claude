using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;

/// <summary>
/// Turns a shopper's stated need ("I need a chandelier") into products from the store's catalogue.
/// </summary>
public interface IRecommendationService
{
    /// <summary>
    /// Recommends products for a recognised need.
    /// </summary>
    /// <param name="need">What the shopper asked for</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="maxProducts">How many to return</param>
    Task<IList<Product>> RecommendAsync(NeedProfile need, int storeId, int languageId, int maxProducts);

    /// <summary>
    /// Recommends products to fulfil a lighting plan, preferring fixtures whose wattage is close to the
    /// wattage the plan calls for.
    /// </summary>
    /// <param name="need">The fixture kind; null falls back to spotlights</param>
    /// <param name="wattsPerFixture">The wattage the plan calls for</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="languageId">Language identifier</param>
    /// <param name="maxProducts">How many to return</param>
    Task<IList<Product>> RecommendForPlanAsync(NeedProfile need, int wattsPerFixture, int storeId, int languageId, int maxProducts);
}
