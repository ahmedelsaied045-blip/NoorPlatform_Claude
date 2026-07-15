using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Nop.Plugin.Misc.NoorAiAssistant.Models;

/// <summary>
/// Represents the structured half of an assistant answer. The prose half lives in the message text; this
/// carries anything the browser must render as a component rather than as markdown. It is what gets
/// serialised into <see cref="Domain.ChatMessage.PayloadJson"/>, so replaying history restores the product
/// cards and tables exactly, without re-running the search.
/// </summary>
[JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
public class ChatAnswerPayload
{
    /// <summary>
    /// Gets or sets the product cards to render under the answer.
    /// </summary>
    public IList<ProductCardDto> Products { get; set; }

    /// <summary>
    /// Gets or sets the comparison table.
    /// </summary>
    public ProductComparisonDto Comparison { get; set; }

    /// <summary>
    /// Gets or sets the lighting plan.
    /// </summary>
    public LightingPlanDto LightingPlan { get; set; }

    /// <summary>
    /// Gets or sets the quantity estimate.
    /// </summary>
    public QuantityEstimateDto QuantityEstimate { get; set; }

    /// <summary>
    /// Gets or sets the follow-up questions offered as chips beneath the answer.
    /// </summary>
    public IList<string> FollowUps { get; set; }

    /// <summary>
    /// Gets a value indicating whether there is anything at all to render. Used to avoid persisting an
    /// empty JSON object on every plain-prose answer.
    /// </summary>
    [JsonIgnore]
    public bool IsEmpty =>
        (Products is null || Products.Count == 0) &&
        Comparison is null &&
        LightingPlan is null &&
        QuantityEstimate is null &&
        (FollowUps is null || FollowUps.Count == 0);
}
