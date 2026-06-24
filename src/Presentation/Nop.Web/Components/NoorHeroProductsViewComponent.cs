using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Web.Factories;
using Nop.Web.Framework.Components;

namespace Nop.Web.Components;

/// <summary>
/// Renders new products and current offers inside the NoorTheme1 hero visual slot.
/// New products are sourced from "mark as new" products and supplemented with the
/// merchant-curated "show on home page" products so offers can be highlighted there too.
/// </summary>
public partial class NoorHeroProductsViewComponent : NopViewComponent
{
    protected readonly IAclService _aclService;
    protected readonly IProductModelFactory _productModelFactory;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;
    protected readonly IStoreMappingService _storeMappingService;

    public NoorHeroProductsViewComponent(IAclService aclService,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IStoreContext storeContext,
        IStoreMappingService storeMappingService)
    {
        _aclService = aclService;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _storeContext = storeContext;
        _storeMappingService = storeMappingService;
    }

    public async Task<IViewComponentResult> InvokeAsync(int? maxItems, int? productThumbPictureSize)
    {
        var take = maxItems ?? 8;
        //prepare a wider candidate pool so the PNG-only filter below still has enough to show
        var pool = System.Math.Max(take * 4, 24);
        var store = await _storeContext.GetCurrentStoreAsync();

        //newest products first, then merchant-curated homepage products (offers/featured)
        var newProducts = await _productService.GetProductsMarkedAsNewAsync(storeId: store.Id, pageSize: pool);
        var homepageProducts = await _productService.GetAllProductsDisplayedOnHomepageAsync();

        var products = await newProducts.Concat(homepageProducts)
            //de-duplicate while preserving order (new products take precedence)
            .GroupBy(p => p.Id).Select(g => g.First())
            //ACL and store mapping
            .WhereAwait(async p => await _aclService.AuthorizeAsync(p) && await _storeMappingService.AuthorizeAsync(p))
            //availability dates
            .Where(p => _productService.ProductIsAvailable(p))
            //visible individually
            .Where(p => p.VisibleIndividually)
            .Take(pool)
            .ToListAsync();

        if (!products.Any())
            return Content("");

        var models = (await _productModelFactory.PrepareProductOverviewModelsAsync(products, true, true, productThumbPictureSize)).ToList();

        //only show products whose main image is a transparent PNG, so it floats on the banner background
        static bool HasPngPicture(Nop.Web.Models.Catalog.ProductOverviewModel m)
        {
            var url = m.PictureModels?.FirstOrDefault()?.ImageUrl;
            return !string.IsNullOrEmpty(url)
                && url.Split('?')[0].EndsWith(".png", System.StringComparison.OrdinalIgnoreCase);
        }

        var pngModels = models.Where(HasPngPicture).ToList();
        //fall back to all products if none are PNG, so the hero is never empty
        var model = (pngModels.Any() ? pngModels : models).Take(take).ToList();

        return View(model);
    }
}
