# GameShell

The shared web GUI of the board games [Checkers](https://github.com/cjp-dev/Checkers),
[Connect 4](https://github.com/cjp-dev/Connect-4), [Gomoku](https://github.com/cjp-dev/Gomoku) and
[Stello](https://github.com/cjp-dev/Stello): one Razor class library, so the four Blazor WebAssembly
apps look and behave the same.

The GUI is described in [docs/general-game-gui-specification.md](docs/general-game-gui-specification.md).

## What is in it

| Part | What it does |
|---|---|
| `ShellLayout` | The page frame: desktop menu bar (File, Game, Options, Help), mobile ☰ drawer, `BoardArea` and `SidePanels` slots, keyboard shortcuts (Ctrl+letter, F1, Shift+F1, F2, Escape), menu bar lined up with the board |
| `MenuDefinition`, `MenuEntry` | The menus as data: text, command, shortcut text, ✓ toggles, ● radio groups, separators |
| `StatusPanel`, `PlayerCard` | STATUS header (crimson when the game was modified), variant and limit badges, turn message, thinking bar, two player cards, Undo / Redo / Move Now |
| `AnalysisPanel` | The seven analysis fields, from an `IAnalysisView` the game provides |
| `MoveHistoryPanel` | The moves in paired rows, the latest move highlighted, scrolled into view |
| `gameshell.css` | The shared look (served as `_content/GameShell/gameshell.css`) |
| `gameshell.js` | Shortcut listener, menu alignment, history scrolling, text file open and download (`_content/GameShell/gameshell.js`) |

Each game keeps its own board, dialogs, engine and view models. The view models live in the game's
platform-neutral `.App` project (shared with its WPF version), so GameShell never references them: the game
passes plain values, commands and small adapters.

## Using it in a game

GameShell is a git submodule of each game, so every game pins the GameShell commit it was built and tested with.

```bash
git submodule add https://github.com/cjp-dev/GameShell.git external/GameShell
```

Reference the library from the game's web project:

```xml
<ProjectReference Include="..\..\external\GameShell\src\GameShell\GameShell.csproj" />
```

Load the styles before the game's own and the script before Blazor in `wwwroot/index.html`:

```html
<link rel="stylesheet" href="_content/GameShell/gameshell.css" />
<link rel="stylesheet" href="css/app.css" />
...
<script src="_content/GameShell/gameshell.js"></script>
<script src="_framework/blazor.webassembly#[.{fingerprint}].js"></script>
```

Then build the page from the components (abridged):

```razor
<ShellLayout Brand="Connect 4" Menus="_menus" Shortcuts="_shortcuts" Inert="@(Dialogs.Current is not null)" OnCommand="() => { }">
    <BoardArea>
        <Board Cells="Vm.Cells" OnPlay="Play" />
    </BoardArea>
    <SidePanels>
        <StatusPanel IsModified="Vm.IsStatusModified" VariantBadge="@Vm.VariantBadgeText"
                     SettingBadge="@(Vm.IsVsComputer ? Vm.SettingsBadgeText : null)"
                     StatusText="@Vm.TurnIndicatorText" IsThinking="Vm.IsAiThinking"
                     First="@RedPlayer" Second="@YellowPlayer"
                     Undo="Vm.UndoCommand" CanUndo="Vm.CanUndo" Redo="Vm.RedoCommand" CanRedo="Vm.CanRedo"
                     MoveNow="@(Vm.IsVsComputer ? Vm.MoveNowCommand : null)" CanMoveNow="Vm.IsAiThinking" />
        <AnalysisPanel Analysis="_analysis" />
        <MoveHistoryPanel Rows="@HistoryRows" />
    </SidePanels>
</ShellLayout>
```

The game's stylesheet adds the board, the player colour dots used by `PlayerInfo.GlyphClass` (e.g. `.red-dot`)
and its dialogs. The board size is set with CSS variables; see the top of `gameshell.css`.

In the game's deploy workflow, check out the submodule and redeploy when it changes:

```yaml
on:
  push:
    paths:
      - 'external/GameShell/**'
      # ...the game's own paths
jobs:
  build-and-deploy:
    steps:
      - uses: actions/checkout@v7
        with:
          submodules: true
```

Clone a game with `git clone --recurse-submodules`, or run `git submodule update --init` in an existing clone.

## Updating a game to a newer GameShell

```bash
cd external/GameShell
git pull origin main
cd ../..
git add external/GameShell
git commit -m "Update GameShell"
```

## Development

```bash
dotnet build GameShell.slnx
dotnet test GameShell.slnx
```

The tests (xUnit and bUnit) render the components and check their behaviour.
