using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace PokeTibiaBot.Services;

/// <summary>
/// Simulação de input via SendInput (nativo do Windows).
/// Usado para teclas, movimento e cliques.
/// </summary>
public static class InputSimulator
{
    #region Win32
    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint Type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT Mouse;
        [FieldOffset(0)] public KEYBDINPUT Keyboard;
        [FieldOffset(0)] public HARDWAREINPUT Hardware;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int Dx; public int Dy; public uint MouseData;
        public uint Flags; public uint Time; public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort Vk; public ushort Scan; public uint Flags;
        public uint Time; public IntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT { public uint Msg; public ushort ParamL; public ushort ParamH; }

    private const uint INPUT_MOUSE = 0, INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;
    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002, MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008, MOUSEEVENTF_RIGHTUP = 0x0010;
    private const uint MOUSEEVENTF_MOVE = 0x0001, MOUSEEVENTF_ABSOLUTE = 0x8000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern short VkKeyScan(char ch);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int X, int Y);
    #endregion

    /// <summary>
    /// Direções em tibia (setas do teclado).
    /// </summary>
    public static readonly System.Collections.Generic.Dictionary<string, ushort> DirectionKeys = new()
    {
        ["n"]  = 0x26, // VK_UP
        ["s"]  = 0x28, // VK_DOWN
        ["w"]  = 0x25, // VK_LEFT
        ["e"]  = 0x27, // VK_RIGHT
        ["ne"] = 0x21, // VK_PRIOR (PageUp) - diagonal upper-right in Tibia
        ["nw"] = 0x24, // VK_HOME - diagonal upper-left
        ["se"] = 0x22, // VK_NEXT (PageDown) - diagonal lower-right
        ["sw"] = 0x23, // VK_END - diagonal lower-left
    };

    public static void PressKey(ushort vk, int holdMs = 30)
    {
        var down = MakeKey(vk, false);
        var up = MakeKey(vk, true);
        SendInput(1, new[] { down }, Marshal.SizeOf<INPUT>());
        Thread.Sleep(holdMs);
        SendInput(1, new[] { up }, Marshal.SizeOf<INPUT>());
    }

    /// <summary>Pressiona uma tecla nomeada como "F1", "Enter", "1", "a".</summary>
    public static void PressNamed(string name, int holdMs = 30)
    {
        var vk = ResolveVk(name);
        if (vk == 0) return;
        PressKey(vk, holdMs);
    }

    public static void PressDirection(string dir, int holdMs = 30)
    {
        if (DirectionKeys.TryGetValue(dir.ToLowerInvariant(), out var vk))
            PressKey(vk, holdMs);
    }

    public static void TypeText(string text, int perCharDelayMs = 15)
    {
        foreach (var c in text)
        {
            short vkScan = VkKeyScan(c);
            if (vkScan == -1) continue;
            ushort vk = (ushort)(vkScan & 0xFF);
            bool shift = (vkScan & 0x100) != 0;
            if (shift) PressDown(0x10); // VK_SHIFT
            PressKey(vk, 15);
            if (shift) PressUp(0x10);
            // Delay variável entre teclas (parece digitação humana)
            Thread.Sleep(HumanizeService.Enabled ? HumanizeService.KeystrokeDelayMs() : perCharDelayMs);
        }
    }

    public static void SendChatMessage(string text)
    {
        PressKey(0x0D); // Enter (abre chat)
        HumanizeService.HumanSleep(80);
        TypeText(text);
        HumanizeService.HumanSleep(60);
        PressKey(0x0D); // Enter (envia)
    }

    public static void LeftClick(int x, int y)
    {
        HumanizeService.HumanMoveMouse(x, y);
        HumanizeService.HumanSleep(25);
        SendMouse(MOUSEEVENTF_LEFTDOWN); Thread.Sleep(15);
        SendMouse(MOUSEEVENTF_LEFTUP);
    }

    public static void RightClick(int x, int y)
    {
        HumanizeService.HumanMoveMouse(x, y);
        HumanizeService.HumanSleep(25);
        SendMouse(MOUSEEVENTF_RIGHTDOWN); Thread.Sleep(15);
        SendMouse(MOUSEEVENTF_RIGHTUP);
    }

    #region Helpers
    private static INPUT MakeKey(ushort vk, bool up)
    {
        return new INPUT
        {
            Type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                Keyboard = new KEYBDINPUT { Vk = vk, Flags = up ? KEYEVENTF_KEYUP : 0 }
            }
        };
    }

    private static void PressDown(ushort vk) =>
        SendInput(1, new[] { MakeKey(vk, false) }, Marshal.SizeOf<INPUT>());
    private static void PressUp(ushort vk) =>
        SendInput(1, new[] { MakeKey(vk, true) }, Marshal.SizeOf<INPUT>());

    private static void SendMouse(uint flags)
    {
        var inp = new INPUT
        {
            Type = INPUT_MOUSE,
            U = new InputUnion { Mouse = new MOUSEINPUT { Flags = flags } }
        };
        SendInput(1, new[] { inp }, Marshal.SizeOf<INPUT>());
    }

    private static ushort ResolveVk(string name)
    {
        name = name.Trim().ToUpperInvariant();
        if (name.StartsWith("F") && int.TryParse(name.AsSpan(1), out var fn) && fn >= 1 && fn <= 24)
            return (ushort)(0x70 + (fn - 1)); // VK_F1 = 0x70
        return name switch
        {
            "ENTER" => 0x0D,
            "SPACE" => 0x20,
            "ESC" or "ESCAPE" => 0x1B,
            "TAB" => 0x09,
            "SHIFT" => 0x10,
            "CTRL" or "CONTROL" => 0x11,
            "ALT" => 0x12,
            _ when name.Length == 1 => (ushort)(VkKeyScan(name[0]) & 0xFF),
            _ => 0
        };
    }
    #endregion
}
