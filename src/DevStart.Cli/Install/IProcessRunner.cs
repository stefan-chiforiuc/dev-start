using System.Diagnostics;

namespace DevStart.Install;

public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, string args, CancellationToken ct = default);
}

public sealed class DefaultProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string fileName, string args, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo(fileName, args)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        try
        {
            using var p = Process.Start(psi) ?? throw new InvalidOperationException("Process.Start returned null.");
            var stdOutTask = p.StandardOutput.ReadToEndAsync(ct);
            var stdErrTask = p.StandardError.ReadToEndAsync(ct);
            await p.WaitForExitAsync(ct);
            return new ProcessResult(p.ExitCode, await stdOutTask, await stdErrTask);
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return new ProcessResult(-1, "", $"command not found: {fileName}");
        }
    }
}
