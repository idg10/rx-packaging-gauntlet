using CheckDisableTransitiveFailingExtensionMethod;

using Spectre.Console.Cli;



var app = new CommandApp<CheckDisableTransitiveFailingExtensionMethodCommand>();

app.Configure(config =>
{
    //config.AddCommand<CheckDisableTransitiveFailingExtensionMethodCommand>("check");
});

return await app.RunAsync(args);