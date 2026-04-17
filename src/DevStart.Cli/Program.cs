using System.CommandLine;
using DevStart.Commands;

var root = new RootCommand("dev-start — opinionated .NET scaffolder and day-to-day companion.");

root.AddCommand(NewCommand.Build());
root.AddCommand(AddCommand.Build());
root.AddCommand(DoctorCommand.Build());
root.AddCommand(UpgradeCommand.Build());
root.AddCommand(ListCommand.Build());

return await root.InvokeAsync(args);
