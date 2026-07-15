namespace Nop.Plugin.Misc.NoorAiAssistant.Domain;

/// <summary>
/// Represents the author of a chat message
/// </summary>
public enum ChatMessageRole
{
    /// <summary>
    /// Written by the shopper
    /// </summary>
    User = 10,

    /// <summary>
    /// Written by the assistant (i.e. the active <see cref="Services.Providers.IChatProvider"/>)
    /// </summary>
    Assistant = 20,

    /// <summary>
    /// Instructions/context. Never rendered in the UI, but passed to the provider.
    /// </summary>
    System = 30
}
