using System;
using System.Runtime.InteropServices;

namespace PokeTibiaBot.Services;

/// <summary>
/// Ponte P/Invoke para a DLL nativa em C++.
/// A DLL fornece: leitura de tela, análise de barras HP/MP, template matching e leitura opcional de memória.
/// </summary>
public static class NativeBridge
{
    private const string Dll = "PokeTibiaBot.Native.dll";

    // ---- Process / Window ----
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr FindGameWindow([MarshalAs(UnmanagedType.LPStr)] string titleContains);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool GetWindowBounds(IntPtr hwnd, out int x, out int y, out int w, out int h);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern bool BringWindowFront(IntPtr hwnd);

    // ---- Screen sampling ----
    /// <summary>
    /// Lê o pixel na coordenada global de tela. Retorna 0xRRGGBB.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern uint ReadPixel(int screenX, int screenY);

    /// <summary>
    /// Retorna a % de preenchimento (0-100) de uma barra horizontal na tela,
    /// checando quantos pixels correspondem à cor esperada (com tolerância).
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ReadBarPercent(int x, int y, int width,
                                            int expectedR, int expectedG, int expectedB,
                                            int tolerance);

    // ---- Template matching (loot / itens) ----
    /// <summary>
    /// Procura o template BMP dentro de um retângulo da tela.
    /// Retorna 1 se encontrou e escreve x,y do centro do match.
    /// </summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern int FindTemplate([MarshalAs(UnmanagedType.LPStr)] string templatePath,
                                          int roiX, int roiY, int roiW, int roiH,
                                          double threshold,
                                          out int foundX, out int foundY);

    // ---- Optional memory reader (para clientes onde você tem offsets) ----
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr OpenProcessByName([MarshalAs(UnmanagedType.LPStr)] string procName);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern int ReadInt32(IntPtr hProc, ulong address);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void CloseProcessHandle(IntPtr hProc);

    // ---- Memory Scanner ----
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr Scanner_Create([MarshalAs(UnmanagedType.LPStr)] string procName);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern void Scanner_Destroy(IntPtr session);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong Scanner_FirstScan(IntPtr session, int value, ulong maxResults);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong Scanner_NextScan(IntPtr session, int value);

    /// <summary>mode: 0=unchanged, 1=changed, 2=increased, 3=decreased</summary>
    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong Scanner_NextCompare(IntPtr session, int mode);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong Scanner_Count(IntPtr session);

    [DllImport(Dll, CallingConvention = CallingConvention.Cdecl)]
    public static extern ulong Scanner_GetResults(IntPtr session, ulong offset, ulong maxOut,
                                                   [Out] ulong[] addrs, [Out] int[] vals);
}
