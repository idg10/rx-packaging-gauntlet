#if UseNonUiFrameworkSpecificRxDirectly
using System.Reactive.Linq;
#endif

#if InvokeLibraryMethodThatUsesUiFrameworkSpecificRxFeature
using System.Windows.Threading;
#endif

using Transitive.Lib.UsesRx;


internal class Program
{
    // RxLib.UseRxWpf creates a Dispatcher, so it needs to be running on an STA thread.
    [STAThread]
    private static async Task Main(string[] args)
    {
#if UseNonUiFrameworkSpecificRxDirectly
        Console.WriteLine("App using Rx directly start");
        Observable.Range(1, 1).Subscribe(x => Console.WriteLine($"Received {x} from Observable.Range"));
        Console.WriteLine("App using Rx directly end");
        Console.WriteLine();
#endif


        Console.WriteLine("App using RxLib non-UI start");
        RxLib.UseRx(() => { Console.WriteLine("Callback from RxLib.UseRx"); });
        Console.WriteLine("Yielding after RxLib.UseRx");
        await Task.Yield();
        Console.WriteLine("App using RxLib non-UI end");
        Console.WriteLine();

#if InvokeLibraryMethodThatUsesUiFrameworkSpecificRxFeature
        // This creates a Dispatcher for this thread.
        _ = Dispatcher.CurrentDispatcher;

        Console.WriteLine();
        Console.WriteLine("App using RxLib UI start");
        RxLib.UseRxWpf(() => {  Console.WriteLine("Callback from RxLib.UseRxWpf"); });
        Console.WriteLine("Draining message loop after RxLib.UseRxWpf");
        Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        Dispatcher.Run();
        Console.WriteLine("App using RxLib UI end");
#endif
    }
}
