using CheckDisableTransitiveFailingExtensionMethod;

using Spectre.Console.Cli;



var app = new CommandApp<CheckDisableTransitiveFailingExtensionMethodCommand>();

return await app.RunAsync(args);