using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace PokeTibiaBot.Views;

/// <summary>
/// Overlay transparente sempre-no-topo, click-through (WS_EX_TRANSPARENT).
/// Mostra HP/MP e o waypoint atual.
/// </summary>
public partial class OverlayWindow : Window
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int newStyle);
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x80;
    private const int WS_EX_LAYERED = 0x80000;

    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED);
        };
    }

    public void UpdateStats(int hp, int mp)
    {
        Dispatcher.Invoke(() =>
        {
            HpBar.Width = Math.Max(0, Math.Min(220, hp * 2.2));
            MpBar.Width = Math.Max(0, Math.Min(220, mp * 2.2));
            HpText.Text = $"{hp}%";
            MpText.Text = $"{mp}%";
        });
    }

    public void UpdateWaypoint(string text) =>
        Dispatcher.Invoke(() => WpText.Text = text);

    public void UpdateState(string s) =>
        Dispatcher.Invoke(() => StateText.Text = s);
}
