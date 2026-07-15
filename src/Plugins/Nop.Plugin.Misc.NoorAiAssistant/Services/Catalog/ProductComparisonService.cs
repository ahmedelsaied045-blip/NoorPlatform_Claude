using System.Net;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Factories;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;

/// <summary>
/// Compares two products and picks a winner.
/// </summary>
/// <remarks>
/// The verdict is scored on the three things that are objectively comparable — price, rating and
/// availability — and deliberately not on specifications. A shopper comparing a 9W and an 18W spotlight is
/// not asking which number is bigger; whether more watts is better depends entirely on the room, and a
/// confident wrong answer there is worse than none. So specification rows are shown side by side for the
/// shopper to judge, and left out of the score.
/// </remarks>
public partial class ProductComparisonService : IProductComparisonService
{
    #region Fields

    private readonly ILocalizationService _localizationService;
    private readonly IManufacturerService _manufacturerService;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IProductSearchService _productSearchService;

    #endregion

    #region Ctor

    public ProductComparisonService(ILocalizationService localizationService,
        IManufacturerService manufacturerService,
        IProductModelFactory productModelFactory,
        IProductSearchService productSearchService)
    {
        _localizationService = localizationService;
        _manufacturerService = manufacturerService;
        _productModelFactory = productModelFactory;
        _productSearchService = productSearchService;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Gets the brand name, or a dash when the product has none.
    /// </summary>
    protected virtual async Task<string> GetBrandAsync(Product product)
    {
        var mappings = await _manufacturerService.GetProductManufacturersByProductIdAsync(product.Id);

        var names = new List<string>();
        foreach (var mapping in mappings)
        {
            var manufacturer = await _manufacturerService.GetManufacturerByIdAsync(mapping.ManufacturerId);
            if (manufacturer is not null)
                names.Add(await _localizationService.GetLocalizedAsync(manufacturer, m => m.Name));
        }

        return names.Count > 0 ? string.Join(", ", names) : "—";
    }

    /// <summary>
    /// Flattens a product's specification groups into attribute name -> value.
    /// </summary>
    protected static Dictionary<string, string> FlattenSpecifications(ProductSpecificationModel model)
    {
        var specs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var attribute in model.Groups.SelectMany(group => group.Attributes))
        {
            //ValueRaw arrives HTML-encoded because the storefront drops it straight into markup. The chat
            //window escapes on render instead, so decoding here avoids the shopper seeing "10&nbsp;W".
            var value = string.Join(", ", attribute.Values
                .Select(v => WebUtility.HtmlDecode(v.ValueRaw ?? string.Empty).Trim())
                .Where(v => v.Length > 0));

            if (value.Length > 0 && !specs.ContainsKey(attribute.Name))
                specs[attribute.Name] = value;
        }

        return specs;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Compares two products
    /// </summary>
    public virtual async Task<ProductComparisonDto> CompareAsync(Product productA, Product productB, bool arabic)
    {
        ArgumentNullException.ThrowIfNull(productA);
        ArgumentNullException.ThrowIfNull(productB);

        var cards = await _productSearchService.PrepareCardsAsync([productA, productB]);
        var cardA = cards.FirstOrDefault(c => c.Id == productA.Id);
        var cardB = cards.FirstOrDefault(c => c.Id == productB.Id);

        if (cardA is null || cardB is null)
            return null;

        //the specification models are not on the cards, so ask the factory for them separately
        var overviews = (await _productModelFactory.PrepareProductOverviewModelsAsync(
                [productA, productB], preparePriceModel: false, preparePictureModel: false,
                prepareSpecificationAttributes: true))
            .ToDictionary(m => m.Id);

        var specsA = FlattenSpecifications(overviews[productA.Id].ProductSpecificationModel);
        var specsB = FlattenSpecifications(overviews[productB.Id].ProductSpecificationModel);

        var comparison = new ProductComparisonDto { ProductA = cardA, ProductB = cardB };

        //--- price. A call-for-price product has no comparable number, so nobody wins that row.
        var priceWinner = 0;
        if (cardA.PriceValue.HasValue && cardB.PriceValue.HasValue && cardA.PriceValue != cardB.PriceValue)
            priceWinner = cardA.PriceValue < cardB.PriceValue ? -1 : 1;

        comparison.Rows.Add(new ComparisonRowDto
        {
            Label = arabic ? "السعر" : "Price",
            ValueA = cardA.Price ?? (arabic ? "اتصل للسعر" : "Call for price"),
            ValueB = cardB.Price ?? (arabic ? "اتصل للسعر" : "Call for price"),
            Winner = priceWinner
        });

        //--- discount
        if (cardA.DiscountPercent > 0 || cardB.DiscountPercent > 0)
        {
            comparison.Rows.Add(new ComparisonRowDto
            {
                Label = arabic ? "الخصم" : "Discount",
                ValueA = cardA.DiscountPercent > 0 ? $"{cardA.DiscountPercent}%" : "—",
                ValueB = cardB.DiscountPercent > 0 ? $"{cardB.DiscountPercent}%" : "—",
                Winner = cardA.DiscountPercent == cardB.DiscountPercent ? 0 : cardA.DiscountPercent > cardB.DiscountPercent ? -1 : 1
            });
        }

        //--- brand
        comparison.Rows.Add(new ComparisonRowDto
        {
            Label = arabic ? "العلامة التجارية" : "Brand",
            ValueA = await GetBrandAsync(productA),
            ValueB = await GetBrandAsync(productB),
            Winner = 0
        });

        //--- availability
        comparison.Rows.Add(new ComparisonRowDto
        {
            Label = arabic ? "التوفر" : "Availability",
            ValueA = cardA.StockStatus,
            ValueB = cardB.StockStatus,
            Winner = cardA.InStock == cardB.InStock ? 0 : cardA.InStock ? -1 : 1
        });

        //--- rating
        var ratingWinner = Math.Abs(cardA.Rating - cardB.Rating) < 0.05 ? 0 : cardA.Rating > cardB.Rating ? -1 : 1;
        comparison.Rows.Add(new ComparisonRowDto
        {
            Label = arabic ? "التقييم" : "Rating",
            //Invariant, as elsewhere: the thread culture follows the storefront language, not the language
            //this answer is being written in
            ValueA = cardA.ReviewCount > 0 ? FormattableString.Invariant($"{cardA.Rating:0.#}/5 ({cardA.ReviewCount})") : "—",
            ValueB = cardB.ReviewCount > 0 ? FormattableString.Invariant($"{cardB.Rating:0.#}/5 ({cardB.ReviewCount})") : "—",
            //an unreviewed product cannot win on rating, and a 5/5 from one review does not beat 4.6 from forty
            Winner = cardA.ReviewCount == 0 || cardB.ReviewCount == 0 ? 0 : ratingWinner
        });

        //--- specifications, shown but not scored
        foreach (var label in specsA.Keys.Union(specsB.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(k => k))
        {
            comparison.Rows.Add(new ComparisonRowDto
            {
                Label = label,
                ValueA = specsA.GetValueOrDefault(label, "—"),
                ValueB = specsB.GetValueOrDefault(label, "—"),
                Winner = 0
            });
        }

        BuildVerdict(comparison, arabic);

        return comparison;
    }

    /// <summary>
    /// Fills in the advantages, disadvantages and closing recommendation from the scored rows.
    /// </summary>
    protected virtual void BuildVerdict(ProductComparisonDto comparison, bool arabic)
    {
        var cardA = comparison.ProductA;
        var cardB = comparison.ProductB;

        void Note(int winner, string labelA, string labelB)
        {
            switch (winner)
            {
                case -1:
                    comparison.AdvantagesA.Add(labelA);
                    comparison.DisadvantagesB.Add(labelB);
                    break;
                case 1:
                    comparison.AdvantagesB.Add(labelA);
                    comparison.DisadvantagesA.Add(labelB);
                    break;
            }
        }

        var priceRow = comparison.Rows.First(r => r.Label is "Price" or "السعر");
        Note(priceRow.Winner,
            arabic ? "السعر أقل" : "Lower price",
            arabic ? "السعر أعلى" : "Higher price");

        var discountRow = comparison.Rows.FirstOrDefault(r => r.Label is "Discount" or "الخصم");
        if (discountRow is not null)
        {
            Note(discountRow.Winner,
                arabic ? "خصم أكبر" : "Bigger discount",
                arabic ? "خصم أقل" : "Smaller discount");
        }

        var stockRow = comparison.Rows.First(r => r.Label is "Availability" or "التوفر");
        Note(stockRow.Winner,
            arabic ? "متوفر في المخزون" : "In stock",
            arabic ? "غير متوفر حاليًا" : "Not currently available");

        var ratingRow = comparison.Rows.First(r => r.Label is "Rating" or "التقييم");
        Note(ratingRow.Winner,
            arabic ? "تقييم أعلى من العملاء" : "Better customer rating",
            arabic ? "تقييم أقل" : "Lower rating");

        //availability is weighted heaviest: the cheapest, best-rated product is worth nothing to a shopper
        //who cannot buy it today
        var scoreA = (stockRow.Winner == -1 ? 2 : 0) + (priceRow.Winner == -1 ? 1 : 0) + (ratingRow.Winner == -1 ? 1 : 0);
        var scoreB = (stockRow.Winner == 1 ? 2 : 0) + (priceRow.Winner == 1 ? 1 : 0) + (ratingRow.Winner == 1 ? 1 : 0);

        if (scoreA == scoreB)
        {
            comparison.RecommendedProductId = 0;
            comparison.Recommendation = arabic
                ? "المنتجان متقاربان جدًا. اختر بناءً على الشكل والمواصفات التي تناسب مكانك — وسأساعدك إن أخبرتني بمقاس الغرفة."
                : "The two are very close. Pick on looks and the specifications that suit your space — tell me your room size and I can narrow it down.";

            return;
        }

        var winner = scoreA > scoreB ? cardA : cardB;
        var reasons = scoreA > scoreB ? comparison.AdvantagesA : comparison.AdvantagesB;

        comparison.RecommendedProductId = winner.Id;
        comparison.Recommendation = arabic
            ? $"أنصحك بـ **{winner.Name}** — {string.Join("، ", reasons.Select(r => r.ToLowerInvariant()))}."
            : $"I'd go with **{winner.Name}** — {string.Join(", ", reasons.Select(r => r.ToLowerInvariant()))}.";
    }

    #endregion
}
