using System.Reflection;

namespace DevStart;

/// <summary>
/// Shared lookup over the CLI assembly's embedded resources. Capability and
/// Policy both need the same normalization (Windows-built resources can ship
/// with '\' separators; we canonicalise to '/') and the same read-by-name
/// behaviour. Building the index once avoids N walks of
/// <see cref="Assembly.GetManifestResourceNames"/>.
/// </summary>
internal static class EmbeddedResourceIndex
{
    private static readonly Lazy<Dictionary<string, string>> _index =
        new(Build, isThreadSafe: true);

    public static Dictionary<string, string> Map => _index.Value;

    public static IEnumerable<string> KeysUnder(string prefix) =>
        Map.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal));

    public static byte[]? ReadBytes(string logicalName)
    {
        var normalized = Normalize(logicalName);
        if (!Map.TryGetValue(normalized, out var actual)) return null;
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream(actual);
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public static string? ReadText(string logicalName)
    {
        var bytes = ReadBytes(logicalName);
        return bytes is null ? null : System.Text.Encoding.UTF8.GetString(bytes);
    }

    public static string Normalize(string name) => name.Replace('\\', '/');

    private static Dictionary<string, string> Build()
    {
        var asm = Assembly.GetExecutingAssembly();
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in asm.GetManifestResourceNames())
        {
            map[Normalize(name)] = name;
        }
        return map;
    }
}
