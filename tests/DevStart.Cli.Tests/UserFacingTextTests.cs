using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace DevStart.Tests;

/// <summary>
/// Regression guards on text users see in their terminal. The class of bug
/// these prevent: messages that tell the user to run `devstart foo` when
/// the actual binary is `dev-start`. See docs/bug-catalog.md (BUG-004).
/// </summary>
public class UserFacingTextTests
{
    [Fact]
    public void No_user_facing_string_says_devstart_without_dash()
    {
        var sourceDir = FindRepoSourceDir();

        // Only flag forms that look like CLI-invocation instructions to the user:
        // `devstart <verb>` where verb is one of the actual subcommands. This
        // ignores incidental occurrences like `.devstart/baselines.json` or
        // assembly-name metadata, which are not user-facing instructions.
        var pattern = new Regex(
            @"(?<!-|\.)devstart (new|add|doctor|install|upgrade|list|capability|promote|policy)\b",
            RegexOptions.IgnoreCase);

        var offenders = Directory.EnumerateFiles(sourceDir, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => File.ReadLines(path)
                .Where(line => pattern.IsMatch(line))
                .Select(line => $"{Path.GetFileName(path)}: {line.Trim()}"))
            .ToList();

        offenders.Should().BeEmpty(
            "user-facing strings must say 'dev-start' (with the dash) to match the CLI binary name");
    }

    /// <summary>
    /// Walks up from the test-assembly location until it finds the repo root,
    /// then returns the CLI source folder. Works from <c>tests/.../bin/...</c>.
    /// Uses <c>Path.Join</c> so an absolute child segment can't silently
    /// discard the parent path (CodeQL CA1085 / CWE-23 family).
    /// </summary>
    private static string FindRepoSourceDir()
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(typeof(UserFacingTextTests).Assembly.Location)!);
        while (dir is not null && !File.Exists(Path.Join(dir.FullName, "DevStart.sln")))
        {
            dir = dir.Parent;
        }
        if (dir is null) throw new InvalidOperationException("Could not locate repo root.");
        return Path.Join(dir.FullName, "src", "DevStart.Cli");
    }
}
