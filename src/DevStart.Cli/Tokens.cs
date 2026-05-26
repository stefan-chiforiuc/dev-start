namespace DevStart;

using System.Text.RegularExpressions;

/// <summary>Token replacement for file paths and file content.</summary>
public sealed class Tokens
{
    public string Name { get; }            // PascalCase, dotted segments preserved: "My.Cool.App" or "MyApp"
    public string KebabName { get; }       // kebab-case, dots flattened to dashes: "my-cool-app"
    public string LowerName { get; }       // lowercase no separators (Docker/npm scope): "mycoolapp"
    public string CamelName { get; }       // camelCase first segment only: "myCoolApp"
    public string ScopedName { get; }      // npm scope form: "@my-cool-app"
    public string DotName { get; }         // explicit dotted form: "My.Cool.App" (== Name when input had dots)
    public string FolderName { get; }      // user's typed input, sanitized — preserves case and dots: "My.Cool.App", "my-app", "MyApp"

    // Per-segment validation: each segment between dots must start with a
    // letter, then letters/digits/hyphens, 1–40 chars; 1–6 segments; up to
    // 60 chars total. Matches what `dotnet new sln -n` accepts without
    // surprising the user.
    private static readonly Regex SegmentRegex = new("^[a-zA-Z][a-zA-Z0-9-]{0,39}$", RegexOptions.Compiled);

    public Tokens(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            throw new DevStartUserException(
                "Project name is required.",
                "Example: dev-start new My.Cool.App");

        var input = rawName.Trim().Replace('_', '-').Replace(' ', '-');

        if (input.StartsWith('.') || input.EndsWith('.') || input.Contains(".."))
            throw new DevStartUserException(
                $"Project name '{rawName}' has a misplaced dot. Dots are only allowed between segments.",
                "Try: My.Cool.App");

        if (input.Length > 60)
            throw new DevStartUserException(
                $"Project name '{rawName}' is {input.Length} chars; keep it ≤ 60.",
                null);

        var segments = input.Split('.');
        if (segments.Length > 6)
            throw new DevStartUserException(
                $"Project name '{rawName}' has {segments.Length} dotted segments; keep it ≤ 6.",
                null);

        var invalidSegment = segments.FirstOrDefault(s => !SegmentRegex.IsMatch(s));
        if (invalidSegment is not null)
            throw new DevStartUserException(
                $"Project name segment '{invalidSegment}' is invalid.",
                "Each segment must start with a letter and contain only letters, digits, and hyphens (e.g. 'My-App').");

        // Name: PascalCase per segment, rejoined with dots.
        Name = string.Join('.', segments.Select(PascalSegment));
        DotName = Name;

        // FolderName: user's input, lightly normalized (trim, _/space → -),
        // preserving case and dots so the on-disk folder matches what they
        // typed. For `MyApp` → "MyApp", `my-app` → "my-app",
        // `My.Cool.App` → "My.Cool.App".
        FolderName = input;

        // KebabName: lowercase, dots -> dashes, hyphens collapsed.
        var kebab = string.Join('-', segments.Select(s => s.ToLowerInvariant()));
        KebabName = Regex.Replace(kebab, "-+", "-").Trim('-');

        LowerName = KebabName.Replace("-", "", StringComparison.Ordinal);
        CamelName = ToCamel(KebabName);
        ScopedName = "@" + KebabName;
    }

    public string Apply(string input) => input
        .Replace("{{Name}}", Name, StringComparison.Ordinal)
        .Replace("{{DotName}}", DotName, StringComparison.Ordinal)
        .Replace("{{name}}", KebabName, StringComparison.Ordinal)
        .Replace("{{namelower}}", LowerName, StringComparison.Ordinal)
        .Replace("{{nameCamel}}", CamelName, StringComparison.Ordinal)
        .Replace("{{NameScope}}", ScopedName, StringComparison.Ordinal);

    // Pascal-case a single segment (input may contain hyphens from kebab-style
    // segments like "my-app" => "MyApp"). Pure ASCII letters.
    private static string PascalSegment(string segment)
    {
        var parts = segment.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return segment;
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private static string ToCamel(string kebab)
    {
        var parts = kebab.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return kebab;
        return parts[0] + string.Concat(parts.Skip(1).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
