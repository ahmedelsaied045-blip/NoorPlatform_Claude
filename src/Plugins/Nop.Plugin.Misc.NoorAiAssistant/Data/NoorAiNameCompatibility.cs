using Nop.Data.Mapping;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Data;

/// <summary>
/// Prefixes the plugin's tables so they group together in SSMS and can never collide with a core table
/// (nopCommerce would otherwise name them ChatSession, ChatMessage, ...).
/// </summary>
public class NoorAiNameCompatibility : INameCompatibility
{
    public Dictionary<Type, string> TableNames => new()
    {
        [typeof(ChatSession)] = "NoorAIChatSession",
        [typeof(ChatMessage)] = "NoorAIChatMessage",
        [typeof(SuggestedQuestion)] = "NoorAISuggestedQuestion",
        [typeof(ChatFaq)] = "NoorAIFaq",
        [typeof(ChatEvent)] = "NoorAIChatEvent"
    };

    public Dictionary<(Type, string), string> ColumnName => new();
}
