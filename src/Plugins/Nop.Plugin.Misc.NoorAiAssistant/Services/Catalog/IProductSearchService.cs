using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Models;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;

/// <summary>
/// Finds products in the store's own catalogue and shapes them into cards the chat window can render.
/// </summary>
/// <remarks>
/// This is the assistant's single door into the catalogue. A future LLM provider would expose this as its
/// product-search function rather than reimplementing search, which is why it takes a plain query object
/// and returns plain DTOs.
/// </remarks>
public interface IProductSearchService
{
    /// <summary>
    /// Searches the catalogue by name, SKU, manufacturer part number, tags, description, category name and
    /// manufacturer name.
    /// </summary>
    /// <param name="query">What to look for</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the matching products, best match first, at most <see cref="ProductSearchQuery.PageSize"/> of them.
    /// </returns>
    Task<IList<Product>> SearchAsync(ProductSearchQuery query);

    /// <summary>
    /// Finds the single product a phrase most likely refers to, for "compare X and Y". Returns null when
    /// the phrase matches nothing.
    /// </summary>
    /// <param name="term">A product name, SKU or fragment of one</param>
    /// <param name="storeId">Store identifier</param>
    /// <param name="languageId">Language identifier</param>
    Task<Product> FindBestMatchAsync(string term, int storeId, int languageId);

    /// <summary>
    /// Turns products into fully-populated cards: picture, formatted prices, discount, stock message,
    /// rating and an add-to-cart URL.
    /// </summary>
    /// <param name="products">The products to render</param>
    Task<IList<ProductCardDto>> PrepareCardsAsync(IEnumerable<Product> products);
}

/// <summary>
/// Represents a catalogue query.
/// </summary>
public class ProductSearchQuery
{
    /// <summary>
    /// Gets or sets the free-text terms.
    /// </summary>
    public string Keywords { get; set; }

    /// <summary>
    /// Gets or sets categories to restrict the search to. Empty searches the whole catalogue.
    /// </summary>
    public IList<int> CategoryIds { get; set; } = new List<int>();

    /// <summary>
    /// Gets or sets manufacturers to restrict the search to.
    /// </summary>
    public IList<int> ManufacturerIds { get; set; } = new List<int>();

    public int StoreId { get; set; }

    public int LanguageId { get; set; }

    public int PageSize { get; set; } = 3;

    /// <summary>
    /// Gets or sets category-name fragments used to widen the search when the free-text terms find
    /// nothing, e.g. "chandelier" also looking in a category actually called "Ceiling & Pendant".
    /// </summary>
    public IList<string> CategoryHints { get; set; } = new List<string>();
}
