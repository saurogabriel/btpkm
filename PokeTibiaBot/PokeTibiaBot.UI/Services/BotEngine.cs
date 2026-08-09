using System;
using System.Threading;
using System.Threading.Tasks;
using PokeTibiaBot.Models;

namespace PokeTibiaBot.Services;

/// <summary>
/// Núcleo do bot. Roda em background e executa: healing, waypoints, loot.
/// Máquina de estados leve.
/// </summary>
public class BotEngine
{
    public enum BotState { Idle, Running, Paused, Stopping }

    private readonly BotProfile _profile;
    private CancellationTokenSource? _cts;
    private Task? _mainTask;
    private IntPtr _hwnd = IntPtr.Zero;

    private readonly System.Collections.Generic.Dictionary<string, DateTime> _cooldowns = new();

    public BotState State { get; private set; } = BotState.Idle;
    public event Action<string>? LogMessage;
    public event Action<BotState>? StateChanged;
    public event Action<int, int>? StatsUpdated; // HP%, MP%
    public event Action<string>? WaypointChanged; // nome do WP atual

    public int CurrentWaypointIndex { get; private set; }

    public BotEngine(BotProfile profile) { _profile = profile; }

    public void Start()
    {
        if (State == BotState.Running) return;
        _cts = new CancellationTokenSource();
        _hwnd = NativeBridge.FindGameWindow(_profile.GameWindowTitle);
        if (_hwnd == IntPtr.Zero)
            Log($"AVISO: janela '{_profile.GameWindowTitle}' não encontrada. Bot rodando mesmo assim (input global).");
        else
            NativeBridge.BringWindowFront(_hwnd);

        // Aplica configurações de humanização
        HumanizeService.Enabled = _profile.HumanizeEnabled;
        HumanizeService.JitterPercent = _profile.JitterPercent;
        HumanizeService.MicroPauseMinMs = _profile.MicroPauseMinMs;
        HumanizeService.MicroPauseMaxMs = _profile.MicroPauseMaxMs;
        HumanizeService.DistractionChance = _profile.DistractionChance;
        HumanizeService.DistractionMinMs = _profile.DistractionMinMs;
        HumanizeService.DistractionMaxMs = _profile.DistractionMaxMs;

        State = BotState.Running;
        StateChanged?.Invoke(State);
        _mainTask = Task.Run(() => MainLoop(_cts.Token));
        Log($"Bot iniciado. Humanize={_profile.HumanizeEnabled} (jitter ±{_profile.JitterPercent}%).");
    }

    public void Pause()
    {
        if (State != BotState.Running) return;
        State = BotState.Paused;
        StateChanged?.Invoke(State);
        Log("Bot pausado.");
    }

    public void Resume()
    {
        if (State != BotState.Paused) return;
        State = BotState.Running;
        StateChanged?.Invoke(State);
        Log("Bot retomado.");
    }

    public void Stop()
    {
        if (State == BotState.Idle) return;
        State = BotState.Stopping;
        StateChanged?.Invoke(State);
        _cts?.Cancel();
        try { _mainTask?.Wait(2000); } catch { }
        State = BotState.Idle;
        StateChanged?.Invoke(State);
        Log("Bot parado.");
    }

    private void MainLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (State == BotState.Paused) { Thread.Sleep(200); continue; }

                // 1. HEALING (prioridade máxima)
                TickHealing();

                // 2. HOTKEYS (attacks recorrentes, etc)
                TickHotkeys();

                // 3. WAYPOINT ATUAL
                TickWaypoints(ct);

                Thread.Sleep(80);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Log($"ERRO no loop: {ex.Message}"); }
    }

    private void TickHealing()
    {
        if (_profile.HpBarWidth <= 0) return;

        int hpPct = NativeBridge.ReadBarPercent(
            _profile.HpBarX, _profile.HpBarY, _profile.HpBarWidth,
            _profile.HpBarColorR, _profile.HpBarColorG, _profile.HpBarColorB, 40);
        int mpPct = _profile.MpBarWidth > 0
            ? NativeBridge.ReadBarPercent(
                _profile.MpBarX, _profile.MpBarY, _profile.MpBarWidth,
                _profile.MpBarColorR, _profile.MpBarColorG, _profile.MpBarColorB, 40)
            : 100;

        StatsUpdated?.Invoke(hpPct, mpPct);

        foreach (var rule in _profile.HealingRules)
        {
            if (!rule.Enabled) continue;
            bool triggerHp = hpPct <= rule.HpThresholdPercent;
            bool triggerMp = rule.MpThresholdPercent > 0 && mpPct <= rule.MpThresholdPercent;
            if (!(triggerHp || triggerMp)) continue;
            if (!CooldownReady(rule.Name, rule.CooldownMs)) continue;

            InputSimulator.PressNamed(rule.Hotkey);
            SetCooldown(rule.Name);
            Log($"Heal '{rule.Name}' -> tecla {rule.Hotkey} (HP {hpPct}% / MP {mpPct}%).");
        }
    }

    private void TickHotkeys()
    {
        foreach (var hk in _profile.Hotkeys)
        {
            if (!hk.Enabled) continue;
            if (!CooldownReady($"hk_{hk.Name}", hk.CooldownMs)) continue;

            if (string.IsNullOrEmpty(hk.Command) || hk.Command == "attack")
                InputSimulator.PressNamed(hk.Key);
            else
                InputSimulator.SendChatMessage(hk.Command);

            SetCooldown($"hk_{hk.Name}");
        }
    }

    private void TickWaypoints(CancellationToken ct)
    {
        if (_profile.Waypoints.Count == 0) { Thread.Sleep(200); return; }
        if (CurrentWaypointIndex >= _profile.Waypoints.Count)
        {
            if (_profile.LoopWaypoints) CurrentWaypointIndex = 0;
            else { Thread.Sleep(300); return; }
        }

        var wp = _profile.Waypoints[CurrentWaypointIndex];
        WaypointChanged?.Invoke($"#{CurrentWaypointIndex + 1} {wp.Name} ({wp.Action})");
        ExecuteWaypoint(wp, ct);
        CurrentWaypointIndex++;
    }

    private void ExecuteWaypoint(Waypoint wp, CancellationToken ct)
    {
        for (int r = 0; r < Math.Max(1, wp.Repeat) && !ct.IsCancellationRequested; r++)
        {
            switch (wp.Action)
            {
                case WaypointAction.Walk:
                    InputSimulator.PressDirection(wp.Direction);
                    HumanizeService.HumanSleep(_profile.WalkDelayMs);
                    break;
                case WaypointAction.Wait:
                    Sleep(HumanizeService.JitterMs(wp.WaitMs), ct);
                    break;
                case WaypointAction.UseItem:
                case WaypointAction.Heal:
                    InputSimulator.PressNamed(wp.Hotkey);
                    HumanizeService.HumanSleep(_profile.ActionDelayMs);
                    break;
                case WaypointAction.TalkNpc:
                    foreach (var msg in wp.NpcMessages)
                    {
                        if (ct.IsCancellationRequested) break;
                        InputSimulator.SendChatMessage(msg);
                        HumanizeService.HumanSleep(500);
                    }
                    break;
                case WaypointAction.Command:
                    if (!string.IsNullOrEmpty(wp.ChatText))
                        InputSimulator.SendChatMessage(wp.ChatText);
                    HumanizeService.HumanSleep(_profile.ActionDelayMs);
                    break;
                case WaypointAction.Ladder:
                    InputSimulator.RightClick(wp.ScreenX, wp.ScreenY);
                    HumanizeService.HumanSleep(_profile.ActionDelayMs);
                    break;
                case WaypointAction.Rope:
                    InputSimulator.PressNamed(wp.Hotkey);
                    HumanizeService.HumanSleep(120);
                    InputSimulator.LeftClick(wp.ScreenX, wp.ScreenY);
                    HumanizeService.HumanSleep(_profile.ActionDelayMs);
                    break;
                case WaypointAction.Depot:
                    InputSimulator.SendChatMessage("hi");
                    HumanizeService.HumanSleep(400);
                    InputSimulator.SendChatMessage("deposit all");
                    HumanizeService.HumanSleep(400);
                    InputSimulator.SendChatMessage("yes");
                    HumanizeService.HumanSleep(500);
                    break;
                case WaypointAction.Custom:
                    if (!string.IsNullOrEmpty(wp.ChatText))
                        InputSimulator.SendChatMessage(wp.ChatText);
                    HumanizeService.HumanSleep(_profile.ActionDelayMs);
                    break;
            }

            if (_profile.Loot.Enabled && (wp.Action == WaypointAction.Walk || wp.Action == WaypointAction.Wait))
                TryLootNearby();
        }
    }

    private void TryLootNearby()
    {
        // Estratégia genérica: se LootAll estiver ativo, apenas envia comando padrão OTServ
        if (_profile.Loot.LootAll)
        {
            InputSimulator.PressNamed("F12"); // convenção: F12 = pick up loot macro do cliente
        }
        // Para loot por template, o BotEngine pode ser estendido invocando NativeBridge.FindTemplate
    }

    private bool CooldownReady(string key, int ms)
    {
        if (_cooldowns.TryGetValue(key, out var last))
            if ((DateTime.UtcNow - last).TotalMilliseconds < ms) return false;
        return true;
    }
    private void SetCooldown(string key) => _cooldowns[key] = DateTime.UtcNow;

    private static void Sleep(int ms, CancellationToken ct)
    {
        var end = DateTime.UtcNow.AddMilliseconds(ms);
        while (DateTime.UtcNow < end && !ct.IsCancellationRequested)
            Thread.Sleep(50);
    }

    private void Log(string m) => LogMessage?.Invoke($"[{DateTime.Now:HH:mm:ss}] {m}");
}
