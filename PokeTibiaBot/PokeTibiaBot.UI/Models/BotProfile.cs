using System.Collections.Generic;

namespace PokeTibiaBot.Models;

public class HotkeyBinding
{
    public string Name { get; set; } = "";
    public string Key { get; set; } = "F1";        // F1..F12
    public string Command { get; set; } = "";      // texto ou comando enviado
    public int CooldownMs { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
}

public class HealingRule
{
    public string Name { get; set; } = "Heal";
    public int HpThresholdPercent { get; set; } = 70;    // usar quando HP <= X%
    public int MpThresholdPercent { get; set; } = 0;     // 0 = não checa mana
    public string Hotkey { get; set; } = "F1";
    public int CooldownMs { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
}

public class LootItem
{
    public string ItemName { get; set; } = "";
    public string ItemImagePath { get; set; } = "";  // template para matching
    public bool AutoLoot { get; set; } = true;
}

public class LootConfig
{
    public bool Enabled { get; set; } = true;
    public bool LootAll { get; set; } = true;              // pega tudo do corpo
    public int LootAttempts { get; set; } = 3;
    public int LootDelayMs { get; set; } = 250;
    public List<LootItem> Items { get; set; } = new();
}

public class BotProfile
{
    public string Name { get; set; } = "New Profile";
    public string Version { get; set; } = "1.0";
    public string GameWindowTitle { get; set; } = "Tibia";  // ajuste conforme cliente

    // Coordenadas na tela onde estão as barras de HP/MP (para leitura por pixel)
    public int HpBarX { get; set; } = 0;
    public int HpBarY { get; set; } = 0;
    public int HpBarWidth { get; set; } = 100;
    public int MpBarX { get; set; } = 0;
    public int MpBarY { get; set; } = 0;
    public int MpBarWidth { get; set; } = 100;

    // Cor esperada da barra de HP (RGB) - default vermelho
    public int HpBarColorR { get; set; } = 220;
    public int HpBarColorG { get; set; } = 60;
    public int HpBarColorB { get; set; } = 60;

    // Cor esperada da barra de MP (RGB) - default azul
    public int MpBarColorR { get; set; } = 60;
    public int MpBarColorG { get; set; } = 120;
    public int MpBarColorB { get; set; } = 220;

    public int WalkDelayMs { get; set; } = 350;
    public int ActionDelayMs { get; set; } = 200;
    public bool LoopWaypoints { get; set; } = true;

    // ---- Anti-ban (Humanize) ----
    public bool HumanizeEnabled { get; set; } = true;
    public int JitterPercent { get; set; } = 25;              // ±% em cada delay
    public int MicroPauseMinMs { get; set; } = 15;
    public int MicroPauseMaxMs { get; set; } = 65;
    public int DistractionChance { get; set; } = 3;           // % de chance de pausa longa
    public int DistractionMinMs { get; set; } = 400;
    public int DistractionMaxMs { get; set; } = 1200;

    public List<Waypoint> Waypoints { get; set; } = new();
    public List<HotkeyBinding> Hotkeys { get; set; } = new();
    public List<HealingRule> HealingRules { get; set; } = new();
    public LootConfig Loot { get; set; } = new();
}
