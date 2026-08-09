# PokeTibia Bot

Bot para clientes de PokeTibia (OTServ-based) e clientes genéricos de Tibia.

**Stack:** C# WPF (.NET 8, x64) + C++ DLL nativa (leitura de tela, template matching e leitura de memória opcional). Sem injeção de DLL — o bot opera **externamente** ao cliente do jogo.

> AVISO: Automação pode violar os Termos de Serviço do seu servidor. Uso por sua conta e risco. Recomenda-se usar em **servidor próprio/privado** ou em teste local.

---

## Funcionalidades

- **Movimentação por waypoints** (N/S/E/W + diagonais) com loop configurável
- **Uso de itens** (poções de HP/MP) via regras de cura por % de HP/MP
- **Hotkeys de comandos** recorrentes com cooldown (attacks, buffs, etc.)
- **Loot automático** (envia hotkey de "loot all" padrão OTServ)
- **Interação com NPCs** via envio de mensagens sequenciais no chat
- **Editor visual de waypoints** com adicionar/remover/reordenar
- **Perfis JSON** salvar/carregar
- **Record & Play** (grava setas pressionadas no teclado para virar waypoints)
- **Hotkeys globais** para controlar o bot: `Ctrl+F11` Start · `Ctrl+F12` Stop · `Ctrl+F10` Pause/Resume
- **🖥 Overlay HUD** transparente sobre o cliente com HP/MP/estado/waypoint atual (click-through)
- **🛡 Anti-Ban Humanize**: jitter em todos os delays, curvas de Bézier no mouse, pausas de "distração" aleatórias
- **🔎 Memory Scanner** integrado estilo Cheat Engine: First Scan → Next Scan (=, mudou, aumentou, diminuiu) para descobrir offsets de HP/MP/coords

## Estrutura

```
PokeTibiaBot/
├── PokeTibiaBot.sln
├── PokeTibiaBot.UI/           # WPF (C# .NET 8)
│   ├── Models/                # Waypoint, BotProfile, HealingRule, Hotkey
│   ├── Services/              # BotEngine, InputSimulator, NativeBridge,
│   │                          #   ProfileService, GlobalHotkeyService, WaypointRecorder
│   ├── ViewModels/MainViewModel.cs
│   └── MainWindow.xaml
├── PokeTibiaBot.Native/       # C++ DLL (x64)
│   ├── ScreenCapture.cpp      # GDI screen sampling + análise de barras HP/MP
│   ├── ImageProcessor.cpp     # Template matching por SSD (24-bpp BMP)
│   ├── MemoryReader.cpp       # ReadProcessMemory (opcional)
│   └── ProcessFinder.cpp      # EnumWindows/Toolhelp
├── profiles/example.json
└── docs/
    ├── SETUP.md
    └── COMMANDS.md
```

## Como compilar

Você precisa de **Windows + Visual Studio 2022** com as workloads:
- Desktop development with C++
- .NET desktop development

Passos:
1. Abra `PokeTibiaBot.sln` no VS2022.
2. Defina a plataforma como `x64` (é fixa).
3. **Build Solution** (F6) — a DLL C++ compila primeiro e é copiada automaticamente para o output do projeto C# via `<None Update>` no `.csproj`.
4. Rode `PokeTibiaBot.UI` (F5).

Ou pela linha de comando:
```powershell
msbuild PokeTibiaBot.sln /p:Configuration=Release /p:Platform=x64
```

## Uso rápido

1. Abra o cliente do PokeTibia e faça login.
2. No bot: em **Configurações do Perfil** ajuste o "Título da janela do jogo" (ex.: `PokeTibia`).
3. Coloque a janela do jogo em uma resolução fixa e anote as coordenadas de tela das barras HP/MP (use um pipette tool ou o próprio Windows Magnifier). Preencha os campos `HP X/Y/Width` e `MP X/Y/Width`.
4. Adicione waypoints manualmente **ou** clique em `⏺ REC` e ande com as setas — os movimentos são gravados como waypoints em tempo real.
5. Configure regras de cura (ex.: HP ≤ 70% → F1).
6. Salve o perfil (`💾 SAVE`).
7. Pressione **Ctrl+F11** ou o botão `▶ START` para iniciar. **Ctrl+F12** para parar.

Veja `docs/COMMANDS.md` para a lista completa de comandos/hotkeys e `docs/SETUP.md` para configuração detalhada.

## Extensão para leitura de memória

O `NativeBridge.cs` expõe `OpenProcessByName` e `ReadInt32`. Se você conhecer os offsets de HP/MP/coordenadas do seu cliente específico, pode substituir a leitura por barra de tela pela leitura direta de memória — muito mais precisa. Basta chamar essas funções no `BotEngine.TickHealing()`.
