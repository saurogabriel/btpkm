using System;
using System.Collections.ObjectModel;
using System.Windows;
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

    private IntPtr _session = IntPtr.Zero;
    private readonly ObservableCollection<ScanResult> _results = new();
    private readonly DispatcherTimer _refresh = new() { Interval = TimeSpan.FromMilliseconds(700) };

    public MemoryScannerWindow()
    {
        InitializeComponent();
        ResultsGrid.ItemsSource = _results;
        _refresh.Tick += (s, e) => RefreshVisibleValues();
        _refresh.Start();
        Closed += (s, e) => Detach();
    }

    private void OnAttach(object s, RoutedEventArgs e)
    {
        Detach();
        _session = NativeBridge.Scanner_Create(ProcessNameTb.Text.Trim());
        if (_session == IntPtr.Zero)
        {
            StatusTxt.Text = "FALHOU (processo não encontrado ou sem permissão — rode como Admin)";
            StatusTxt.Foreground = (System.Windows.Media.Brush)FindResource("AccentRed");
        }
        else
        {
            StatusTxt.Text = "anexado";
            StatusTxt.Foreground = (System.Windows.Media.Brush)FindResource("AccentGreen");
        }
    }

    private void OnDetach(object s, RoutedEventArgs e) => Detach();

    private void Detach()
    {
        if (_session != IntPtr.Zero)
        {
            NativeBridge.Scanner_Destroy(_session);
            _session = IntPtr.Zero;
        }
        _results.Clear();
        StatusTxt.Text = "não anexado";
        StatusTxt.Foreground = (System.Windows.Media.Brush)FindResource("AccentRed");
        CountTxt.Text = "0 resultados";
    }

    private bool RequireSession()
    {
        if (_session == IntPtr.Zero)
        {
            MessageBox.Show(this, "Anexe primeiro a um processo.", "Scanner");
            return false;
        }
        return true;
    }

    private void OnFirstScan(object s, RoutedEventArgs e)
    {
        if (!RequireSession()) return;
        if (!int.TryParse(ValueTb.Text, out var v)) return;
        if (!ulong.TryParse(MaxTb.Text, out var max)) max = 500_000UL;
        var count = NativeBridge.Scanner_FirstScan(_session, v, max);
        UpdateCount(count);
        LoadPreview();
    }

    private void OnNextEqual(object s, RoutedEventArgs e)
    {
        if (!RequireSession()) return;
        if (!int.TryParse(ValueTb.Text, out var v)) return;
        var count = NativeBridge.Scanner_NextScan(_session, v);
        UpdateCount(count);
        LoadPreview();
    }

    private void OnCmpUnchanged(object s, RoutedEventArgs e) => NextCompare(0);
    private void OnCmpChanged(object s, RoutedEventArgs e) => NextCompare(1);
    private void OnCmpInc(object s, RoutedEventArgs e) => NextCompare(2);
    private void OnCmpDec(object s, RoutedEventArgs e) => NextCompare(3);

    private void NextCompare(int mode)
    {
        if (!RequireSession()) return;
        var count = NativeBridge.Scanner_NextCompare(_session, mode);
        UpdateCount(count);
        LoadPreview();
    }

    private void UpdateCount(ulong count) =>
        CountTxt.Text = $"{count:N0} resultados";

    /// <summary>Carrega até 200 resultados no grid.</summary>
    private void LoadPreview()
    {
        _results.Clear();
        const int Max = 200;
        var addrs = new ulong[Max];
        var vals = new int[Max];
        var got = NativeBridge.Scanner_GetResults(_session, 0, (ulong)Max, addrs, vals);
        for (int i = 0; i < (int)got; i++)
            _results.Add(new ScanResult { Index = i + 1, Address = addrs[i], Value = vals[i] });
    }

    /// <summary>Atualiza valores atuais dos resultados visíveis para o usuário ver mudanças em tempo real.</summary>
    private void RefreshVisibleValues()
    {
        if (_session == IntPtr.Zero || _results.Count == 0) return;
        int n = Math.Min(_results.Count, 200);
        var addrs = new ulong[n];
        var vals = new int[n];
        NativeBridge.Scanner_GetResults(_session, 0, (ulong)n, addrs, vals);
        for (int i = 0; i < n; i++)
            if (_results[i].Address == addrs[i]) _results[i].Value = vals[i];
        ResultsGrid.Items.Refresh();
    }
}
