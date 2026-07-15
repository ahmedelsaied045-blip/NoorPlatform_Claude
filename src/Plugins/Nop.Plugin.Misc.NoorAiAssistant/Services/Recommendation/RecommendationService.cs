using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;
using Nop.Services.Localization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Recommendation;

/// <summary>
/// Recommends products for a need by resolving that need against the live catalogue.
/// </summary>
public partial class RecommendationService : IRecommendationService
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly IProductSearchService _productSearchService;

    #endregion

    #region Ctor

    public RecommendationService(ILocalizationService localizationService,
        IProductSearchService productSearchService)
    {
        _localizationService = localizationService;
        _productSearchService = productSearchService;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Recommends products for a recognised need
    /// </summary>
    public virtual async Task<IList<Product>> RecommendAsync(NeedProfile need, int storeId, int languageId, int maxProducts)
    {
        ArgumentNullException.ThrowIfNull(need);

        //hand the search both routes to the product: the category names this need is likely filed under,
        //and the words likely to appear in the product's own text. The search cascade tries the narrow one
        //first and only widens if it comes back empty.
        return await _productSearchService.SearchAsync(new ProductSearchQuery
        {
            Keywords = string.Join(' ', need.SearchKeywords),
            CategoryHints = need.CategoryHints.ToList(),
            StoreId = storeId,
            LanguageId = languageId,
            PageSize = maxProducts
        });
    }

    /// <summary>
    /// Recommends products to fulfil a lighting plan
    /// </summary>
    public virtual async Task<IList<Product>> RecommendForPlanAsync(NeedProfile need, int wattsPerFixture,
        int storeId, int languageId, int maxProducts)
    {
        need ??= NeedCatalog.FindBySlug("spotlight");

        //over-fetch, because the wattage preference below can only re-rank what it is given, and the best
        //wattage match is often not in the first three by catalogue position
        var candidates = await RecommendAsync(need, storeId, languageId, Math.Max(maxProducts * 4, 12));
        if (candidates.Count <= maxProducts)
            return candidates;

        //prefer the fixture closest to the wattage the plan calls for. Stores routinely put the wattage in
        //the product name ("Spotlight COB 9W"), so reading it from there covers most catalogues without
        //requiring a specification attribute to have been filled in.
        var scored = new List<(Product product, int distance)>();
        foreach (var product in candidates)
        {
            var name = await _localizationService.GetLocalizedAsync(product, p => p.Name);
            var watts = ExtractWatts(name);

            //a product with no readable wattage sorts after every product that has one, but ahead of nothing
            scored.Add((product, watts.HasValue ? Math.Abs(watts.Value - wattsPerFixture) : int.MaxValue));
        }

        return scored
            .OrderBy(s => s.distance)
            .Take(maxProducts)
            .Select(s => s.product)
            .ToList();
    }

    /// <summary>
    /// Reads a wattage out of a product name: "Spotlight COB 9W", "بانل 18 واط".
    /// </summary>
    protected static int? ExtractWatts(string name)
    {
        var normalized = TextNormalizer.Normalize(name);
        if (normalized.Length == 0)
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(normalized,
            @"(?<w>\d{1,3})\s*(?:w|watt|watts|واط|وات)\b",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase,
            TimeSpan.FromMilliseconds(200));

        return match.Success && int.TryParse(match.Groups["w"].Value, out var watts) && watts is > 0 and <= 500
            ? watts
            : null;
    }

    #endregion
}
