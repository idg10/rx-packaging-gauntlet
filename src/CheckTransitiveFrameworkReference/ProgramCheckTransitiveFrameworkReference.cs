using CheckTransitiveFrameworkReference;

using Spectre.Console.Cli;


var app = new CommandApp<CheckTransitiveFrameworkReferenceCommand>();

return await app.RunAsync(args);
