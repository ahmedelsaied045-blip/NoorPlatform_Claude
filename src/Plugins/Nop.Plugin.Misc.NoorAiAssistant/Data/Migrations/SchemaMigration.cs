using FluentMigrator;
using Nop.Data.Extensions;
using Nop.Data.Migrations;
using Nop.Plugin.Misc.NoorAiAssistant.Domain;

namespace Nop.Plugin.Misc.NoorAiAssistant.Data.Migrations;

/// <summary>
/// Creates the plugin's tables. Applied by <see cref="Nop.Services.Plugins.PluginService"/> when the
/// plugin is installed and reversed on uninstall (AutoReversingMigration drops the tables in reverse
/// order, so the child tables go before ChatSession).
/// </summary>
[NopMigration("2026-07-14 09:00:00", "Nop.Plugin.Misc.NoorAiAssistant schema", MigrationProcessType.Installation)]
public class SchemaMigration : AutoReversingMigration
{
    public override void Up()
    {
        Create.TableFor<ChatSession>();
        Create.TableFor<ChatMessage>();
        Create.TableFor<ChatEvent>();
        Create.TableFor<ChatFaq>();
        Create.TableFor<SuggestedQuestion>();
    }
}
