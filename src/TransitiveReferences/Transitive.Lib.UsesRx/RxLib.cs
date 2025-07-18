using System.Reactive.Concurrency;
using System.Reactive.Disposables;
#if UseNonUiFrameworkRx
using System.Windows.Threading;
#endif

namespace Transitive.Lib.UsesRx;

public static class RxLib
{
#if UseNonUiFrameworkRx
    public static IDisposable UseRxWpf(Action callme)
    {
        Console.WriteLine("RxLib.UseRxWpf enter");

        // This creates a Dispatcher for this thread.
        _ = Dispatcher.CurrentDispatcher;

        Console.WriteLine($"Rx WPF (via lib): {typeof(DispatcherScheduler).Assembly.FullName}");
        IDisposable result = DispatcherScheduler.Current.Schedule(
            default(object),
            (s_, _) =>
            {
                callme();
                return Disposable.Empty;
            });

        Console.WriteLine("Draining message loop after Schedule");
        Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
        Dispatcher.Run();

        Console.WriteLine("RxLib.UseRxWpf exit");
        return result;
    }
#endif

    public static IDisposable UseRx(Action callme)
    {
        Console.WriteLine("RxLib.UseRx enter");
        Console.WriteLine($"Rx (via lib): {typeof(CurrentThreadScheduler).Assembly.FullName}");

        // Using CurrentThreadScheduler because it invokes work items synchronously when it can,
        // and if the Schedule is not recursive (which it won't be in the way this test library
        // is meant to be used by the corresponding Check) Schedule will drain the queue before
        // returning, meaning all outstanding work will complete before we return.
        // (The DefaultScheduler doesn't do that. It schedules work via the thread pool, so
        // there's no particular guarantee of when it will run. For normal apps that's typically
        // fine, but it's not helpful for a console app that runs some simple tests and then
        // immediately exits.)
        IDisposable result = CurrentThreadScheduler.Instance.Schedule(
            default(object),
            (cs, _) =>
            {
                callme();
                return Disposable.Empty;
            });
        Console.WriteLine("RxLib.UseRx exit");
        return result;
    }
}
