using FluentAssertions;
using HomeInventory.Application;
using HomeInventory.Application.Assistant.Tools;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HomeInventory.Application.UnitTests.Assistant;

public class AssistantToolRegistrationTests
{
    // Verbs that would indicate a state-changing tool; none must be exposed to the assistant.
    private static readonly string[] MutationVerbs =
        ["add", "create", "update", "delete", "move", "consume", "discard", "remove", "set", "join"];

    [Fact]
    public void Only_the_read_only_tools_are_registered()
    {
        using var provider = new ServiceCollection().AddLogging().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();

        var names = scope.ServiceProvider.GetServices<IAssistantTool>()
            .Select(t => t.Name)
            .ToList();

        names.Should().BeEquivalentTo(
        [
            "search_inventory",
            "get_item_details",
            "get_location_contents",
            "list_locations",
            "get_expiring_stock",
            "get_inventory_summary",
        ]);
    }

    [Fact]
    public void No_registered_tool_can_mutate_data()
    {
        using var provider = new ServiceCollection().AddLogging().AddApplication().BuildServiceProvider();
        using var scope = provider.CreateScope();

        var names = scope.ServiceProvider.GetServices<IAssistantTool>().Select(t => t.Name);

        foreach (var name in names)
        {
            MutationVerbs.Should().NotContain(verb => name.StartsWith($"{verb}_", StringComparison.Ordinal),
                $"the assistant must be read-only, but '{name}' looks like a mutation");
        }
    }
}
