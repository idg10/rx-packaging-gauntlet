using System.Reactive.Disposables;

namespace Transitive.Lib.UsesRx;

public static class RxLib
{
#if WINDOWS
    public static IDisposable UseRxWpf(Action callme)
    {
        return System.Reactive.Concurrency.DispatcherScheduler.Current.Schedule(
            default(object),
            (s_, _) =>
            {
                callme();
                return Disposable.Empty;
            });
    }
#endif

    public static IDisposable UseRx(Action callme)
    {
        return System.Reactive.Concurrency.DefaultScheduler.Instance.Schedule(
            default(object),
            (s_, _) =>
            {
                callme();
                return Disposable.Empty;
            });
    }
}
