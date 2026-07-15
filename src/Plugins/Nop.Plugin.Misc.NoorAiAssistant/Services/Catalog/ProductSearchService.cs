using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Http;
using Nop.Plugin.Misc.NoorAiAssistant.Models;
using Nop.Plugin.Misc.NoorAiAssistant.Services.Text;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;

/// <summary>
/// Searches the catalogue and builds product cards.
/// </summary>
/// <remarks>
/// Search runs as a cascade, widening only when the previous step found nothing. Doing it in one shot with
/// every term OR-ed together sounds simpler but ranks badly: a shopper asking for a "philips led strip"
/// would get every LED product in the store, with the Philips strip somewhere down the list. Narrow first,
/// widen only on failure.
/// </remarks>
public partial class ProductSearchService : IProductSearchService
{
    #region Fields

    private readonly ICategoryService _categoryService;
    private readonly ILocalizationService _localizationService;
    private readonly IManufacturerService _manufacturerService;
    private readonly INopUrlHelper _nopUrlHelper;
    private readonly IProductModelFactory _productModelFactory;
    private readonly IProductService _productService;
    private readonly IWebHelper _webHelper;

    #endregion

    #region Ctor

    public ProductSearchService(ICategoryService categoryService,
        ILocalizationService localizationService,
        IManufacturerService manufacturerService,
        INopUrlHelper nopUrlHelper,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IWebHelper webHelper)
    {
        _categoryService = categoryService;
        _localizationService = localizationService;
        _manufacturerService = manufacturerService;
        _nopUrlHelper = nopUrlHelper;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _webHelper = webHelper;
    }

    #endregion

    #region Utilities

    /// <summary>
    /// Finds categories whose (localised) name matches any of the given fragments.
    /// </summary>
    /// <remarks>
    /// Matching on the name rather than storing category ids is what lets this plugin work against Noor's
    /// real catalogue without being told what is in it, and survive the catalogue being reorganised. The
    /// category list is small and already cached by nopCommerce, so scanning it is cheap.
    /// </remarks>
    protected virtual async Task<IList<int>> ResolveCategoryIdsAsync(IEnumerable<string> fragments, int storeId)
    {
        var normalizedFragments = fragments
            .Select(TextNormalizer.Normalize)
            .Where(f => f.Length > 1)
            .ToList();

        if (normalizedFragments.Count == 0)
            return [];

        var categories = await _categoryService.GetAllCategoriesAsync(storeId: storeId);

        var matched = new List<int>();
        foreach (var category in categories)
        {
            var name = TextNormalizer.Normalize(await _localizationService.GetLocalizedAsync(category, c => c.Name));
            if (name.Length == 0)
                continue;

            //both name and fragment are normalised, so Arabic spellings already fold together here — no
            //ArabicSearchVariants needed (see the remarks on ResolveManufacturerIdsAsync)
            if (normalizedFragments.Any(fragment => name.Contains(fragment, StringComparison.Ordinal)))
                matched.Add(category.Id);
        }

        //include children, so "outdoor" also returns everything filed under Outdoor > Garden
        var withChildren = new List<int>(matched);
        foreach (var categoryId in matched)
            withChildren.AddRange(await _categoryService.GetChildCategoryIdsAsync(categoryId, storeId));

        return withChildren.Distinct().ToList();
    }

    /// <summary>
    /// Finds manufacturers whose (localised) name matches any of the given fragments, so a shopper can
    /// search by brand.
    /// </summary>
    /// <remarks>
    /// Both sides of the comparison are put through <see cref="TextNormalizer.Normalize"/> first — the
    /// brand name below and the fragments above — so Arabic spelling variance (ة/ه, أ/ا, ى/ي) already
    /// folds away here and a query spelled one way matches a brand stored the other. This is why
    /// <see cref="TextNormalizer.ArabicSearchVariants"/> is deliberately *not* applied to brand
    /// resolution: it exists to reach the raw, un-normalised product text behind core's SQL LIKE
    /// (see step 4 of <see cref="SearchAsync"/>), and against an already-folded name its un-folded
    /// spellings would only fail to match. Adding it here would be dead code.
    /// </remarks>
    protected virtual async Task<IList<int>> ResolveManufacturerIdsAsync(IEnumerable<string> fragments, int storeId)
    {
        var normalizedFragments = fragments
            .Select(TextNormalizer.Normalize)
            //a two-letter brand fragment matches half the catalogue; require something substantial
            .Where(f => f.Length > 2)
            .ToList();

        if (normalizedFragments.Count == 0)
            return [];

        var manufacturers = await _manufacturerService.GetAllManufacturersAsync(storeId: storeId);

        var matched = new List<int>();
        foreach (var manufacturer in manufacturers)
        {
            var name = TextNormalizer.Normalize(await _localizationService.GetLocalizedAsync(manufacturer, m => m.Name));
            if (name.Length == 0)
                continue;

            //brand names are matched whole ("philips"), not as substrings, to avoid "led" hitting "Ledvance"
            if (normalizedFragments.Any(fragment => TextNormalizer.ContainsTerm(name, fragment)))
                matched.Add(manufacturer.Id);
        }

        return matched;
    }

    /// <summary>
    /// Runs one catalogue query with every text field switched on.
    /// </summary>
    protected virtual async Task<IList<Product>> RunSearchAsync(ProductSearchQuery query, string keywords,
        IList<int> categoryIds, IList<int> manufacturerIds)
    {
        var products = await _productService.SearchProductsAsync(
            pageIndex: 0,
            pageSize: query.PageSize,
            categoryIds: categoryIds?.Any() == true ? categoryIds : null,
            manufacturerIds: manufacturerIds?.Any() == true ? manufacturerIds : null,
            storeId: query.StoreId,
            visibleIndividuallyOnly: true,
            keywords: string.IsNullOrWhiteSpace(keywords) ? null : keywords,
            searchDescriptions: true,
            searchManufacturerPartNumber: true,
            searchSku: true,
            searchProductTags: true,
            languageId: query.LanguageId,
            orderBy: ProductSortingEnum.Position);

        return products.ToList();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Searches the catalogue
    /// </summary>
    public virtual async Task<IList<Product>> SearchAsync(ProductSearchQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        var terms = TextNormalizer.ExtractKeywords(query.Keywords ?? string.Empty);

        //the caller may already know the categories (a recognised need); otherwise infer them from the text
        var categoryIds = query.CategoryIds?.ToList() ?? [];
        var manufacturerIds = query.ManufacturerIds?.ToList() ?? [];

        if (categoryIds.Count == 0 && query.CategoryHints.Any())
            categoryIds = (await ResolveCategoryIdsAsync(query.CategoryHints, query.StoreId)).ToList();

        if (manufacturerIds.Count == 0 && terms.Count > 0)
            manufacturerIds = (await ResolveManufacturerIdsAsync(terms, query.StoreId)).ToList();

        //1. the whole phrase, inside any category/brand we resolved. Most specific, so most likely right.
        var products = await RunSearchAsync(query, query.Keywords, categoryIds, manufacturerIds);
        if (products.Count > 0)
            return products;

        //2. the category/brand alone. The shopper's wording may not appear in any product text — "I need
        //something for the garden" will not match a product called "Bollard Light 12W", but the category will.
        if (categoryIds.Count > 0 || manufacturerIds.Count > 0)
        {
            products = await RunSearchAsync(query, keywords: null, categoryIds, manufacturerIds);
            if (products.Count > 0)
                return products;
        }

        //3. terms alone, ignoring the categories we guessed at — the guess may simply have been wrong
        if (terms.Count > 0 && (categoryIds.Count > 0 || manufacturerIds.Count > 0))
        {
            products = await RunSearchAsync(query, string.Join(' ', terms), categoryIds: null, manufacturerIds: null);
            if (products.Count > 0)
                return products;
        }

        //4. one term at a time, longest first: the longest word is the most distinctive, so "chandelier" is
        //tried before "gold". Each Arabic term is also tried in the spellings the catalogue might store it
        //under — the search keyword is folded (ة to ه, أ to ا) but the product text is not, so "نجفه" has to
        //reach a product called "نجفة". The folded form is tried first, so exact matches still rank first.
        foreach (var term in terms.OrderByDescending(t => t.Length))
        {
            foreach (var spelling in TextNormalizer.ArabicSearchVariants(term))
            {
                products = await RunSearchAsync(query, spelling, categoryIds: null, manufacturerIds: null);
                if (products.Count > 0)
                    return products;
            }
        }

        return [];
    }

    /// <summary>
    /// Finds the single product a phrase most likely refers to
    /// </summary>
    public virtual async Task<Product> FindBestMatchAsync(string term, int storeId, int languageId)
    {
        if (string.IsNullOrWhiteSpace(term))
            return null;

        var query = new ProductSearchQuery
        {
            Keywords = term,
            StoreId = storeId,
            LanguageId = languageId,
            //ask for a handful rather than one, so the closest-name tie-break below has something to work with
            PageSize = 10
        };

        var products = await SearchAsync(query);
        if (products.Count == 0)
            return null;

        if (products.Count == 1)
            return products[0];

        //the catalogue ranks by Position, which has nothing to do with how well a name matches. Prefer the
        //product whose name actually contains the phrase, then the shortest such name — "LED Strip 5m" beats
        //"LED Strip 5m Warm White Waterproof Kit" for the query "led strip 5m".
        var normalizedTerm = TextNormalizer.Normalize(term);

        var scored = new List<(Product product, int score, int length)>();
        foreach (var product in products)
        {
            var name = TextNormalizer.Normalize(await _localizationService.GetLocalizedAsync(product, p => p.Name));

            var score = 0;
            if (name.Equals(normalizedTerm, StringComparison.Ordinal))
                score = 3;
            else if (name.Contains(normalizedTerm, StringComparison.Ordinal))
                score = 2;
            else if (TextNormalizer.ExtractKeywords(normalizedTerm).All(t => name.Contains(t, StringComparison.Ordinal)))
                score = 1;

            scored.Add((product, score, name.Length));
        }

        return scored.OrderByDescending(s => s.score).ThenBy(s => s.length).First().product;
    }

    /// <summary>
    /// Turns products into fully-populated cards
    /// </summary>
    public virtual async Task<IList<ProductCardDto>> PrepareCardsAsync(IEnumerable<Product> products)
    {
        var productList = products?.ToList() ?? [];
        if (productList.Count == 0)
            return [];

        //reuse the storefront's own factory, so prices carry the customer's currency, tax and discounts and
        //cannot drift from what the product page shows
        var overviewModels = (await _productModelFactory.PrepareProductOverviewModelsAsync(productList))
            .ToDictionary(model => model.Id);

        var cards = new List<ProductCardDto>();
        foreach (var product in productList)
        {
            if (!overviewModels.TryGetValue(product.Id, out var overview))
                continue;

            var price = overview.ProductPrice;
            var picture = overview.PictureModels.FirstOrDefault();
            var review = overview.ReviewOverviewModel;

            //ProductOverviewModel carries no stock field in 4.90 — the storefront only shows availability on
            //the product page — so the message is formatted here the same way the product page does it
            var stockMessage = await _productService.FormatStockMessageAsync(product, string.Empty);

            var discountPercent = 0;
            if (price.OldPriceValue > 0 && price.PriceValue > 0 && price.OldPriceValue > price.PriceValue)
            {
                discountPercent = (int)Math.Round(
                    (1 - price.PriceValue.Value / price.OldPriceValue.Value) * 100, MidpointRounding.AwayFromZero);
            }

            //the one-click catalog add-to-cart only works for simple products; anything with required
            //attributes, a grouped product or a call-for-price item has to go through the product page
            var canAddToCart = product.ProductType == ProductType.SimpleProduct &&
                               !price.DisableBuyButton &&
                               !price.CallForPrice &&
                               !price.CustomerEntersPrice &&
                               !price.AvailableForPreOrder;

            cards.Add(new ProductCardDto
            {
                Id = product.Id,
                Name = overview.Name,
                ShortDescription = overview.ShortDescription,
                Sku = overview.Sku,
                PictureUrl = picture?.ImageUrl,
                PictureAlt = picture?.AlternateText ?? overview.Name,
                Url = await _nopUrlHelper.RouteGenericUrlAsync(product, _webHelper.GetCurrentRequestProtocol()),
                Price = price.CallForPrice ? null : price.Price,
                PriceValue = price.CallForPrice ? null : price.PriceValue,
                OldPrice = price.OldPrice,
                DiscountPercent = discountPercent,
                StockStatus = stockMessage,
                //a product that does not track stock is always buyable; one that does is buyable while it
                //has quantity, or indefinitely if the store accepts backorders
                InStock = product.ManageInventoryMethod != ManageInventoryMethod.ManageStock ||
                          product.StockQuantity > 0 ||
                          product.BackorderMode != BackorderMode.NoBackorders,
                Rating = review.TotalReviews > 0
                    ? Math.Round((double)review.RatingSum / review.TotalReviews, 1)
                    : 0,
                ReviewCount = review.TotalReviews,
                CanAddToCart = canAddToCart,
                //the storefront's own AJAX add-to-cart endpoint, so the cart flyout, totals and
                //notifications all behave exactly as they do from a product box
                AddToCartUrl = canAddToCart
                    ? _nopUrlHelper.RouteUrl(NopRouteNames.Ajax.ADD_PRODUCT_TO_CART_CATALOG, new
                    {
                        productId = product.Id,
                        shoppingCartTypeId = (int)ShoppingCartType.ShoppingCart,
                        quantity = 1
                    })
                    : null
            });
        }

        return cards;
    }

    #endregion
}
