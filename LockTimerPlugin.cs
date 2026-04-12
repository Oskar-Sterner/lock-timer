using System.IO;
using System.Linq;
using DeadworksManaged.Api;
using LockTimer.Commands;
using LockTimer.Data;
using LockTimer.Records;
using LockTimer.Timing;
using LockTimer.Zones;

namespace LockTimer;

public class LockTimerPlugin : DeadworksPluginBase
{
    public override string Name => "LockTimer";

    private LockTimerDb? _db;
    private ZoneRepository? _zones;
    private RecordRepository? _records;
    private ZoneRenderer? _renderer;
    private TimerEngine? _engine;
    private ZoneEditor? _editor;
    private ChatCommands? _commands;

    public override void OnLoad(bool isReload)
    {
        try
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "LockTimer");
            Directory.CreateDirectory(dir);
            var dbPath = Path.Combine(dir, "locktimer.db");

            _db       = LockTimerDb.Open(dbPath);
            _zones    = new ZoneRepository(_db.Connection);
            _records  = new RecordRepository(_db.Connection);
            _renderer = new ZoneRenderer();
            _engine   = new TimerEngine();
            _editor   = new ZoneEditor(_zones, _engine);
            _commands = new ChatCommands(_editor, _renderer, _records, _engine);

            // PluginRegistry.RegisterChatCommands does not exist in the vendored API.
            // The plugin loader (PluginLoader.ChatCommands.cs) scans plugin.GetType().GetMethods()
            // for [ChatCommand] attributes — only methods on the plugin class itself are found.
            // Thin wrappers below delegate to _commands.

            Console.WriteLine($"[{Name}] {(isReload ? "Reloaded" : "Loaded")}. DB: {dbPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] OnLoad failed: {ex}");
        }
    }

    public override void OnUnload()
    {
        try
        {
            _renderer?.ClearAll();
            _db?.Dispose();
            Console.WriteLine($"[{Name}] Unloaded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] OnUnload failed: {ex}");
        }
    }

    public override void OnStartupServer()
    {
        try
        {
            if (_zones is null || _engine is null || _renderer is null) return;

            _renderer.ClearAll();
            _engine.ResetAll();

            var map = Server.MapName;
            if (string.IsNullOrEmpty(map)) return;

            var zones = _zones.GetForMap(map);
            var start = zones.FirstOrDefault(z => z.Kind == ZoneKind.Start);
            var end   = zones.FirstOrDefault(z => z.Kind == ZoneKind.End);
            _engine.SetZones(start, end);

            if (start is not null) _renderer.Render(start);
            if (end   is not null) _renderer.Render(end);

            Console.WriteLine($"[{Name}] Loaded {zones.Count} zone(s) for {map}.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] OnStartupServer failed: {ex}");
        }
    }

    public override void OnClientDisconnect(ClientDisconnectedEvent args)
    {
        _engine?.Remove(args.Slot);
    }

    public override void OnGameFrame(bool simulating, bool firstTick, bool lastTick)
    {
        if (!simulating || _engine is null || _records is null) return;

        try
        {
            long now = Environment.TickCount64;

            // Players.All does not exist; use Players.GetAll() (Players.cs line 34).
            foreach (var player in Players.GetAll())
            {
                // No IsBot property on CCitadelPlayerController; skip non-connected handled by GetAll().
                // No .Pawn property on controller; use GetHeroPawn() directly (PlayerEntities.cs line 47).
                var pawn = player.GetHeroPawn();
                if (pawn is null) continue;

                // player.Slot does not exist; EntityIndex - 1 gives the slot (Chat.cs line 22).
                int slot = player.EntityIndex - 1;

                var finished = _engine.Tick(slot, pawn.Position, now);
                if (finished is null) continue;

                OnRunFinished(player, finished.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] OnGameFrame failed: {ex}");
        }
    }

    private void OnRunFinished(CCitadelPlayerController player, FinishedRun run)
    {
        if (_records is null) return;

        // CCitadelPlayerController has no SteamId property (PlayerEntities.cs confirms).
        // Use slot (EntityIndex - 1) as the persistent player key, consistent with OnPb.
        long sid = player.EntityIndex - 1;
        var result = _records.UpsertIfFaster(
            steamId: sid,
            map: Server.MapName,
            timeMs: run.ElapsedMs,
            playerName: player.PlayerName,
            nowUnix: DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var formatted = TimeFormatter.FormatTime(run.ElapsedMs);
        string msg;
        if (result.Changed && result.PreviousMs is null)
            msg = $"[LockTimer] {player.PlayerName} finished in {formatted} (new PB!)";
        else if (result.Changed)
            msg = $"[LockTimer] {player.PlayerName} finished in {formatted} " +
                  $"(new PB! prev {TimeFormatter.FormatTime(result.PreviousMs!.Value)})";
        else
            msg = $"[LockTimer] {player.PlayerName} finished in {formatted} " +
                  $"(pb {TimeFormatter.FormatTime(result.PreviousMs!.Value)})";

        // Chat.SayToAll does not exist; use Chat.PrintToChatAll (Chat.cs line 25).
        Chat.PrintToChatAll(msg);
    }

    // --- Chat command wrappers ---
    // The plugin loader scans this class's methods for [ChatCommand] attributes
    // (PluginLoader.ChatCommands.cs line 59). Each wrapper delegates to _commands.

    [ChatCommand("!start1")]
    public HookResult OnStart1(ChatCommandContext ctx)
        => _commands?.OnStart1(ctx) ?? HookResult.Continue;

    [ChatCommand("!start2")]
    public HookResult OnStart2(ChatCommandContext ctx)
        => _commands?.OnStart2(ctx) ?? HookResult.Continue;

    [ChatCommand("!end1")]
    public HookResult OnEnd1(ChatCommandContext ctx)
        => _commands?.OnEnd1(ctx) ?? HookResult.Continue;

    [ChatCommand("!end2")]
    public HookResult OnEnd2(ChatCommandContext ctx)
        => _commands?.OnEnd2(ctx) ?? HookResult.Continue;

    [ChatCommand("!savezones")]
    public HookResult OnSaveZones(ChatCommandContext ctx)
        => _commands?.OnSaveZones(ctx) ?? HookResult.Continue;

    [ChatCommand("!delzones")]
    public HookResult OnDelZones(ChatCommandContext ctx)
        => _commands?.OnDelZones(ctx) ?? HookResult.Continue;

    [ChatCommand("!zones")]
    public HookResult OnZonesStatus(ChatCommandContext ctx)
        => _commands?.OnZonesStatus(ctx) ?? HookResult.Continue;

    [ChatCommand("!pb")]
    public HookResult OnPb(ChatCommandContext ctx)
        => _commands?.OnPb(ctx) ?? HookResult.Continue;

    [ChatCommand("!top")]
    public HookResult OnTop(ChatCommandContext ctx)
        => _commands?.OnTop(ctx) ?? HookResult.Continue;

    [ChatCommand("!reset")]
    public HookResult OnReset(ChatCommandContext ctx)
        => _commands?.OnReset(ctx) ?? HookResult.Continue;
}
