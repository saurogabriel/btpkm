using System.Collections.Generic;

namespace PokeTibiaBot.Models;

/// <summary>
/// Tipos de ação que um waypoint pode executar.
/// </summary>
public enum WaypointAction
{
    Walk,       // andar para uma direção (N/S/E/W/NE/NW/SE/SW)
    Wait,       // esperar X milissegundos
    UseItem,    // usar um item da hotbar (F1..F12)
    TalkNpc,    // conversar com NPC (sequência de mensagens)
    Ladder,     // subir/descer escada (clique em coordenada relativa)
    Rope,       // usar corda em coordenada
    Depot,      // depositar itens no depot
    Heal,       // usar poção
    Command,    // executar comando de chat, ex: /pokeball
    Custom      // comando customizado com script
}

public class Waypoint
{
    public int Id { get; set; }
    public string Name { get; set; } = "Waypoint";
    public WaypointAction Action { get; set; } = WaypointAction.Walk;

    // Coordenadas do jogo (opcional, se você lê da memória do cliente)
    public int GameX { get; set; }
    public int GameY { get; set; }
    public int GameZ { get; set; }

    // Direção quando Action == Walk: "n","s","e","w","ne","nw","se","sw"
    public string Direction { get; set; } = "n";

    // Tempo de espera em ms quando Action == Wait
    public int WaitMs { get; set; } = 500;

    // Tecla (F1..F12) quando Action == UseItem ou Heal
    public string Hotkey { get; set; } = "F1";

    // Sequência de mensagens quando Action == TalkNpc
    public List<string> NpcMessages { get; set; } = new();

    // Comando de chat quando Action == Command (ex: "/pokeball", "hi", "trade")
    public string ChatText { get; set; } = "";

    // Coordenadas de tela quando Action == Ladder/Rope (clique)
    public int ScreenX { get; set; }
    public int ScreenY { get; set; }

    // Repetir X vezes (default 1)
    public int Repeat { get; set; } = 1;

    public override string ToString() => $"#{Id} [{Action}] {Name}";
}
