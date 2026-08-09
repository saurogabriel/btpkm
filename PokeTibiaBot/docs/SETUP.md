# SETUP DETALHADO

## 1. Pré-requisitos

- Windows 10/11 x64
- Visual Studio 2022 com:
  - **Desktop development with C++** (MSVC v143, Windows 10/11 SDK)
  - **.NET desktop development** (.NET 8 SDK)
- Cliente do jogo PokeTibia rodando em janela (não fullscreen exclusivo).

## 2. Compilar

```powershell
cd PokeTibiaBot
msbuild PokeTibiaBot.sln /p:Configuration=Release /p:Platform=x64
```

O executável ficará em:
`PokeTibiaBot.UI\bin\Release\net8.0-windows\win-x64\PokeTibiaBot.exe`
e junto dele: `PokeTibiaBot.Native.dll`.

> Se após compilar a DLL não aparecer no output do C#, copie manualmente de
> `PokeTibiaBot.Native\x64\Release\PokeTibiaBot.Native.dll` para o mesmo diretório do `.exe`.

## 3. Descobrir coordenadas das barras HP/MP

O bot lê a **cor dos pixels** da barra na tela para calcular a % de HP/MP.

Passos:
1. Coloque o cliente do jogo em uma resolução fixa (ex.: 1280×720) e **não redimensione depois**.
2. Use uma ferramenta como **Windows Magnifier**, **ShareX Color Picker**, ou o próprio **Snipping Tool** para descobrir as coordenadas (X, Y) do início da sua barra de HP e sua largura em pixels.
3. Preencha na aba **Configurações do Perfil**:
   - `HP X`, `HP Y`, `HP Width`
   - `MP X`, `MP Y`, `MP Width`
4. Se a cor da sua barra for diferente do padrão (vermelho para HP, azul para MP), ajuste os campos `HpBarColor` no JSON do perfil manualmente.

## 4. Descobrir a hotkey padrão do loot

No cliente OTServ típico, existe um comando para pegar loot do corpo mais próximo. O bot usa **F12** por padrão para acionar isso. Configure no seu cliente uma hotkey em F12 com o comando de loot (ex.: `!loot`, `/take`, ou o macro nativo).

## 5. Hotkeys globais do bot

Estas funcionam **mesmo com o bot em background**:

| Combinação | Ação |
|---|---|
| `Ctrl + F11` | Start |
| `Ctrl + F10` | Pause / Resume |
| `Ctrl + F12` | Stop |

## 6. Testar sem o jogo

Você pode testar as funções de input abrindo o **Notepad** e:
1. Definindo `Game Window Title = Notepad`
2. Adicionando um waypoint `Command` com texto `hello world`
3. Iniciando o bot

Ele digitará no notepad. Isso valida o pipeline de input, timing e hotkeys sem risco de ban.

## 7. Extensão: leitura de memória

Se você tiver os offsets do seu cliente (obtidos com Cheat Engine no seu próprio servidor privado), edite `BotEngine.cs` e use:

```csharp
var h = NativeBridge.OpenProcessByName("poketibia.exe");
int hp = NativeBridge.ReadInt32(h, 0x00ABC1234);
```

E substitua `TickHealing()` para usar essas leituras em vez de `ReadBarPercent`.
