using DeadworksManaged.Api;

namespace LockTimer;

public class LockTimerPlugin : DeadworksPluginBase
{
    public override string Name => "LockTimer";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] {(isReload ? "Reloaded" : "Loaded")}.");
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] Unloaded.");
    }

    public override void OnStartupServer()
    {
        Console.WriteLine($"[{Name}] OnStartupServer fired for map '{Server.MapName}'.");
    }
}
