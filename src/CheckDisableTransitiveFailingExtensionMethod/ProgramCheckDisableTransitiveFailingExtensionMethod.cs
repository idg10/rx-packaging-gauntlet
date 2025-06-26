


using CheckDisableTransitiveFailingExtensionMethod;

using RxGauntlet;
using RxGauntlet.Build;
using RxGauntlet.LogModel;
using RxGauntlet.Xml;

using Spectre.Console.Cli;

using System.Reflection.Metadata;
using System.Xml;
using System.Xml.Linq;



var app = new CommandApp<CheckDisableTransitiveFailingExtensionMethodCommand>();

app.Configure(config =>
{
    //config.AddCommand<CheckDisableTransitiveFailingExtensionMethodCommand>("check");
});

return await app.RunAsync(args);