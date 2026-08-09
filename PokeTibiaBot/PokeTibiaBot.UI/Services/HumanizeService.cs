using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PokeTibiaBot.Services;

/// <summary>
/// Camada anti-detecção. Adiciona jitter (variação aleatória) em todos os timings
/// e move o mouse por curvas de Bézier em pequenos passos, imitando movimento humano.
/// </summary>
public static class HumanizeService
{
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int X, int Y);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out POINT p);
    [StructLayout(LayoutKind.Sequential)] private struct POINT { public int X, Y; }

    // Um único Random com lock para thread-safety.
    private static readonly Random Rng = new();
    private static readonly object RngLock = new();

    public static bool Enabled { get; set; } = true;
    /// <summary>Jitter máximo aplicado a delays, em %. 25 = ±25%.</summary>
    public static int JitterPercent { get; set; } = 25;
    /// <summary>Delay extra aleatório após cada ação (ms).</summary>
    public static int MicroPauseMinMs { get; set; } = 15;
    public static int MicroPauseMaxMs { get; set; } = 65;
    /// <summary>Chance (0..100) de fazer uma micro-pausa "distração" mais longa.</summary>
    public static int DistractionChance { get; set; } = 3;
    public static int DistractionMinMs { get; set; } = 400;
    public static int DistractionMaxMs { get; set; } = 1200;

    /// <summary>Aplica jitter a um valor de delay e faz o Thread.Sleep.</summary>
    public static void HumanSleep(int baseMs)
    {
        if (!Enabled) { Thread.Sleep(baseMs); return; }
        int jittered = JitterMs(baseMs);
        Thread.Sleep(Math.Max(1, jittered));
        MicroPause();
    }

    /// <summary>Devolve o valor com jitter aplicado (± JitterPercent).</summary>
    public static int JitterMs(int baseMs)
    {
        if (!Enabled || baseMs <= 0) return baseMs;
        double factor;
        lock (RngLock) factor = 1.0 + (Rng.NextDouble() * 2 - 1) * (JitterPercent / 100.0);
        return (int)Math.Round(baseMs * factor);
    }

    /// <summary>Pausa aleatória curta simulando reação humana.</summary>
    public static void MicroPause()
    {
        if (!Enabled) return;
        int ms;
        lock (RngLock)
        {
            if (Rng.Next(0, 100) < DistractionChance)
                ms = Rng.Next(DistractionMinMs, DistractionMaxMs);
            else
                ms = Rng.Next(MicroPauseMinMs, MicroPauseMaxMs);
        }
        Thread.Sleep(ms);
    }

    /// <summary>
    /// Move o cursor de (curX,curY) até (tx,ty) seguindo curva de Bézier quadrática
    /// com pequenas oscilações, em passos discretos. Total ~120-260ms.
    /// </summary>
    public static void HumanMoveMouse(int targetX, int targetY)
    {
        if (!GetCursorPos(out var start))
        {
            SetCursorPos(targetX, targetY);
            return;
        }
        if (!Enabled)
        {
            SetCursorPos(targetX, targetY);
            return;
        }

        double sx = start.X, sy = start.Y, ex = targetX, ey = targetY;
        double mx, my, dur, wobble;
        int steps;
        lock (RngLock)
        {
            // Ponto de controle deslocado perpendicularmente ~ 8-25% da distância
            double dx = ex - sx, dy = ey - sy;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double perpX = -dy, perpY = dx;
            double len = Math.Max(1, Math.Sqrt(perpX * perpX + perpY * perpY));
            perpX /= len; perpY /= len;
            double curve = dist * (0.08 + Rng.NextDouble() * 0.17) * (Rng.Next(0, 2) == 0 ? 1 : -1);
            mx = (sx + ex) / 2 + perpX * curve;
            my = (sy + ey) / 2 + perpY * curve;

            steps = Math.Clamp((int)(dist / 8) + Rng.Next(10, 25), 12, 60);
            dur = 120 + Rng.Next(0, 140);
            wobble = 1.2;
        }

        double perStep = dur / steps;
        for (int i = 1; i <= steps; i++)
        {
            double t = (double)i / steps;
            // Bézier quadrática
            double x = (1 - t) * (1 - t) * sx + 2 * (1 - t) * t * mx + t * t * ex;
            double y = (1 - t) * (1 - t) * sy + 2 * (1 - t) * t * my + t * t * ey;
            double wx, wy;
            lock (RngLock)
            {
                wx = (Rng.NextDouble() * 2 - 1) * wobble;
                wy = (Rng.NextDouble() * 2 - 1) * wobble;
            }
            SetCursorPos((int)Math.Round(x + wx), (int)Math.Round(y + wy));
            Thread.Sleep((int)Math.Max(3, perStep));
        }
        SetCursorPos(targetX, targetY);
    }

    /// <summary>Delay realista entre keystrokes ao digitar.</summary>
    public static int KeystrokeDelayMs()
    {
        if (!Enabled) return 15;
        lock (RngLock) return Rng.Next(55, 140);
    }
}
