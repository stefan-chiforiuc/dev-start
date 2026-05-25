using System.Runtime.InteropServices;

namespace DevStart.Install;

public enum OsFamily { Unknown, MacOs, Windows, Debian, Fedora, Arch, Alpine, OpenSuse, OtherLinux }

public sealed record OsInfo(OsFamily Family, string Id, string IdLike, string PrettyName)
{
    public bool IsLinux => Family is OsFamily.Debian or OsFamily.Fedora or OsFamily.Arch
                                       or OsFamily.Alpine or OsFamily.OpenSuse or OsFamily.OtherLinux;
    public bool IsWindows => Family == OsFamily.Windows;
    public bool IsMac => Family == OsFamily.MacOs;
}

public static class OsProbe
{
    public static OsInfo Detect()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return new OsInfo(OsFamily.MacOs, "macos", "", "macOS");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return new OsInfo(OsFamily.Windows, "windows", "", "Windows");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            try
            {
                var content = File.Exists("/etc/os-release")
                    ? File.ReadAllText("/etc/os-release")
                    : "";
                return Parse(content);
            }
            catch (IOException)
            {
                return new OsInfo(OsFamily.OtherLinux, "linux", "", "Linux");
            }
        }

        return new OsInfo(OsFamily.Unknown, "unknown", "", "unknown");
    }

    internal static OsInfo Parse(string osReleaseContent)
    {
        var kv = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in osReleaseContent.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;
            var eq = line.IndexOf('=');
            if (eq < 0) continue;
            var key = line[..eq].Trim();
            var value = line[(eq + 1)..].Trim().Trim('"');
            kv[key] = value;
        }

        kv.TryGetValue("ID", out var id);
        kv.TryGetValue("ID_LIKE", out var idLike);
        kv.TryGetValue("PRETTY_NAME", out var pretty);
        id ??= "linux";
        idLike ??= "";
        pretty ??= "Linux";

        var family = Classify(id, idLike);
        return new OsInfo(family, id, idLike, pretty);
    }

    private static OsFamily Classify(string id, string idLike)
    {
        var hay = ($"{id} {idLike}").ToLowerInvariant();
        if (Contains(hay, "debian") || Contains(hay, "ubuntu") || Contains(hay, "mint")) return OsFamily.Debian;
        if (Contains(hay, "fedora") || Contains(hay, "rhel") || Contains(hay, "rocky")
            || Contains(hay, "centos") || Contains(hay, "almalinux")) return OsFamily.Fedora;
        if (Contains(hay, "arch") || Contains(hay, "manjaro")) return OsFamily.Arch;
        if (Contains(hay, "alpine")) return OsFamily.Alpine;
        if (Contains(hay, "suse") || Contains(hay, "opensuse")) return OsFamily.OpenSuse;
        return OsFamily.OtherLinux;
    }

    private static bool Contains(string hay, string needle)
        => hay.Contains(needle, StringComparison.Ordinal);
}
