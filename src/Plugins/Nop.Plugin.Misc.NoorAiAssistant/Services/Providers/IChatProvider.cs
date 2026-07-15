namespace Nop.Plugin.Misc.NoorAiAssistant.Services.Providers;

/// <summary>
/// Represents the one thing that has to change to move from canned answers to a real model.
/// </summary>
/// <remarks>
/// The seam is deliberately narrow: a provider takes a <see cref="ChatProviderRequest"/> (message,
/// history, capability flags) and returns a <see cref="ChatProviderResponse"/> (markdown + structured
/// payload). It never touches HTTP, persistence, rate limiting or analytics — <see cref="IChatService"/>
/// owns all of that and calls the provider in the middle.
///
/// To add OpenAI / Azure OpenAI / Claude / Gemini / Ollama / DeepSeek:
///   1. implement this interface in Services/Providers/,
///   2. register it with services.AddScoped&lt;IChatProvider, YourProvider&gt;() in PluginNopStartup,
///   3. set NoorAiAssistantSettings.ActiveProviderSystemName to its <see cref="SystemName"/> in the admin.
/// The catalogue services (IProductSearchService, IRecommendationService, IProductComparisonService,
/// ILightingCalculator, IChatFaqService) are the tools such a provider would expose as function calls —
/// they are already provider-agnostic, so none of them need to change.
/// </remarks>
public interface IChatProvider
{
    /// <summary>
    /// Gets the system name this provider is selected by in the admin settings, e.g. "Dummy", "OpenAI".
    /// </summary>
    string SystemName { get; }

    /// <summary>
    /// Gets the name shown in the provider dropdown in the admin.
    /// </summary>
    string FriendlyName { get; }

    /// <summary>
    /// Gets a value indicating whether the provider is usable right now. A provider that needs an API key
    /// returns false until one is configured, which lets the admin explain why it cannot be selected
    /// instead of failing at the first shopper message.
    /// </summary>
    Task<bool> IsConfiguredAsync();

    /// <summary>
    /// Answers one question.
    /// </summary>
    /// <param name="request">The message, the conversation so far, and what the store allows</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains the answer.
    /// </returns>
    Task<ChatProviderResponse> GetResponseAsync(ChatProviderRequest request);
}
