using System;
using System.IO;
using System.Text.Json;
using PokeTibiaBot.Models;

namespace PokeTibiaBot.Services;

/// <summary>
/// Serializa e desserializa perfis em JSON.
/// </summary>
public static class ProfileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static string DefaultProfilesFolder =>
        Path.Combine(AppContext.BaseDirectory, "profiles");

    public static void EnsureFolder()
    {
        if (!Directory.Exists(DefaultProfilesFolder))
            Directory.CreateDirectory(DefaultProfilesFolder);
    }

    public static void Save(BotProfile profile, string path)
    {
        var json = JsonSerializer.Serialize(profile, Options);
        File.WriteAllText(path, json);
    }

    public static BotProfile Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BotProfile>(json, Options)
               ?? new BotProfile();
    }

    public static BotProfile CreateDefault()
    {
        var p = new BotProfile
        {
            Name = "Default PokeTibia Profile",
            GameWindowTitle = "PokeTibia"
        };
        p.HealingRules.Add(new HealingRule
        {
            Name = "Low HP potion",
            HpThresholdPercent = 60,
            Hotkey = "F1",
            CooldownMs = 1200
        });
        p.HealingRules.Add(new HealingRule
        {
            Name = "Low MP potion",
            HpThresholdPercent = 100,
            MpThresholdPercent = 40,
            Hotkey = "F2",
            CooldownMs = 1500
        });
        p.Hotkeys.Add(new HotkeyBinding
        {
            Name = "Attack move",
            Key = "F5",
            Command = "attack",
            CooldownMs = 2000
        });
        return p;
    }
}
