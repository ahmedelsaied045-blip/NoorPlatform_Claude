using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Data.Builders;

public class ChatMessageBuilder : NopEntityBuilder<ChatMessage>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ChatMessage.ChatSessionId)).AsInt32().NotNullable().ForeignKey<ChatSession>()
            .WithColumn(nameof(ChatMessage.Message)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ChatMessage.PayloadJson)).AsString(int.MaxValue).Nullable()
            .WithColumn(nameof(ChatMessage.ProviderSystemName)).AsString(100).Nullable();
    }
}
