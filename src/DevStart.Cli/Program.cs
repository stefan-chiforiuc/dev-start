using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using DevStart;
using DevStart.Commands;
using Spectre.Console;

var root = new RootCommand("dev-start — opinionated .NET scaffolder and day-to-day companion.");

root.AddCommand(NewCommand.Build());
root.AddCommand(AddCommand.Build());
root.AddCommand(DoctorCommand.Build());
root.AddCommand(InstallCommand.Build());
root.AddCommand(UpgradeCommand.Build());
root.AddCommand(ListCommand.Build());
root.AddCommand(CapabilityCommand.Build());
root.AddCommand(PromoteCommand.Build());
root.AddCommand(PolicyCommand.Build());

var debug = Environment.GetEnvironmentVariable("DEV_START_DEBUG") == "1";

// Custom exception handler so DevStartUserException renders as a friendly
// red line + hint, and unexpected exceptions show a tight one-liner with
// a pointer to DEV_START_DEBUG instead of a raw 30-line stack trace.
var parser = new CommandLineBuilder(root)
    .UseDefaults()
    .UseExceptionHandler((ex, ctx) =>
    {
        if (ex is DevStartUserException ux)
        {
            AnsiConsole.MarkupLine($"[red]error:[/] {Markup.Escape(ux.Message)}");
            if (!string.IsNullOrEmpty(ux.Hint))
                AnsiConsole.MarkupLine($"[grey]hint:[/]  {Markup.Escape(ux.Hint)}");
            if (debug) AnsiConsole.WriteException(ux);
            ctx.ExitCode = 1;
            return;
        }
        AnsiConsole.MarkupLine($"[red]unexpected error:[/] {Markup.Escape(ex.Message)}");
        AnsiConsole.MarkupLine("[grey]Set DEV_START_DEBUG=1 for a stack trace, or file a bug at[/] https://github.com/stefan-chiforiuc/dev-start/issues");
        if (debug) AnsiConsole.WriteException(ex);
        ctx.ExitCode = 2;
    })
    .Build();

return await parser.InvokeAsync(args);
