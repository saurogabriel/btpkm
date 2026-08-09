# COMANDOS E AÇÕES

## Hotkeys globais (para controlar o bot)

| Combinação   | Ação                    |
|--------------|-------------------------|
| `Ctrl + F11` | Iniciar o bot           |
| `Ctrl + F10` | Pausar / Retomar        |
| `Ctrl + F12` | Parar o bot             |

Ou use os botões no topo da janela: **START / PAUSE / RESUME / STOP**.

## Botões principais

| Botão      | Função |
|------------|--------|
| `+ Add`    | Adiciona novo waypoint |
| `- Del`    | Remove waypoint selecionado |
| `↑` / `↓`  | Move o waypoint na ordem de execução |
| `⏺ REC`    | Ativa gravação (setas do teclado viram waypoints em tempo real) |
| `💾 SAVE`  | Salva perfil como JSON |
| `📂 LOAD`  | Carrega perfil de JSON |

## Tipos de Waypoint (ação)

| Ação        | Descrição |
|-------------|-----------|
| `Walk`      | Anda em uma direção (`n`,`s`,`e`,`w`,`ne`,`nw`,`se`,`sw`). Usa `Repeat` para andar N vezes. |
| `Wait`      | Espera `WaitMs` milissegundos. |
| `UseItem`   | Pressiona a `Hotkey` definida (F1..F12). |
| `Heal`      | Igual UseItem, semântica de cura. |
| `TalkNpc`   | Envia sequência de mensagens no chat (`NpcMessages`). Ex: `hi`, `trade`, `yes`. |
| `Command`   | Envia texto arbitrário como mensagem de chat. Ex: `/pokeball`, `/return`. |
| `Ladder`    | Clique direito em `ScreenX, ScreenY` (subir/descer escada). |
| `Rope`      | Aperta a `Hotkey` (corda) e depois clique esquerdo em `ScreenX, ScreenY`. |
| `Depot`     | Envia `hi` → `deposit all` → `yes` (padrão OTServ). |
| `Custom`    | Comando livre em `ChatText`. |

## Regras de Cura (Healing)

Cada regra dispara automaticamente quando a barra de HP/MP cair abaixo do %:

- `HP ≤ %` → aperta hotkey (ex.: F1 = potion de HP)
- `MP ≤ %` → aperta hotkey (ex.: F2 = potion de mana)
- `Cooldown` evita spam da mesma poção

O bot lê a cor dos pixels da barra na tela (ver `SETUP.md`) para calcular a %.

## Hotkeys de combate

Cada linha na tabela Hotkeys ativa periodicamente uma tecla ou comando de chat:
- Se `Command` = vazio ou `attack` → apenas aperta a tecla.
- Se `Command` tem texto → envia como mensagem de chat.

Ex.: 
- `Attack move` → F5 a cada 2000ms  
- `Exeta amp res` → F7 a cada 30000ms (buff)  
- `Say something` → chat `estou farmando` a cada 5min

## Formato JSON do perfil

Veja `profiles/example.json`.
