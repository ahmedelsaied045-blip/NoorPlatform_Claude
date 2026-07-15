using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models;

/// <summary>
/// Represents a product rendered as a card inside an answer. Everything is pre-formatted (prices already
/// carry the store's currency, the stock message is already localised) so the browser only has to place
/// strings into the DOM.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ProductCardDto
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string ShortDescription { get; set; }

    public string Sku { get; set; }

    public string PictureUrl { get; set; }

    public string PictureAlt { get; set; }

    /// <summary>
    /// Gets or sets the product page URL.
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// Gets or sets the current price, formatted in the customer's currency.
    /// </summary>
    public string Price { get; set; }

    /// <summary>
    /// Gets or sets the current price as a number, after discounts and tier prices, in the customer's
    /// currency. The comparison service ranks on this rather than on the raw catalogue price, which would
    /// call the wrong winner whenever one of the two products is discounted.
    /// </summary>
    public decimal? PriceValue { get; set; }

    /// <summary>
    /// Gets or sets the price before the discount, formatted. Null when the product is not discounted.
    /// </summary>
    public string OldPrice { get; set; }

    /// <summary>
    /// Gets or sets the saving as a whole percentage, derived from the old and current price. 0 when the
    /// product is not discounted.
    /// </summary>
    public int DiscountPercent { get; set; }

    /// <summary>
    /// Gets or sets the localised stock message, e.g. "In stock" / "متوفر".
    /// </summary>
    public string StockStatus { get; set; }

    public bool InStock { get; set; }

    /// <summary>
    /// Gets or sets the average rating out of 5, rounded to one decimal.
    /// </summary>
    public double Rating { get; set; }

    public int ReviewCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the card may show an add-to-cart button. False for
    /// call-for-price products, products with required attributes, and grouped products, which all have to
    /// go through the product page.
    /// </summary>
    public bool CanAddToCart { get; set; }

    /// <summary>
    /// Gets or sets the URL the add-to-cart button posts to.
    /// </summary>
    public string AddToCartUrl { get; set; }
}
