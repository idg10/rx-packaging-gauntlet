namespace CheckTransitiveFrameworkReference;

internal record RxPackagingChoice(
    SystemReactiveRole SystemReactiveRole,
    bool UiTypesVisibleInSystemReactive,
    bool SystemReactiveSuppliesDesktopFrameworkReference);


internal enum SystemReactiveRole
{
    MainRxComponent,
    LegacyFacade,
}