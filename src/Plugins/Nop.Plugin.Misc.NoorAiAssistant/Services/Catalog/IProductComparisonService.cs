using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.NoorAiAssistant.Models;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Catalog;

/// <summary>
/// Compares two products across price, brand, availability, rating and specifications, and says which one
/// to buy.
/// </summary>
public interface IProductComparisonService
{
    /// <summary>
    /// Compares two products.
    /// </summary>
    /// <param name="productA">The first product</param>
    /// <param name="productB">The second product</param>
    /// <param name="arabic">Whether to phrase the verdict in Arabic</param>
    Task<ProductComparisonDto> CompareAsync(Product productA, Product productB, bool arabic);
}
