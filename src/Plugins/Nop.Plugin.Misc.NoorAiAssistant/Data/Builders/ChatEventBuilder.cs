using FluentMigrator.Builders.Create.Table;
using Nop.Data.Extensions;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Data.Builders;

public class ChatEventBuilder : NopEntityBuilder<ChatEvent>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ChatEvent.ChatSessionId)).AsInt32().NotNullable().ForeignKey<ChatSession>()
            //every analytics report groups by (EventTypeId, EventKey) or (EventTypeId, ProductId)
            .WithColumn(nameof(ChatEvent.EventTypeId)).AsInt32().NotNullable().Indexed()
            .WithColumn(nameof(ChatEvent.EventKey)).AsString(450).Nullable().Indexed()
            .WithColumn(nameof(ChatEvent.ProductId)).AsInt32().NotNullable().Indexed();
    }
}
