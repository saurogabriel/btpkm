using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using PokeTibiaBot.Models;

namespace PokeTibiaBot.Services;

/// <summary>
/// Registra hotkeys globais (Win32 RegisterHotKey). Ativa/desativa perfil, pausa, etc.
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;
    private HwndSource? _source;
    private readonly Dictionary<int, Action> _actions = new();
    private int _nextId = 9000;

    public void Attach(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _source = HwndSource.FromHwnd(helper.EnsureHandle());
        _source.AddHook(HwndHook);
    }

    public bool Register(uint modifiers, uint vk, Action handler)
    {
        if (_source == null) return false;
        var id = _nextId++;
        if (!RegisterHotKey(_source.Handle, id, modifiers, vk)) return false;
        _actions[id] = handler;
        return true;
    }

    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && _actions.TryGetValue(wParam.ToInt32(), out var act))
        {
            act();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source == null) return;
        foreach (var id in _actions.Keys) UnregisterHotKey(_source.Handle, id);
        _actions.Clear();
        _source.RemoveHook(HwndHook);
    }
}
