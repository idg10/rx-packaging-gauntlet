using CheckIssue97;

using Spectre.Console.Cli;


var app = new CommandApp<CheckPluginIssue97Command>();

return await app.RunAsync(args);
