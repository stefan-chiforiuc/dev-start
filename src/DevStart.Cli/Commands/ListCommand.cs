using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

public static class ListCommand
{
    public static Command Build()
    {
        var projectOpt = new Option<string>(["--project", "-p"], () => ".",
            "If set, annotate with what's installed in this project.");
        var treeOpt = new Option<bool>("--tree",
            "Render as a dependency tree instead of a flat table.");

        var cmd = new Command("list", "List available capabilities.") { projectOpt, treeOpt };

        cmd.SetHandler((projectPath, tree) =>
        {
            HashSet<string> installed = [];
            try
            {
                var manifest = Manifest.Load(Path.GetFullPath(projectPath));
                foreach (var c in manifest.Capabilities) installed.Add(c);
            }
            catch
            {
                // Not inside a dev-start project; show plain list.
            }

            if (tree) RenderTree(installed);
            else RenderTable(installed);

            if (installed.Count > 0)
            {
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[green]●[/] installed  [grey]○[/] available");
            }
        }, projectOpt, treeOpt);

        return cmd;
    }

    private static void RenderTable(HashSet<string> installed)
    {
        var table = new Table()
            .AddColumn("status")
            .AddColumn("family")
            .AddColumn("name")
            .AddColumn("version")
            .AddColumn("depends on")
            .AddColumn("description");

        // Group rows by family so backend/deploy variants cluster together.
        var loaded = new List<Capability>();
        foreach (var name in Capability.AvailableNames())
        {
            try { loaded.Add(Capability.LoadEmbedded(name)); }
            catch (Exception ex)
            {
                table.AddRow("[red]![/]", "[grey]—[/]", name, "-", "-", $"[red]{ex.Message}[/]");
            }
        }

        var ordered = loaded
            .OrderBy(c => c.Family is null ? 1 : 0)     // family rows first
            .ThenBy(c => c.Family ?? "", StringComparer.Ordinal)
            .ThenBy(c => c.Framework ?? "", StringComparer.Ordinal)
            .ThenByDescending(c => c.FrameworkVersion ?? "", StringComparer.Ordinal)
            .ThenBy(c => c.Name, StringComparer.Ordinal);

        foreach (var c in ordered)
        {
            var status = installed.Contains(c.Name) ? "[green]●[/]" : "[grey]○[/]";
            var family = c.Family is null
                ? "[grey]—[/]"
                : c.Framework is null
                    ? c.Family
                    : c.FrameworkVersion is null
                        ? $"{c.Family}/{c.Framework}"
                        : $"{c.Family}/{c.Framework} {c.FrameworkVersion}";
            var deps = c.DependsOn.Count > 0
                ? string.Join(", ", c.DependsOn)
                : "[grey]—[/]";
            table.AddRow(status, family, c.Name, c.Version, deps, c.Description);
        }

        AnsiConsole.Write(table);
    }

    private static void RenderTree(HashSet<string> installed)
    {
        // Build reverse dependency map: capability → who depends on it.
        var reverse = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var all = Capability.AvailableNames().ToList();

        foreach (var name in all)
        {
            var c = Capability.LoadEmbedded(name);
            foreach (var dep in c.DependsOn)
            {
                if (!reverse.TryGetValue(dep, out var children))
                {
                    children = [];
                    reverse[dep] = children;
                }
                children.Add(name);
            }
        }

        // Find roots (capabilities no one depends ON but which themselves may have deps).
        // For a cleaner tree start from root capabilities (empty dependsOn).
        var roots = all
            .Select(n => Capability.LoadEmbedded(n))
            .Where(c => c.DependsOn.Count == 0)
            .OrderBy(c => c.Name, StringComparer.Ordinal);

        var tree = new Tree("[bold]Capabilities[/]");
        foreach (var root in roots)
        {
            var node = tree.AddNode(Label(root, installed));
            AddChildren(node, root.Name, reverse, installed);
        }
        AnsiConsole.Write(tree);
    }

    private static void AddChildren(
        TreeNode node, string parent,
        Dictionary<string, List<string>> reverse,
        HashSet<string> installed)
    {
        if (!reverse.TryGetValue(parent, out var children)) return;
        foreach (var childName in children.OrderBy(n => n, StringComparer.Ordinal))
        {
            var child = Capability.LoadEmbedded(childName);
            var childNode = node.AddNode(Label(child, installed));
            AddChildren(childNode, childName, reverse, installed);
        }
    }

    private static string Label(Capability c, HashSet<string> installed)
    {
        var dot = installed.Contains(c.Name) ? "[green]●[/]" : "[grey]○[/]";
        return $"{dot} [bold]{c.Name}[/] [grey]{c.Version}[/] — {c.Description.EscapeMarkup()}";
    }
}
