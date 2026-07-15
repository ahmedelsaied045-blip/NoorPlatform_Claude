using Nop.Plugin.Misc.NoorAiAssistant.Domain;
using Nop.Plugin.Misc.NoorAiAssistant.Models;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Providers;

/// <summary>
/// Represents one answer produced by a provider.
/// </summary>
public class ChatProviderResponse
{
    /// <summary>
    /// Gets or sets the prose answer, in markdown. Rendered by a whitelist markdown renderer in the
    /// browser that escapes HTML first, so a provider can never inject markup here.
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets what the provider decided the shopper wanted. Persisted for analytics.
    /// </summary>
    public ChatIntent Intent { get; set; }

    /// <summary>
    /// Gets or sets the structured content (product cards, comparison, plan) accompanying the prose.
    /// </summary>
    public ChatAnswerPayload Payload { get; set; } = new();

    /// <summary>
    /// Gets or sets the search keywords the provider pulled out of the question, recorded as
    /// <see cref="ChatEventType.KeywordSearched"/> facts to drive the "most searched keywords" report.
    /// </summary>
    public IList<string> Keywords { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the FAQ entry that answered the question, if any.
    /// </summary>
    public int MatchedFaqId { get; set; }

    /// <summary>
    /// Creates a prose-only answer.
    /// </summary>
    public static ChatProviderResponse FromText(string text, ChatIntent intent)
    {
        return new ChatProviderResponse { Text = text, Intent = intent };
    }
}
