using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Data.Builders;

public class ChatSessionBuilder : NopEntityBuilder<ChatSession>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ChatSession.SessionGuid)).AsGuid().NotNullable().Indexed()
            .WithColumn(nameof(ChatSession.Title)).AsString(400).Nullable()
            //deliberately not a foreign key to Customer: guest customers are hard-deleted by the
            //"Delete guest customers" scheduled task, and an FK here would make that task fail
            .WithColumn(nameof(ChatSession.CustomerId)).AsInt32().NotNullable().Indexed();
    }
}
