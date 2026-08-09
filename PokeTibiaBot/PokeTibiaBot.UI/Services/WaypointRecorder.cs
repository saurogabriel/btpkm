using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using PokeTibiaBot.Models;

namespace PokeTibiaBot.Services;

/// <summary>
/// Gravador de waypoints (record & play). Captura teclas de direção pressionadas pelo usuário
/// via low-level keyboard hook e adiciona à lista de waypoints em tempo real.
/// </summary>
public class WaypointRecorder : IDisposable
{
    #region Win32 hook
    private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint threadId);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    [DllImport("user32.dll")] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("kernel32.dll")] private static extern IntPtr GetModuleHandle(string? name);

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    #endregion

    private IntPtr _hookId = IntPtr.Zero;
    private HookProc? _proc;
    private DateTime _lastKey = DateTime.UtcNow;

    public bool IsRecording { get; private set; }
    public event Action<Waypoint>? WaypointCaptured;

    private static readonly Dictionary<int, string> KeyToDir = new()
    {
        { 0x26, "n" }, { 0x28, "s" }, { 0x25, "w" }, { 0x27, "e" },
        { 0x21, "ne" }, { 0x24, "nw" }, { 0x22, "se" }, { 0x23, "sw" }
    };

    public void Start()
    {
        if (IsRecording) return;
        _proc = HookCallback;
        _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(null), 0);
        IsRecording = true;
    }

    public void Stop()
    {
        if (!IsRecording) return;
        UnhookWindowsHookEx(_hookId);
        _hookId = IntPtr.Zero;
        IsRecording = false;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            int vk = Marshal.ReadInt32(lParam);
            if (KeyToDir.TryGetValue(vk, out var dir))
            {
                // Debounce: no mínimo 100ms entre teclas
                if ((DateTime.UtcNow - _lastKey).TotalMilliseconds > 100)
                {
                    _lastKey = DateTime.UtcNow;
                    var wp = new Waypoint
                    {
                        Action = WaypointAction.Walk,
                        Direction = dir,
                        Name = $"Walk {dir.ToUpper()}"
                    };
                    WaypointCaptured?.Invoke(wp);
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose() => Stop();
}
