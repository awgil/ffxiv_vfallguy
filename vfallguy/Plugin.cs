using Dalamud.Interface.Windowing;
using Dalamud.Plugin;

namespace vfallguy;

public sealed class Plugin : IDalamudPlugin
{
    public DalamudPluginInterface Dalamud { get; init; }

    public WindowSystem WindowSystem = new("vfallguy");
    private MainWindow _wnd;

    public Plugin(DalamudPluginInterface dalamud)
    {
        dalamud.Create<Service>();

        _wnd = new();
        WindowSystem.AddWindow(_wnd);

        Dalamud = dalamud;
        dalamud.UiBuilder.DisableAutomaticUiHide = true;
        Dalamud.UiBuilder.Draw += WindowSystem.Draw;
        //Dalamud.UiBuilder.OpenConfigUi += () => _wndConfig.IsOpen = true;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        _wnd.Dispose();
    }
}
