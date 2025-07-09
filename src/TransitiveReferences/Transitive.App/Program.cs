using Transitive.Lib.UsesRx;


internal class Program
{
    // RxLib.UseRxWpf creates a Dispatcher, so it needs to be running on an STA thread.
    [STAThread]
    private static void Main(string[] args)
    {
#if UseNonUiFrameworkSpecificRxDirectly
#endif
        RxLib.UseRx(() => { });

#if InvokeLibraryMethodThatUsesUiFrameworkSpecificRxFeature
        RxLib.UseRxWpf(() => { });
#endif
    }
}
