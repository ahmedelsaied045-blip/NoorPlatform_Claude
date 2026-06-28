using Nop.Web.Framework.Models;

namespace Nop.Web.Models.Visualize;

/// <summary>
/// Represents the "Visualize Before You Buy" page model
/// </summary>
public partial record VisualizeIndexModel : BaseNopModel
{
    public VisualizeIndexModel()
    {
        Categories = new List<VisualizeCategoryModel>();
    }

    public IList<VisualizeCategoryModel> Categories { get; set; }
}

/// <summary>
/// Represents a category option used by the visualizer product picker filter
/// </summary>
public partial record VisualizeCategoryModel : BaseNopEntityModel
{
    public string Name { get; set; }
}
