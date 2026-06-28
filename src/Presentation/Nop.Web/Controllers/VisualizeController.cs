using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Services.Catalog;
using Nop.Services.Localization;
using Nop.Web.Factories;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Visualize;

namespace Nop.Web.Controllers;

[AutoValidateAntiforgeryToken]
public partial class VisualizeController : BasePublicController
{
    #region Fields

    protected readonly CatalogSettings _catalogSettings;
    protected readonly ICategoryService _categoryService;
    protected readonly ILocalizationService _localizationService;
    protected readonly IProductModelFactory _productModelFactory;
    protected readonly IProductService _productService;
    protected readonly IStoreContext _storeContext;
    protected readonly IWorkContext _workContext;

    #endregion

    #region Ctor

    public VisualizeController(CatalogSettings catalogSettings,
        ICategoryService categoryService,
        ILocalizationService localizationService,
        IProductModelFactory productModelFactory,
        IProductService productService,
        IStoreContext storeContext,
        IWorkContext workContext)
    {
        _catalogSettings = catalogSettings;
        _categoryService = categoryService;
        _localizationService = localizationService;
        _productModelFactory = productModelFactory;
        _productService = productService;
        _storeContext = storeContext;
        _workContext = workContext;
    }

    #endregion

    #region Methods

    public virtual async Task<IActionResult> Index()
    {
        var store = await _storeContext.GetCurrentStoreAsync();
        var categories = await _categoryService.GetAllCategoriesAsync(store.Id);

        var model = new VisualizeIndexModel();
        foreach (var category in categories)
        {
            model.Categories.Add(new VisualizeCategoryModel
            {
                Id = category.Id,
                Name = await _localizationService.GetLocalizedAsync(category, x => x.Name)
            });
        }

        return View(model);
    }

    /// <summary>
    /// Returns a JSON page of products (id, name, price, image) for the visualizer product picker
    /// </summary>
    [HttpGet]
    [CheckLanguageSeoCode(ignore: true)]
    public virtual async Task<IActionResult> Products(string term, int categoryId, int pageIndex = 0, int pageSize = 24)
    {
        if (pageSize <= 0 || pageSize > 60)
            pageSize = 24;
        if (pageIndex < 0)
            pageIndex = 0;

        var store = await _storeContext.GetCurrentStoreAsync();
        var language = await _workContext.GetWorkingLanguageAsync();

        var categoryIds = new List<int>();
        if (categoryId > 0)
            categoryIds.AddRange([categoryId, .. await _categoryService.GetChildCategoryIdsAsync(categoryId, store.Id)]);

        var products = await _productService.SearchProductsAsync(pageIndex,
            categoryIds: categoryIds,
            storeId: store.Id,
            keywords: string.IsNullOrWhiteSpace(term) ? null : term.Trim(),
            languageId: language.Id,
            visibleIndividuallyOnly: true,
            orderBy: ProductSortingEnum.Position,
            pageSize: pageSize);

        //reuse the catalog overview factory to get localized name, formatted price and picture URLs
        var models = (await _productModelFactory.PrepareProductOverviewModelsAsync(products,
            preparePriceModel: true,
            preparePictureModel: true,
            productThumbPictureSize: 600)).ToList();

        var items = models.Select(p =>
        {
            var picture = p.PictureModels?.FirstOrDefault();
            return new
            {
                id = p.Id,
                name = p.Name,
                price = p.ProductPrice?.Price,
                //product images are served same-origin, so they can be drawn to a canvas (background removal / export)
                imageUrl = picture?.ImageUrl,
                fullImageUrl = picture?.FullSizeImageUrl,
                //SimpleProduct = 5; others (grouped / require attributes) can't be one-click added to cart
                requiresConfiguration = p.ProductType != ProductType.SimpleProduct
            };
        }).ToList();

        return Json(new
        {
            products = items,
            hasNextPage = products.HasNextPage,
            totalCount = products.TotalCount,
            pageIndex = products.PageIndex
        });
    }

    #endregion
}
