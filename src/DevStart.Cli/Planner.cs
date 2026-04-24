using System.Diagnostics;
using Spectre.Console;

namespace DevStart;

/// <summary>
/// Executes a <c>dev-start new</c> scaffold: resolves capability
/// dependencies, copies embedded files (with token substitution) into the
/// target directory, applies injectors, copies the shared platform
/// bundles, writes the manifest, and runs <c>git init</c>.
/// </summary>
public sealed class Planner
{
    public const string StackDotnet = "dotnet-api";
    public const string StackTypescript = "typescript-fastify";

    public string RawName { get; }
    public Tokens Tokens { get; }
    public string Stack { get; }
    public bool MultiService { get; }
    public IReadOnlyList<string> Capabilities { get; }
    public string DeployTarget { get; }
    public bool IncludeClaude { get; }

    public Planner(
        string name,
        bool multiService,
        IEnumerable<string> capabilities,
        string deployTarget,
        bool includeClaude,
        string stack = StackDotnet)
    {
        RawName = name;
        Tokens = new Tokens(name);
        Stack = NormalizeStack(stack);
        MultiService = multiService;
        DeployTarget = deployTarget;
        IncludeClaude = includeClaude;

        var baseCap = BaseCapabilityFor(Stack);
        var gatewayCap = GatewayCapabilityFor(Stack);
        var resolved = new List<string> { baseCap };
        foreach (var c in capabilities)
        {
            if (!resolved.Contains(c, StringComparer.Ordinal)) resolved.Add(c);
        }
        if (multiService && !resolved.Contains(gatewayCap, StringComparer.Ordinal))
        {
            resolved.Add(gatewayCap);
        }
        var deployCap = DeployCapabilityName(deployTarget, Stack);
        if (deployCap is not null && !resolved.Contains(deployCap, StringComparer.Ordinal))
        {
            resolved.Add(deployCap);
        }
        Capabilities = resolved;
    }

    public static string NormalizeStack(string stack) => stack?.ToLowerInvariant() switch
    {
        "dotnet" or "dotnet-api" or "csharp" or ".net" => StackDotnet,
        "typescript" or "typescript-fastify" or "ts" or "node" or "fastify" => StackTypescript,
        null or "" => StackDotnet,
        _ => stack,
    };

    public static string BaseCapabilityFor(string stack) => stack switch
    {
        StackTypescript => "ts-base",
        _ => "base",
    };

    public static string GatewayCapabilityFor(string stack) => stack switch
    {
        StackTypescript => "ts-gateway",
        _ => "gateway",
    };

    public static string? DeployCapabilityName(string target, string stack)
    {
        var key = target?.ToLowerInvariant() switch
        {
            "fly" or "flyio" or "fly.io" => "deploy-fly",
            "aca" or "azure" or "azurecontainerapps" => "deploy-aca",
            _ => null,
        };
        if (key is null) return null;
        return stack == StackTypescript ? "ts-" + key : key;
    }

    public Task RunAsync()
    {
        var target = Path.GetFullPath(Tokens.KebabName);
        Directory.CreateDirectory(target);

        AnsiConsole.MarkupLine($"[grey]target:[/] {target}");

        var baselines = Render(target, verbose: true);

        WriteManifest(target);
        baselines.Save(target);
        TryGitInit(target);

        AnsiConsole.MarkupLine("[green]Done.[/]");
        AnsiConsole.MarkupLine("Next:");
        AnsiConsole.MarkupLine($"  cd {Tokens.KebabName}");
        AnsiConsole.MarkupLine("  just bootstrap");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Render the template into <paramref name="target"/>: capability files +
    /// injectors + platform bundles + CLAUDE.md. Callers that want manifest
    /// + git init should use <see cref="RunAsync"/>. <c>dev-start upgrade</c>
    /// calls this directly against a staging directory.
    /// </summary>
    public Baselines Render(string target, bool verbose = false)
    {
        Directory.CreateDirectory(target);
        var baselines = new Baselines();

        // Platform bundles first — capability injectors may target these
        // (e.g. frontend injects a web service into docker-compose.yml).
        CopyPlatformBundle("platform/compose/", target, target, baselines);
        CopyPlatformBundle("platform/devcontainer/", Path.Join(target, ".devcontainer"), target, baselines);
        if (IncludeClaude)
        {
            CopyClaudeBundle(target, baselines);
        }

        foreach (var cap in Capabilities)
        {
            if (verbose) AnsiConsole.MarkupLine($"[cyan]· capability[/] {cap}");
            CapabilityInstaller.Install(cap, target, Tokens, baselines);
        }

        // CLAUDE.md briefing renders after capabilities so it can enumerate
        // the installed set (delete the templates when done).
        if (IncludeClaude)
        {
            RenderClaudeBriefing(target, baselines);
        }

        WriteMcpConfig(target, baselines);

        return baselines;
    }

    /// <summary>
    /// Writes <c>.mcp.json</c> at the project root with an <c>mcpServers</c>
    /// entry per installed capability that declares one via its
    /// <c>mcp</c> section.
    /// </summary>
    private void WriteMcpConfig(string target, Baselines? baselines)
    {
        var servers = new Dictionary<string, object>(StringComparer.Ordinal);

        foreach (var capName in Capabilities)
        {
            Capability cap;
            try { cap = Capability.LoadEmbedded(capName); }
            catch { continue; }

            foreach (var spec in cap.Mcp)
            {
                if (string.IsNullOrWhiteSpace(spec.Name) || string.IsNullOrWhiteSpace(spec.Command))
                    continue;

                var entry = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["command"] = spec.Command,
                    ["args"] = spec.Args.Select(a => Tokens.Apply(a)).ToArray(),
                };
                if (spec.Env is { Count: > 0 })
                {
                    entry["env"] = spec.Env.ToDictionary(kv => kv.Key, kv => (object)Tokens.Apply(kv.Value));
                }
                servers[spec.Name] = entry;
            }
        }

        var config = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["mcpServers"] = servers,
        };

        var json = System.Text.Json.JsonSerializer.Serialize(config,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        var path = Path.Join(target, ".mcp.json");
        File.WriteAllText(path, json);
        baselines?.Record(".mcp.json", json);
    }

    /// <summary>
    /// Copy <c>platform/claude/</c> into the target's <c>.claude/</c>. Skills
    /// are stack-filtered: files under <c>skills/dotnet/</c> are copied only
    /// when the stack is .NET (and land at <c>.claude/skills/</c>); same for
    /// <c>skills/typescript/</c>. Files outside those subfolders are always
    /// copied as-is.
    /// </summary>
    private void CopyClaudeBundle(string target, Baselines? baselines)
    {
        const string prefix = "platform/claude/";
        var destRoot = Path.Join(target, ".claude");

        foreach (var rel in Capability.ResourceNamesUnder(prefix))
        {
            var routed = RouteClaudePath(rel, Stack);
            if (routed is null) continue; // skipped: wrong stack

            var bytes = Capability.ReadBytes(prefix + rel)
                ?? throw new InvalidOperationException($"Missing platform resource {prefix}{rel}");

            var appliedRel = Tokens.Apply(routed);
            var dest = Path.Join(destRoot, appliedRel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            byte[] content;
            if (IsText(rel))
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                content = System.Text.Encoding.UTF8.GetBytes(Tokens.Apply(text));
            }
            else
            {
                content = bytes;
            }
            File.WriteAllBytes(dest, content);
            var relativeFromProject = Path.GetRelativePath(target, dest);
            baselines?.Record(relativeFromProject, content);
        }
    }

    public static string? RouteClaudePath(string rel, string stack)
    {
        const string dotnetPrefix = "skills/dotnet/";
        const string tsPrefix = "skills/typescript/";
        if (rel.StartsWith(dotnetPrefix, StringComparison.Ordinal))
        {
            return stack == StackTypescript ? null : "skills/" + rel[dotnetPrefix.Length..];
        }
        if (rel.StartsWith(tsPrefix, StringComparison.Ordinal))
        {
            return stack == StackTypescript ? "skills/" + rel[tsPrefix.Length..] : null;
        }
        return rel;
    }

    private void CopyPlatformBundle(string resourcePrefix, string destRoot, string projectRoot, Baselines? baselines)
    {
        foreach (var rel in Capability.ResourceNamesUnder(resourcePrefix))
        {
            var bytes = Capability.ReadBytes(resourcePrefix + rel)
                ?? throw new InvalidOperationException($"Missing platform resource {resourcePrefix}{rel}");

            var appliedRel = Tokens.Apply(rel);
            var dest = Path.Join(destRoot, appliedRel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);

            byte[] content;
            if (IsText(rel))
            {
                var text = System.Text.Encoding.UTF8.GetString(bytes);
                content = System.Text.Encoding.UTF8.GetBytes(Tokens.Apply(text));
            }
            else
            {
                content = bytes;
            }
            File.WriteAllBytes(dest, content);

            // Record with a path relative to the project root, not destRoot —
            // upgrade --apply needs the same key shape as CapabilityInstaller.
            var relativeFromProject = Path.GetRelativePath(projectRoot, dest);
            baselines?.Record(relativeFromProject, content);
        }
    }

    private void RenderClaudeBriefing(string target, Baselines? baselines)
    {
        // Stack-aware template name. Existing v1.0 content continues to live
        // as CLAUDE.md.dotnet.template; TS scaffolds ship a second variant.
        var templateName = Stack == StackTypescript
            ? "CLAUDE.md.typescript.template"
            : "CLAUDE.md.dotnet.template";
        var templatePath = Path.Join(target, ".claude", templateName);

        // Back-compat: old bundles may still carry CLAUDE.md.template.
        if (!File.Exists(templatePath))
        {
            var legacy = Path.Join(target, ".claude", "CLAUDE.md.template");
            if (File.Exists(legacy)) templatePath = legacy;
            else return;
        }

        var content = File.ReadAllText(templatePath);

        var caps = string.Join(Environment.NewLine, Capabilities.Select(c =>
        {
            try
            {
                var info = Capability.LoadEmbedded(c);
                return $"- **{info.Name}** — {info.Description}";
            }
            catch
            {
                return $"- **{c}**";
            }
        }));

        var adrs = string.Join(Environment.NewLine, Stack == StackTypescript
            ? new[]
            {
                "- ADR 0001 — Record architecture decisions",
                "- ADR 0002 — Fastify plugins as the composition unit",
                "- ADR 0003 — Kysely + postgres driver (no ORM until you need one)",
                "- ADR 0004 — Pino + OpenTelemetry Node SDK",
                "- ADR 0005 — Zod at every boundary (HTTP, env, events)",
                "- ADR 0006 — Capabilities, not monolithic templates",
                "- ADR 0007 — Injectors, not per-capability template forks",
                "- ADR 0008 — ts-* prefix for TypeScript-stack capabilities",
            }
            : new[]
            {
                "- ADR 0001 — Record architecture decisions",
                "- ADR 0002 — Minimal APIs (not controllers)",
                "- ADR 0003 — EF Core + Npgsql",
                "- ADR 0004 — Serilog + OpenTelemetry",
                "- ADR 0005 — CQRS with MediatR + outbox",
                "- ADR 0006 — Capabilities, not monolithic templates",
                "- ADR 0007 — Injectors, not per-capability template forks",
                "- ADR 0008 — ts-* prefix for TypeScript-stack capabilities",
            });

        var extras = new List<string>();
        if (Capabilities.Contains("queue", StringComparer.Ordinal)
            || Capabilities.Contains("ts-queue", StringComparer.Ordinal))
            extras.Add("- RabbitMQ: `localhost:5672` (management UI at :15672)");
        if (Capabilities.Contains("cache", StringComparer.Ordinal)
            || Capabilities.Contains("ts-cache", StringComparer.Ordinal))
            extras.Add("- Redis: `localhost:6379`");

        content = content
            .Replace("{{ProjectName}}", Tokens.Name, StringComparison.Ordinal)
            .Replace("{{CapabilitiesList}}", caps, StringComparison.Ordinal)
            .Replace("{{AdrList}}", adrs, StringComparison.Ordinal)
            .Replace("{{ConditionalServices}}", string.Join(Environment.NewLine, extras), StringComparison.Ordinal);

        var claudeMdPath = Path.Join(target, ".claude", "CLAUDE.md");
        File.WriteAllText(claudeMdPath, content);
        baselines?.Record(Path.GetRelativePath(target, claudeMdPath), content);

        // Delete both possible template files if present, so staging doesn't
        // leave .template files behind.
        foreach (var name in new[] { "CLAUDE.md.dotnet.template", "CLAUDE.md.typescript.template", "CLAUDE.md.template" })
        {
            var p = Path.Join(target, ".claude", name);
            if (File.Exists(p))
            {
                baselines?.Forget(Path.GetRelativePath(target, p));
                File.Delete(p);
            }
        }
    }

    private void WriteManifest(string target)
    {
        var services = BuildServices();
        var manifest = new Manifest
        {
            Name = Tokens.KebabName,
            Stack = Stack,
            Capabilities = [.. Capabilities],
            Services = services,
            Deploy = DeployTarget,
            TemplateVersion = CliVersion.Current,
        };
        manifest.Save(target);
    }

    private List<string> BuildServices()
    {
        var services = MultiService
            ? new List<string> { "gateway", "users", "orders" }
            : new List<string> { "api" };
        if (Capabilities.Contains("frontend", StringComparer.Ordinal) && !services.Contains("web"))
        {
            services.Add("web");
        }
        return services;
    }

    private static void TryGitInit(string target)
    {
        try
        {
            Run("git", "init -b main", target);
            Run("git", "add -A", target);
            Run("git", "-c user.name=dev-start -c user.email=noreply@dev-start.dev commit -m \"chore: initial scaffold by dev-start\" --no-gpg-sign --quiet", target);
        }
        catch
        {
            AnsiConsole.MarkupLine("[yellow]git init skipped[/] — run it manually.");
        }
    }

    private static void Run(string cmd, string args, string cwd)
    {
        var psi = new ProcessStartInfo(cmd, args)
        {
            WorkingDirectory = cwd,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var p = Process.Start(psi)!;
        p.WaitForExit();
        if (p.ExitCode != 0)
        {
            throw new InvalidOperationException($"{cmd} {args} failed: {p.StandardError.ReadToEnd()}");
        }
    }

    private static bool IsText(string path)
    {
        var ext = Path.GetExtension(path);
        return ext switch
        {
            ".cs" or ".csproj" or ".json" or ".jsonc" or ".yaml" or ".yml" or ".md"
            or ".http" or ".props" or ".targets" or ".sln" or ".editorconfig"
            or ".gitignore" or ".gitkeep" or ".sh" or ".ps1" or ".cmd"
            or ".dockerfile" or ".env" or ".example" or ".xml" or ".html"
            or ".css" or ".js" or ".ts" or ".tsx" or ".jsx" or ".mjs" or ".cjs"
            or ".toml" or ".bicep" or ".tf" or ".sql" or ".graphql" or ".svg"
            or "" => true,
            _ => Path.GetFileName(path) is "Dockerfile" or "justfile" or "Tiltfile"
                or "pnpm-workspace.yaml" or "pnpm-lock.yaml",
        };
    }
}
