using RxGauntlet;

using Spectre.Console.Cli;

var app = new CommandApp<RxGauntletCommand>();

app.Configure(config =>
{
    //config.AddCommand<ListTestTypesCommand>("list-tests");
});

return await app.RunAsync(args);