# TROUBLESHOOTING

## "Trava e fecha ao anexar o processo"

Causas possíveis, em ordem de probabilidade:

### 1. DLL nativa ausente ou arquitetura errada
Depois de compilar, verifique que **ao lado do `PokeTibiaBot.exe`** existe o arquivo `PokeTibiaBot.Native.dll` (~ >50 KB).

- Caminho esperado: `PokeTibiaBot.UI\bin\Release\net8.0-windows\PokeTibiaBot.Native.dll`
- Se **não existir**, compile o projeto `PokeTibiaBot.Native` **antes** do UI (ou Build Solution com F6). O `.csproj` do UI agora falha o build explicitamente com mensagem clara se a DLL não estiver disponível.
- Se existir mas o app crasha ao carregar → provavelmente compilada em **x86**. Confirme que ambos os projetos estão em **x64** (dropdown no topo do Visual Studio).

Nas versões atuais do bot, a `MemoryScannerWindow` verifica a DLL no `Loaded` e mostra uma **MessageBox** amigável em vez de crashar, então se você ver essa mensagem, é isso.

### 2. Sem permissão de leitura de memória
`OpenProcess(PROCESS_VM_READ)` pode falhar silenciosamente se o cliente do jogo estiver rodando com privilégios mais altos que o bot.

**Solução:** feche o bot, clique com botão direito no `PokeTibiaBot.exe` → **"Executar como administrador"**.

### 3. Nome do processo errado
Se você digitava manualmente antes, era fácil errar. Agora a versão atualizada da tela de Scanner tem um **ComboBox listando todos os processos abertos** — apenas selecione. Se algo mudou, clique no botão **↻** ao lado pra recarregar.

### 4. Anti-cheat / cliente protegido
Alguns clientes usam drivers de proteção que interceptam `ReadProcessMemory` e podem crashar o processo que tenta ler. Isso é raro em PokeTibia OTServ (que geralmente não tem proteção). Se acontecer, você não vai conseguir usar o scanner nesse cliente — é uma limitação do jogo, não do bot.

### 5. First Scan travando o app (parece crash)
Antes o scan rodava na thread da UI e a janela ficava "Not responding" durante 10-30s em jogos grandes. Isso foi corrigido: agora todo scan roda em **background com barra de progresso**, e o app volta sozinho quando termina.

Se ainda assim demora demais, diminua o campo **Max** de `500000` para `100000`.

---

## Como confirmar que a DLL carregou corretamente

Abra o **Task Manager → Details → PokeTibiaBot.exe → botão direito → Analyze wait chain / Modules**. Você deve ver `PokeTibiaBot.Native.dll` listado. Se não vê, é problema de path/arquitetura.

Alternativa via PowerShell no diretório do exe:
```powershell
Get-Item PokeTibiaBot.Native.dll
```
Deve retornar o arquivo. Se der erro, ele não foi copiado.
