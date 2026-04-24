namespace DevStart;

/// <summary>Token replacement for file paths and file content.</summary>
public sealed class Tokens
{
    public string Name { get; }            // PascalCase, e.g. "MyApp"
    public string KebabName { get; }       // kebab-case, e.g. "my-app"
    public string LowerName { get; }       // lowercase no dashes, e.g. "myapp"
    public string CamelName { get; }       // camelCase, e.g. "myApp"
    public string ScopedName { get; }      // npm scope form, e.g. "@my-app"

    public Tokens(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            throw new ArgumentException("Project name is required.", nameof(rawName));

        var kebab = Normalize(rawName);
        if (!System.Text.RegularExpressions.Regex.IsMatch(kebab, "^[a-z][a-z0-9-]{0,39}$"))
            throw new ArgumentException(
                $"Project name must be kebab-case, start with a letter, and be 1–40 chars. Got '{rawName}'.",
                nameof(rawName));

        KebabName = kebab;
        Name = ToPascal(kebab);
        LowerName = kebab.Replace("-", "", StringComparison.Ordinal);
        CamelName = ToCamel(kebab);
        ScopedName = "@" + kebab;
    }

    public string Apply(string input) => input
        .Replace("{{Name}}", Name, StringComparison.Ordinal)
        .Replace("{{name}}", KebabName, StringComparison.Ordinal)
        .Replace("{{namelower}}", LowerName, StringComparison.Ordinal)
        .Replace("{{nameCamel}}", CamelName, StringComparison.Ordinal)
        .Replace("{{NameScope}}", ScopedName, StringComparison.Ordinal);

    private static string Normalize(string input) =>
        input.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');

    private static string ToPascal(string kebab)
    {
        var parts = kebab.Split('-', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }

    private static string ToCamel(string kebab)
    {
        var parts = kebab.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return kebab;
        return parts[0] + string.Concat(parts.Skip(1).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
