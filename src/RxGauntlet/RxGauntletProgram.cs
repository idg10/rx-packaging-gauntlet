using RxGauntlet.Cli;
using RxGauntlet.CommandLine;

using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.AddCommand<RxGauntletAllPublishedRxCommand>("test-all-published-rx");
    config.AddCommand<RxGauntletCandidateCommand>("test-candidate");
});

return await app.RunAsync(args);
