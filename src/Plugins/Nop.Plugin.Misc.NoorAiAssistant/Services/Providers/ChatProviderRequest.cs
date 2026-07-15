using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Providers;

/// <summary>
/// Represents everything a provider needs to answer one question. Deliberately free of ASP.NET and
/// nopCommerce types: a provider receives data, not a request context, which is what lets an LLM-backed
/// provider be written and unit-tested without a web host.
/// </summary>
public class ChatProviderRequest
{
    /// <summary>
    /// Gets or sets the shopper's message, already sanitised.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Gets or sets the preceding turns, oldest first, already trimmed to the configured history limit.
    /// </summary>
    public IList<ChatTurn> History { get; set; } = new List<ChatTurn>();

    public int CustomerId { get; set; }

    public int StoreId { get; set; }

    public int LanguageId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the active language is written right-to-left. The dummy
    /// provider uses this to pick which language to answer in.
    /// </summary>
    public bool IsRightToLeft { get; set; }

    /// <summary>
    /// Gets or sets the capabilities the store owner has switched on. A provider must not produce a
    /// comparison when <see cref="AllowProductComparison"/> is false, even if it could.
    /// </summary>
    public bool AllowProductRecommendation { get; set; }

    public bool AllowProductComparison { get; set; }

    public bool AllowQuantityEstimation { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of product cards an answer may carry.
    /// </summary>
    public int MaxProducts { get; set; }
}

/// <summary>
/// Represents one preceding turn of the conversation.
/// </summary>
public class ChatTurn
{
    public ChatMessageRole Role { get; set; }

    public string Message { get; set; }
}
