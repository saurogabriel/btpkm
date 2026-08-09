using System;
using System.Windows;
using PokeTibiaBot.Services;

namespace PokeTibiaBot;

public partial class MainWindow : Window
{
    private readonly GlobalHotkeyService _hotkeys = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closed += (s, e) => _hotkeys.Dispose();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _hotkeys.Attach(this);

        var vm = (ViewModels.MainViewModel)DataContext;

        // Hotkeys globais padrão:
        // Ctrl+F11 -> Start | Ctrl+F12 -> Stop | Ctrl+F10 -> Pause/Resume
        const uint MOD_CONTROL = 0x0002;
        _hotkeys.Register(MOD_CONTROL, 0x7A /*F11*/, () => vm.StartBotCommand.Execute(null));
        _hotkeys.Register(MOD_CONTROL, 0x7B /*F12*/, () => vm.StopBotCommand.Execute(null));
        _hotkeys.Register(MOD_CONTROL, 0x79 /*F10*/, () =>
        {
            if (vm.IsRunning) vm.PauseBotCommand.Execute(null);
            else vm.ResumeBotCommand.Execute(null);
        });
    }
}
