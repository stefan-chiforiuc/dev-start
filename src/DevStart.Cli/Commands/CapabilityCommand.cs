using System.CommandLine;
using Spectre.Console;

namespace DevStart.Commands;

/// <summary>
/// Scaffolding helpers for capability authors. <c>dev-start capability new foo</c>
/// seeds a new <c>capabilities/foo/</c> folder from the <c>_skeleton</c>.
/// </summary>
public static class CapabilityCommand
{
    public static Command Build()
    {
        var root = new Command("capability", "Tools for capability authors.");
        root.AddCommand(BuildNew());
        return root;
    }

    private static Command BuildNew()
    {
        var nameArg = new Argument<string>("name", "Capability name (kebab-case).");
        var rootOpt = new Option<string>(["--root", "-r"], () => ".",
            "Root of the dev-start checkout (defaults to cwd).");

        var cmd = new Command("new", "Seed a new capability folder from the skeleton.")
        {
            nameArg, rootOpt
        };

        cmd.SetHandler((name, repoRoot) =>
        {
            var validName = System.Text.RegularExpressions.Regex.IsMatch(name, "^[a-z][a-z0-9-]{0,31}$");
            if (!validName)
            {
                AnsiConsole.MarkupLine($"[red]Invalid capability name[/]: must be kebab-case, start with a letter, < 32 chars.");
                return;
            }

            var root = Path.GetFullPath(repoRoot);
            var skeleton = Path.Combine(root, "capabilities", "_skeleton");
            var dest = Path.Combine(root, "capabilities", name);

            if (!Directory.Exists(skeleton))
            {
                AnsiConsole.MarkupLine($"[red]Skeleton not found[/]: {skeleton}. Run this from a dev-start checkout.");
                return;
            }
            if (Directory.Exists(dest))
            {
                AnsiConsole.MarkupLine($"[red]Capability already exists[/]: {dest}");
                return;
            }

            CopyTree(skeleton, dest);
            ReplaceTokens(dest, name);

            AnsiConsole.MarkupLine($"[green]Created[/] capabilities/{name}");
            AnsiConsole.MarkupLine($"[grey]Next:[/]");
            AnsiConsole.MarkupLine($"  edit capabilities/{name}/capability.json (name, description, dependsOn)");
            AnsiConsole.MarkupLine($"  add files under capabilities/{name}/files/");
            AnsiConsole.MarkupLine($"  declare injectors in capabilities/{name}/injectors.json");
        }, nameArg, rootOpt);

        return cmd;
    }

    private static void CopyTree(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var d in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, d);
            Directory.CreateDirectory(Path.Combine(dest, rel));
        }
        foreach (var f in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, f);
            File.Copy(f, Path.Combine(dest, rel), overwrite: false);
        }
    }

    private static void ReplaceTokens(string dir, string name)
    {
        foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(f);
            text = text.Replace("TODO", name, StringComparison.Ordinal);
            File.WriteAllText(f, text);
        }
    }
}
