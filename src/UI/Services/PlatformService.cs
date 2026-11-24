namespace MCEPatcher.UI.Services;

public static class PlatformService
{
    private static IPlatformService _current = new DesktopPlatformService();

    public static IPlatformService Current
    {
        get => _current;
        set => _current = value ?? new DesktopPlatformService();
    }
}
