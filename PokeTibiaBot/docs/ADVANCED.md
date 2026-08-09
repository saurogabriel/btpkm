# FEATURES AVANÇADAS

## 🛡 Anti-Ban Humanize

Ativado por padrão no perfil. Configurável em **Configurações do Perfil → ANTI-BAN**:

| Parâmetro | O que faz |
|---|---|
| Humanize ativado | Master switch; se desligado, delays viram fixos e mouse teleporta |
| Jitter (± %) | Adiciona variação aleatória em cada `Sleep(ms)`. 25 = ±25% |
| Micro-pausa min/max (ms) | Pequena pausa aleatória entre ações (imita reação humana) |
| Chance de distração (%) | Chance de fazer uma pausa muito mais longa (simula você olhando pro celular) |
| Distração min/max (ms) | Duração das pausas de distração |

Efeitos aplicados automaticamente:
- **Cursor**: cada `LeftClick`/`RightClick` faz movimento em **curva de Bézier quadrática** com "wobble" (~120-260ms), em vez de teleportar.
- **Digitação**: cada tecla numa mensagem de chat tem delay aleatório entre 55-140ms.
- **Delays de walk/action/wait**: todos passam por `JitterMs()`.

## 🖥 Overlay HUD

Clique no botão **🖥 HUD** no topo do bot para ligar/desligar. Uma janelinha translúcida, **sempre no topo** e **click-through** (não intercepta cliques — você joga normalmente através dela), mostra:

- Estado do bot (Running/Paused/Idle)
- Barra de HP e MP em % (atualizada a cada tick)
- Nome do waypoint atual sendo executado

Arraste o executável para outro monitor se quiser, e mova a janela do overlay uma vez pelo Alt+Space → Mover (WPF preserva a posição por sessão).

## 🔎 Memory Scanner (CE-style)

Clique em **🔎 SCAN**. Fluxo típico para achar o offset de HP:

1. **Anexar** ao processo (`PokeTibia.exe` ou nome do seu cliente). Se falhar por permissão, rode o bot **como Administrador**.
2. Anote seu HP atual (ex.: `500`). Digite no campo Valor e clique **First Scan**. O contador mostrará quantos matches (tipicamente milhares).
3. Volte pro jogo, tome dano ou use poção → HP muda para outro valor (ex.: `480`).
4. Digite `480` no bot e clique **Next: = valor**. A lista encolhe drasticamente.
5. Alternativa: use **Diminuiu** (sem digitar valor) — filtra endereços cujo valor caiu desde o último scan. Bom quando você não sabe o valor exato.
6. Repita até restarem ~1-5 endereços. Cada linha mostra o endereço em hex e o valor atualizado em tempo real (~700ms de refresh).

Uma vez encontrado o offset, você pode:
- Codificá-lo em `BotEngine.TickHealing()` usando `NativeBridge.ReadInt32(handle, address)` para leitura direta (100% precisão, imune a resolução/tema).
- Copiar o endereço e anotar como offset base + delta se o cliente usar ponteiro estático.

> ⚠️ Alguns processos estão protegidos e retornam 0 matches — nesse caso rode o bot como Admin ou desabilite antivírus temporariamente para testar. **Não** injetamos DLL, apenas fazemos `ReadProcessMemory` (leitura, não escrita).
