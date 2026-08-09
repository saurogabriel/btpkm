using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using PokeTibiaBot.Services;

namespace PokeTibiaBot.Views;

public partial class MemoryScannerWindow : Window
{
    public class ScanResult
    {
        public int Index { get; set; }
        public ulong Address { get; set; }
        public int Value { get; set; }
        public string HexAddress => $"0x{Address:X}";
    }

    public class ProcItem
    {
        public uint Pid { get; set; }
        public string Name { get; set; } = "";
        public string Display => $"{Name}  (pid {Pid})";
        public override string ToString() => Display; // usado quando ComboBox editável
    }

    private IntPtr _session = IntPtr.Zero;
    private readonly ObservableCollection<ScanResult> _results = new();
    private readonly ObservableCollection<ProcItem> _procs = new();
    private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromMilliseconds(700) };
    private bool _busy;

    public MemoryScannerWindow()
    {
        InitializeComponent();
        ResultsGrid.ItemsSource = _results;
        ProcessCombo.ItemsSource = _procs;
        _refresh.Tick += (s, e) => RefreshVisibleValues();
        _refresh.Start();
        Closed += (s, e) => { _refresh.Stop(); Detach(); };
        Loaded += (s, e) => InitialCheck();
    }

    /// <summary>
    /// Checa se a DLL nativa existe e pode ser carregada. Se não, mostra erro
    /// amigável em vez de deixar a app crashar no primeiro P/Invoke.
    /// </summary>
    private void InitialCheck()
    {
        string dllPath = Path.Combine(AppContext.BaseDirectory, "PokeTibiaBot.Native.dll");
        if (!File.Exists(dllPath))
        {
            ShowFatalMsg($"DLL nativa não encontrada:\n{dllPath}\n\n" +
                         "Compile o projeto PokeTibiaBot.Native em Release|x64 e garanta que a DLL fique junto do .exe.");
            return;
        }
        // Tenta carregar (uma chamada simples) protegido
        try { NativeBridge.ListProcesses(new byte[260], new uint[1], 1); }
        catch (DllNotFoundException) { ShowFatalMsg("Falha ao carregar PokeTibiaBot.Native.dll — provavelmente arquitetura errada (deve ser x64)."); return; }
        catch (BadImageFormatException) { ShowFatalMsg("PokeTibiaBot.Native.dll tem plataforma incompatível. Compile em x64."); return; }
        catch (Exception ex) { ShowFatalMsg($"Erro ao inicializar DLL nativa:\n{ex.Message}"); return; }

        RefreshProcs();
    }

    private void ShowFatalMsg(string msg)
    {
        StatusTxt.Text = "ERRO: " + msg.Split('\n')[0];
        StatusTxt.Foreground = (Brush)FindResource("AccentRed");
        FirstScanBtn.IsEnabled = false;
        NextScanBtn.IsEnabled = false;
        MessageBox.Show(this, msg, "Memory Scanner", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void OnRefreshProcs(object s, RoutedEventArgs e) => RefreshProcs();

    private void RefreshProcs()
    {
        try
        {
            const int Max = 512;
            var buf = new byte[Max * 260];
            var pids = new uint[Max];
            uint got;
            try { got = NativeBridge.ListProcesses(buf, pids, Max); }
            catch (Exception ex) { StatusTxt.Text = "Erro ao listar processos: " + ex.Message; return; }

            _procs.Clear();
            for (int i = 0; i < got; i++)
            {
                // Cada nome tem exatamente 260 bytes, ANSI, terminado em NUL.
                int start = i * 260;
                int len = 0; while (len < 260 && buf[start + len] != 0) len++;
                var name = Encoding.Default.GetString(buf, start, len);
                if (!string.IsNullOrWhiteSpace(name))
                    _procs.Add(new ProcItem { Pid = pids[i], Name = name });
            }
        }
        catch (Exception ex)
        {
            StatusTxt.Text = "Erro: " + ex.Message;
        }
    }

    private void OnAttach(object s, RoutedEventArgs e)
    {
        Detach();
        string procName = ResolveSelectedProcName();
        if (string.IsNullOrWhiteSpace(procName))
        {
            SetStatus("Escolha um processo da lista (ou digite o nome, ex: PokeTibia.exe).", false);
            return;
        }

        // Envolve o P/Invoke inteiro num try/catch reforçado
        try
        {
            _session = NativeBridge.Scanner_Create(procName);
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao anexar (exceção nativa): {ex.GetType().Name} — {ex.Message}", false);
            _session = IntPtr.Zero;
            return;
        }

        if (_session == IntPtr.Zero)
        {
            SetStatus($"Não consegui abrir '{procName}'. Rode o bot como Administrador ou confira o nome (com .exe).", false);
            return;
        }
        SetStatus($"anexado a {procName}", true);
    }

    private string ResolveSelectedProcName()
    {
        if (ProcessCombo.SelectedItem is ProcItem pi) return pi.Name;
        var typed = (ProcessCombo.Text ?? "").Trim();
        // Se o usuário digitou "PokeTibia" sem .exe, adiciona
        if (!string.IsNullOrEmpty(typed) && !typed.Contains('.')) typed += ".exe";
        return typed;
    }

    private void OnDetach(object s, RoutedEventArgs e) => Detach();

    private void Detach()
    {
        if (_session != IntPtr.Zero)
        {
            try { NativeBridge.Scanner_Destroy(_session); } catch { /* engolir para não crashar no close */ }
            _session = IntPtr.Zero;
        }
        _results.Clear();
        CountTxt.Text = "0 resultados";
        SetStatus("não anexado", false);
    }

    private void SetStatus(string text, bool ok)
    {
        StatusTxt.Text = text;
        StatusTxt.Foreground = (Brush)FindResource(ok ? "AccentGreen" : "AccentRed");
    }

    private bool RequireSession()
    {
        if (_session == IntPtr.Zero)
        {
            MessageBox.Show(this, "Anexe primeiro a um processo.", "Scanner");
            return false;
        }
        if (_busy)
        {
            MessageBox.Show(this, "Um scan já está em andamento, aguarde.", "Scanner");
            return false;
        }
        return true;
    }

    // ==== Scans (executados em background para não travar a UI) ====

    private async void OnFirstScan(object s, RoutedEventArgs e)
    {
        if (!RequireSession()) return;
        if (!int.TryParse(ValueTb.Text, out var v)) return;
        if (!ulong.TryParse(MaxTb.Text, out var max)) max = 500_000UL;
        var session = _session;
        await RunScan(() => NativeBridge.Scanner_FirstScan(session, v, max));
    }

    private async void OnNextEqual(object s, RoutedEventArgs e)
    {
        if (!RequireSession()) return;
        if (!int.TryParse(ValueTb.Text, out var v)) return;
        var session = _session;
        await RunScan(() => NativeBridge.Scanner_NextScan(session, v));
    }

    private async void OnCmpUnchanged(object s, RoutedEventArgs e) => await NextCompare(0);
    private async void OnCmpChanged(object s, RoutedEventArgs e) => await NextCompare(1);
    private async void OnCmpInc(object s, RoutedEventArgs e) => await NextCompare(2);
    private async void OnCmpDec(object s, RoutedEventArgs e) => await NextCompare(3);

    private async Task NextCompare(int mode)
    {
        if (!RequireSession()) return;
        var session = _session;
        await RunScan(() => NativeBridge.Scanner_NextCompare(session, mode));
    }

    private async Task RunScan(Func<ulong> op)
    {
        _busy = true;
        Busy.Visibility = Visibility.Visible;
        Busy.IsIndeterminate = true;
        FirstScanBtn.IsEnabled = false;
        NextScanBtn.IsEnabled = false;

        ulong count = 0; string? error = null;
        try { count = await Task.Run(op); }
        catch (Exception ex) { error = $"{ex.GetType().Name}: {ex.Message}"; }
        finally
        {
            Busy.IsIndeterminate = false;
            Busy.Visibility = Visibility.Collapsed;
            FirstScanBtn.IsEnabled = true;
            NextScanBtn.IsEnabled = true;
            _busy = false;
        }

        if (error != null) { MessageBox.Show(this, "Scan falhou: " + error, "Scanner"); return; }
        CountTxt.Text = $"{count:N0} resultados";
        LoadPreview();
    }

    private void LoadPreview()
    {
        _results.Clear();
        if (_session == IntPtr.Zero) return;
        const int Max = 200;
        var addrs = new ulong[Max];
        var vals = new int[Max];
        ulong got;
        try { got = NativeBridge.Scanner_GetResults(_session, 0, (ulong)Max, addrs, vals); }
        catch { return; }
        for (int i = 0; i < (int)got; i++)
            _results.Add(new ScanResult { Index = i + 1, Address = addrs[i], Value = vals[i] });
    }

    private void RefreshVisibleValues()
    {
        if (_session == IntPtr.Zero || _results.Count == 0 || _busy) return;
        int n = Math.Min(_results.Count, 200);
        var addrs = new ulong[n];
        var vals = new int[n];
        try { NativeBridge.Scanner_GetResults(_session, 0, (ulong)n, addrs, vals); }
        catch { return; }
        for (int i = 0; i < n; i++)
            if (_results[i].Address == addrs[i]) _results[i].Value = vals[i];
        ResultsGrid.Items.Refresh();
    }
}
