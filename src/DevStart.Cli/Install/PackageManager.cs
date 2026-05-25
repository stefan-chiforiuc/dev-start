namespace DevStart.Install;

public abstract class PackageManager
{
    public abstract string Name { get; }
    public abstract bool RequiresElevation { get; }

    // Returns the catalog field used for this manager (brew/apt/dnf/winget).
    public abstract string? PackageFor(ToolEntry entry);

    // Builds a shell-ready command for the user / runner. Null when no mapping.
    public virtual string? BuildInstallCommand(ToolEntry entry)
    {
        var pkg = PackageFor(entry);
        if (pkg is null) return null;
        return BuildCommand(pkg);
    }

    protected abstract string BuildCommand(string pkg);
}

public sealed class BrewPackageManager : PackageManager
{
    public override string Name => "brew";
    public override bool RequiresElevation => false;
    public override string? PackageFor(ToolEntry entry) => entry.BrewPkg;
    protected override string BuildCommand(string pkg) => $"brew install {pkg}";
}

public sealed class AptPackageManager : PackageManager
{
    public override string Name => "apt";
    public override bool RequiresElevation => true;
    public override string? PackageFor(ToolEntry entry) => entry.AptPkg;
    protected override string BuildCommand(string pkg) => $"sudo apt-get install -y {pkg}";
}

public sealed class DnfPackageManager : PackageManager
{
    public override string Name => "dnf";
    public override bool RequiresElevation => true;
    public override string? PackageFor(ToolEntry entry) => entry.DnfPkg;
    protected override string BuildCommand(string pkg) => $"sudo dnf install -y {pkg}";
}

public sealed class WingetPackageManager : PackageManager
{
    public override string Name => "winget";
    public override bool RequiresElevation => false;
    public override string? PackageFor(ToolEntry entry) => entry.WingetId;
    protected override string BuildCommand(string pkg)
        => $"winget install --id {pkg} --accept-package-agreements --accept-source-agreements -e";
}

public sealed class NullPackageManager : PackageManager
{
    public override string Name => "manual";
    public override bool RequiresElevation => false;
    public override string? PackageFor(ToolEntry entry) => null;
    public override string? BuildInstallCommand(ToolEntry entry) => null;
    protected override string BuildCommand(string pkg) => throw new NotSupportedException();
}

public static class PackageManagerFactory
{
    public static PackageManager Detect(OsInfo os) => os.Family switch
    {
        OsFamily.MacOs => new BrewPackageManager(),
        OsFamily.Debian => new AptPackageManager(),
        OsFamily.Fedora => new DnfPackageManager(),
        OsFamily.Windows => new WingetPackageManager(),
        _ => new NullPackageManager(),
    };
}
