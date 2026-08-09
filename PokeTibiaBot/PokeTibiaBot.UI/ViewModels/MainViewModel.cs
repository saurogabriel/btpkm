using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PokeTibiaBot.Models;
using PokeTibiaBot.Services;
using PokeTibiaBot.Views;

namespace PokeTibiaBot.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private BotProfile profile;
    [ObservableProperty] private Waypoint? selectedWaypoint;
    [ObservableProperty] private string status = "Idle";
    [ObservableProperty] private int hpPercent = 100;
    [ObservableProperty] private int mpPercent = 100;
    [ObservableProperty] private bool isRunning;
    [ObservableProperty] private bool isRecording;

    public ObservableCollection<string> Log { get; } = new();
    public ObservableCollection<Waypoint> Waypoints { get; } = new();
    public ObservableCollection<HealingRule> HealingRules { get; } = new();
    public ObservableCollection<HotkeyBinding> Hotkeys { get; } = new();

    private BotEngine? _engine;
    private readonly WaypointRecorder _recorder = new();

    public MainViewModel()
    {
        ProfileService.EnsureFolder();
        profile = ProfileService.CreateDefault();
        RefreshFromProfile();

        _recorder.WaypointCaptured += wp =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                wp.Id = Waypoints.Count + 1;
                Waypoints.Add(wp);
                Profile.Waypoints.Add(wp);
            });
        };
    }

    private void RefreshFromProfile()
    {
        Waypoints.Clear();
        foreach (var wp in Profile.Waypoints) Waypoints.Add(wp);
        HealingRules.Clear();
        foreach (var h in Profile.HealingRules) HealingRules.Add(h);
        Hotkeys.Clear();
        foreach (var k in Profile.Hotkeys) Hotkeys.Add(k);
    }

    [RelayCommand]
    private void StartBot()
    {
        if (_engine != null && IsRunning) return;
        _engine = new BotEngine(Profile);
        _engine.LogMessage += m => Application.Current.Dispatcher.Invoke(() =>
        {
            Log.Insert(0, m);
            if (Log.Count > 500) Log.RemoveAt(Log.Count - 1);
        });
        _engine.StateChanged += s => Application.Current.Dispatcher.Invoke(() =>
        {
            Status = s.ToString();
            IsRunning = s == BotEngine.BotState.Running;
        });
        _engine.StatsUpdated += (hp, mp) => Application.Current.Dispatcher.Invoke(() =>
        {
            HpPercent = hp; MpPercent = mp;
            _overlay?.UpdateStats(hp, mp);
        });
        _engine.WaypointChanged += wp => Application.Current.Dispatcher.Invoke(() =>
            _overlay?.UpdateWaypoint(wp));
        _engine.StateChanged += s => Application.Current.Dispatcher.Invoke(() =>
            _overlay?.UpdateState(s.ToString()));
        _engine.Start();
    }

    [RelayCommand] private void PauseBot() => _engine?.Pause();
    [RelayCommand] private void ResumeBot() => _engine?.Resume();
    [RelayCommand] private void StopBot() => _engine?.Stop();

    [RelayCommand]
    private void ToggleOverlay()
    {
        if (_overlay == null || !_overlay.IsVisible)
        {
            _overlay = new OverlayWindow();
            _overlay.Show();
            _overlay.UpdateStats(HpPercent, MpPercent);
            _overlay.UpdateState(Status);
        }
        else
        {
            _overlay.Close();
            _overlay = null;
        }
    }

    [RelayCommand]
    private void OpenScanner()
    {
        if (_scannerWin != null && _scannerWin.IsVisible) { _scannerWin.Activate(); return; }
        _scannerWin = new MemoryScannerWindow { Owner = Application.Current.MainWindow };
        _scannerWin.Show();
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        if (_recorder.IsRecording) { _recorder.Stop(); IsRecording = false; }
        else { _recorder.Start(); IsRecording = true; }
    }

    [RelayCommand]
    private void AddWaypoint()
    {
        var wp = new Waypoint { Id = Waypoints.Count + 1, Name = "New WP", Action = WaypointAction.Walk };
        Waypoints.Add(wp);
        Profile.Waypoints.Add(wp);
        SelectedWaypoint = wp;
    }

    [RelayCommand]
    private void RemoveWaypoint()
    {
        if (SelectedWaypoint == null) return;
        Profile.Waypoints.Remove(SelectedWaypoint);
        Waypoints.Remove(SelectedWaypoint);
    }

    [RelayCommand]
    private void MoveWaypointUp()
    {
        if (SelectedWaypoint == null) return;
        var i = Waypoints.IndexOf(SelectedWaypoint);
        if (i <= 0) return;
        Waypoints.Move(i, i - 1);
        Profile.Waypoints.Remove(SelectedWaypoint);
        Profile.Waypoints.Insert(i - 1, SelectedWaypoint);
    }

    [RelayCommand]
    private void MoveWaypointDown()
    {
        if (SelectedWaypoint == null) return;
        var i = Waypoints.IndexOf(SelectedWaypoint);
        if (i < 0 || i >= Waypoints.Count - 1) return;
        Waypoints.Move(i, i + 1);
        Profile.Waypoints.Remove(SelectedWaypoint);
        Profile.Waypoints.Insert(i + 1, SelectedWaypoint);
    }

    [RelayCommand]
    private void AddHealingRule()
    {
        var r = new HealingRule { Name = "New Rule" };
        HealingRules.Add(r);
        Profile.HealingRules.Add(r);
    }

    [RelayCommand]
    private void AddHotkey()
    {
        var h = new HotkeyBinding { Name = "New Hotkey", Key = "F5" };
        Hotkeys.Add(h);
        Profile.Hotkeys.Add(h);
    }

    [RelayCommand]
    private void SaveProfile()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "PokeTibia Profile (*.json)|*.json",
            InitialDirectory = ProfileService.DefaultProfilesFolder,
            FileName = $"{Profile.Name}.json"
        };
        if (dlg.ShowDialog() == true)
        {
            ProfileService.Save(Profile, dlg.FileName);
            AddLog($"Perfil salvo em: {dlg.FileName}");
        }
    }

    [RelayCommand]
    private void LoadProfile()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "PokeTibia Profile (*.json)|*.json",
            InitialDirectory = ProfileService.DefaultProfilesFolder
        };
        if (dlg.ShowDialog() == true)
        {
            Profile = ProfileService.Load(dlg.FileName);
            RefreshFromProfile();
            AddLog($"Perfil carregado: {Path.GetFileName(dlg.FileName)}");
        }
    }

    private void AddLog(string m) => Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {m}");
}
m) => Log.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {m}");
}
