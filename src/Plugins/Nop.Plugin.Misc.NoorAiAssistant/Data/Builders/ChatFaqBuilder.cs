using FluentMigrator.Builders.Create.Table;
using Nop.Data.Mapping.Builders;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Data.Builders;

public class ChatFaqBuilder : NopEntityBuilder<ChatFaq>
{
    /// <summary>
    /// Apply entity configuration
    /// </summary>
    /// <param name="table">Create table expression builder</param>
    public override void MapEntity(CreateTableExpressionBuilder table)
    {
        table
            .WithColumn(nameof(ChatFaq.Question)).AsString(450).NotNullable()
            .WithColumn(nameof(ChatFaq.Answer)).AsString(int.MaxValue).NotNullable()
            .WithColumn(nameof(ChatFaq.Keywords)).AsString(1000).Nullable();
    }
}
