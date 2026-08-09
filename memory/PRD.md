# PokeTibia Bot — PRD

## Problema
Bot externo para clientes de PokeTibia (OTServ-based). Sem injeção de DLL — apenas leitura de tela, simulação de input e (opcionalmente) leitura de memória via ReadProcessMemory.

## Stack
- C# WPF (.NET 8, x64) — UI, orquestração, MVVM (CommunityToolkit.Mvvm)
- C++ DLL (v143, x64) — GDI screen sampling, template matching, ReadProcessMemory
- P/Invoke ligando os dois; DLL copiada automaticamente ao output.

## Personas
- Player casual em servidor privado que quer automatizar farm.
- Dono de servidor privado testando resistência a bots.

## Requisitos essenciais (MVP) — TODOS IMPLEMENTADOS
- [x] Movimentação por waypoints (8 direções, loop, repeat)
- [x] Uso de itens / poções (regras por % de HP/MP com cooldown)
- [x] Loot automático (hotkey F12 configurável para "loot all")
- [x] Interação com NPCs (envio de sequência de mensagens no chat)
- [x] Configuração de rota (editor visual + JSON manual + Record & Play)
- [x] Comandos/hotkeys arbitrários com cooldown
- [x] Hotkeys globais para Start/Pause/Stop (Ctrl+F11/F10/F12)
- [x] Salvar/carregar perfis em JSON

## Features avançadas (iteração 2) — IMPLEMENTADAS
- [x] **Anti-Ban Humanize**: jitter ±% em todos delays, curvas de Bézier no mouse com wobble, delay realista entre keystrokes, pausas de "distração" aleatórias, tudo configurável no perfil
- [x] **Overlay HUD transparente**: janela WPF `WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_LAYERED`, sempre-no-topo, click-through, mostra HP/MP/estado/waypoint atual em tempo real
- [x] **Memory Scanner CE-style**: DLL C++ com `VirtualQueryEx`/`ReadProcessMemory`, First Scan int32 com limite (default 500k), Next Scan por valor exato, e Next Compare (unchanged/changed/increased/decreased). UI WPF em `MemoryScannerWindow` com DataGrid e refresh de 700ms dos valores atuais

## Arquitetura
```
UI (WPF) ──► ViewModel ──► BotEngine (thread) ──► InputSimulator (SendInput)
                                          └──► NativeBridge (P/Invoke) ──► DLL C++
                                                                          ├── ScreenCapture (GDI)
                                                                          ├── ImageProcessor (SSD)
                                                                          ├── ProcessFinder
                                                                          └── MemoryReader
```

## Não implementado / Backlog
- P1: OCR real (Tesseract) para ler HP/MP em texto (hoje é por cor de pixel)
- P2: Detecção de player próximo (screenshot + análise) → auto-logout / auto-pause
- P2: Sistema de scripts Lua/C# para lógicas custom
- P2: Mapa 2D dos waypoints (visualização gráfica)
- P2: Scanner de ponteiros estáticos (multi-level pointer scan)

## Ambiente de build
Windows + Visual Studio 2022 (x64). Não compilável no Linux/container atual.

## Datas
- 2026-01: MVP entregue (código-fonte completo, documentação, perfil de exemplo).
