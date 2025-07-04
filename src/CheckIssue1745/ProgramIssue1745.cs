using CheckIssue1745;

using Spectre.Console.Cli;

var app = new CommandApp<CheckDeploymentBloatCommand>();

return await app.RunAsync(args);