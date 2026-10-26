# General Game GUI Description & Engineering Blueprint

> **A Universal Implementation Specification and Architecture Checklist for Turn-Based 2-Player Board Games**  
> *Derived from Checkers .NET and Gomoku .NET Architecture. The web version is implemented by the shared **GameShell** library (see §11).*

---

## 1. Executive Summary & Purpose

### 1.1 Objective
This document defines the **standard graphical user interface (GUI) architecture, visual layouts, state contracts, and component behaviors** for two-player turn-based board games (e.g., Checkers/Draughts, Gomoku/Renju, Chess, Reversi/Othello, Connect Four, Nine Men's Morris).

It is written specifically as an **engineering specification and checklist** for other game projects. By adhering to this specification, developers and AI engineering agents can build an identical, tournament-grade user experience across both **Web (Blazor WebAssembly / Single Page Application)** and **Desktop (WPF / WinUI / Avalonia)** platforms, customizing only game-specific rules, board dimensions, and custom settings options.

### 1.2 Core Design Principles
1. **Unified Dual-Platform Parity:** Core presentation logic resides in a shared MVVM ViewModel layer (`.App`), consumed identically by Web and Desktop frontends.
2. **Modern High-Contrast Light Theme:** Warm neutral parchment/wood styling, elevated white cards, soft ambient drop shadows, and high-readability slate typography.
3. **Responsive Adaptive Layout:** Board and sidebar side by side, centred on wide screens; stacked (board above the panels) on narrow or portrait screens; the menu bar becomes a slide-out navigation drawer at $\le 768\text{ px}$. See §2.4 for the exact rules.
4. **Zero-Lag Asynchronous AI Architecture:** Heavy engine search operations execute in isolated background threads (Web Workers in browser; background Tasks on desktop) with progress streaming and non-blocking cooperative yielding.
5. **Fair Play & Competitive Transparency:** Dynamic match integrity tracking (turning `"STATUS"` crimson red `#E74C3C` when game state has been altered) and interactive timeout continuation handling.

---

## 2. Master Layout & Visual Design System

### 2.1 Component Architecture Overview

```mermaid
graph TD
    App["Game Application Window / Page"]
    App --> Header["Top Header / Menu System<br/>Desktop: Horizontal Menu Bar (File • Game • Options • Help)<br/>Mobile (≤768px): Brand Title + Hamburger Drawer (☰)"]
    App --> MainContent["Responsive Content Container"]

    subgraph DesktopLayout ["Two-Column Layout (> 768px)"]
        MainContent --> BoardCol["Primary Board Column<br/>Interactive Board • Coordinates • Active Selection • Threat Indicators"]
        MainContent --> SideCol["Sidebar Information Column"]
        SideCol --> ScoreCard["Dual-Card Scoreboard<br/>Status Header • Variant Badge • Time Setting • Clocks • Player Cards"]
        SideCol --> Actions["Quick Action Controls<br/>Undo • Redo • Move Now"]
        SideCol --> AnalysisCard["Real-Time AI Analysis Card<br/>Move • Depth • Score • Best Move • Nodes • Speed • Time"]
        SideCol --> HistoryCard["Move History Card<br/>Two-Column Plies • Algebraic Notation • Auto-Scroll"]
    end

    subgraph MobileLayout ["Fluid Stacked Layout (≤ 768px)"]
        MobileStack["Board → Scoreboard → Actions → Analysis → Move History"]
    end
```

### 2.2 Wireframe Layout

#### Desktop Multi-Column View ($> 768\text{ px}$)
```
┌────────────────────────────────────────────────────────────────────────────────────────────────────┐
│ File   Game   Options   Help                                                       [Desktop Menu]  │
├────────────────────────────────────────┬───────────────────────────────────────────────────────────┤
│                                        │ STATUS                [Variant Badge]         5 min / game│
│                                        ├───────────────────────────────────────────────────────────┤
│                                        │ White's Turn — Click a piece to move it                   │
│                                        │ ┌───────────────────────────┐ ┌─────────────────────────┐ │
│                                        │ │ ○ White              4:58 │ │ ● Black            5:00 │ │
│                                        │ │         Human             │ │        Computer         │ │
│                                        │ │          12               │ │           12            │ │
│                                        │ │       Kings: 0            │ │        Kings: 0         │ │
│                                        │ └───────────────────────────┘ └─────────────────────────┘ │
│                                        │ [   Undo   ]        [   Redo   ]        [ Move Now (Ctrl+M)]│
│                                        ├───────────────────────────────────────────────────────────┤
│                                        │ ANALYSIS                                                  │
│         INTERACTIVE GAME BOARD         │ Move:      6                     Nodes: 666,790           │
│                                        │ Depth:     9 plies               Speed: 345,666 nps       │
│        (e.g., 8x8 Checkers /           │ Score:     +34                   Time:  0:01.9            │
│         15x15 Gomoku / 8x8 Chess)      │ Best move: 18                                             │
│                                        ├───────────────────────────────────────────────────────────┤
│                                        │ MOVE HISTORY                                              │
│                                        │ #   White           Black                                 │
│                                        │ 1.  11-15           24-20                                 │
│                                        │ 2.  8-11            28-24                                 │
│                                        │ 3.  9-14            22-17 (active)                        │
└────────────────────────────────────────┴───────────────────────────────────────────────────────────┘
```

#### Mobile View ($\le 768\text{ px}$)
```
┌────────────────────────────────────────┐
│ Checkers .NET                       ☰  │  <-- Header with Brand Title & Hamburger Button
├────────────────────────────────────────┤
│                                        │
│         INTERACTIVE GAME BOARD         │  <-- Full-width responsive square board
│                                        │
├────────────────────────────────────────┤
│ STATUS       [Variant]     5 min / game│
│ White's Turn                           │
│ ┌──────────────────┐┌────────────────┐ │
│ │ ○ White     4:58 ││ ● Black   5:00 │ │
│ │ Human         12 ││ Computer    12 │ │
│ └──────────────────┘└────────────────┘ │
│ [  Undo  ]   [  Redo  ]   [ Move Now ] │
├────────────────────────────────────────┤
│ ANALYSIS                               │
│ Depth: 9 plies       Nodes: 666,790    │
│ Score: +34           Speed: 345,666 nps│
│ Best:  18            Time:  0:01.9     │
├────────────────────────────────────────┤
│ MOVE HISTORY                           │
│ 1. 11-15  24-20 | 2. 8-11  28-24       │
└────────────────────────────────────────┘
```

### 2.3 Master Color System & Design Tokens

Every implementing project should preserve these core color tokens to maintain visual consistency across games:

| Token Name | Hex Code | Purpose & Application |
|---|---|---|
| `--app-bg` | `#F4F1EA` / `#F5F7FA` | App background canvas (warm parchment / clean light gray) |
| `--card-bg` | `#FFFFFF` | Elevated surface for scoreboard, analysis panel, and move history |
| `--card-border` | `#E2E8F0` / `#CBD5E1` | Subtle $1\text{ px}$ borders with $8\text{ px}$ card border radius |
| `--text-primary` | `#2C3E50` / `#0F172A` | High-contrast dark slate text for headings and primary metrics |
| `--text-muted` | `#7F8C8D` / `#64748B` | Secondary text for labels, subtitles, and neutral status headers |
| `--active-turn` | `#3498DB` / `#3B82F6` | Sky-blue border, glowing ring, and turn indicator for active side |
| `--status-modified` | `#E74C3C` / `#DC2626` | Bold crimson red indicating modified game state (Undo/Switch/Timeout) |
| `--badge-variant` | `#16A085` / `#2C3E50` | Teal or deep slate pill badges for variant indicators |
| `--selection-glow` | `#F39C12` / `#F59E0B` | Warm amber highlight for selected pieces or source squares |
| `--target-dot` | `#2ECC71` / `#10B981` | Emerald green indicators for legal destination squares |
| `--btn-primary` | `#3498DB` | Primary action button background (e.g. "Move Now", modal OK) |
| `--btn-secondary` | `#ECF0F1` / `#F1F5F9` | Secondary button background (Undo, Redo, Cancel) |

### 2.4 Layout Rules & Defaults

1. **Centred layout:** On wide screens the board and the side panels are centred as one group (maximum width $1650\text{ px}$). The desktop menu bar is lined up with the board's left edge.
2. **Breakpoints by width and shape:** A square board needs height as much as width, so stacking depends on the screen's shape:
   - *Side by side:* wider than $900\text{ px}$, or $601$–$900\text{ px}$ in landscape (compact: narrower sidebar, smaller gaps).
   - *Stacked* (board → scoreboard → actions → analysis → history): $\le 600\text{ px}$, or $\le 900\text{ px}$ in portrait.
   - *Menu:* the horizontal menu bar turns into the ☰ header and drawer at $\le 768\text{ px}$, independently of stacking.
3. **Default game mode:** A first visit starts **vs Computer**, with the human playing the side that moves first.
4. **Board style:** Each game offers its board styles as radio items in the **Options** menu (e.g. `Golden Oak Board`, `Reddish Wood Board`), not in the Settings dialog. The choice is remembered.

---

## 3. Top Navigation & Menu System

The top navigation system exposes session lifecycle commands, opponent selection, settings, and documentation.

### 3.1 Standard 4-Menu Desktop Hierarchy

```
File                     Game                          Options              Help
├── New Game (Ctrl+N)    ├── vs Computer (White)       ├── Analysis (Ctrl+A)├── User guide (F1)
├── Open...  (Ctrl+O)    ├── vs Computer (Black)       ├── Sound            ├── Rules & Documentation (Shift+F1)
├── Save     (Ctrl+S)    ├── vs Human (Two players)    ├─────────────────   └── About [Game]...
├── Save As...           ├─────────────────────────    ├── [Board styles]
└── Exit [Desktop only]  ├── Switch sides              ├─────────────────
                         ├─────────────────────────    └── [Custom Options]
                         └── Settings... (F2)
```

#### 1. File Menu
* **`New Game` (`Ctrl+N`):** Resets board to initial layout, wipes move history, clears undo/redo stacks, restores clocks to full time, clears modified game flags, and begins a fresh game.
* **`Open...` (`Ctrl+O`):** Prompts file picker to open a saved game record (`.pdn`, `.sgf`, `.pgn`, `.json`), parsing metadata and move history.
* **`Save` (`Ctrl+S`):** Saves the game directly to the current active file path (or triggers Save As if unsaved).
* **`Save As...`:** Prompts file save dialog to export standard match notation with full header tags (`[Event]`, `[Date]`, `[White]`, `[Black]`, `[Result]`, `[Variant]`).
* **`Exit`** *(Desktop WPF/WinUI only)*: Gracefully terminates the application window.

#### 2. Game Menu
* **`vs Computer (White)`:** Configures single-player mode where the **Human plays White/Player 1** (moving first from the bottom).
  * *Restart Safeguard:* If clicked while a match is currently in progress, a confirmation warning dialog prompts: *"Abandon current match and start a new game?"* with `Yes` and `No`. (Questions are answered with `Yes` / `No`; `OK` / `Cancel` is used for forms such as the Settings dialog.)
* **`vs Computer (Black)`:** Configures single-player mode where the **Computer plays White/Player 1** (moving first) and the **Human plays Black/Player 2**. The board flips $180^\circ$ so Black is positioned at the bottom.
  * *Restart Safeguard:* Includes the identical confirmation warning prompt when a match is in progress.
* **`vs Human (Two players)`:** Enables local hot-seat multiplayer mode where two human players share the interface without computer calculation.
* *(Separator)*
* **`Switch sides`:** Allows the human player to swap sides with the computer mid-game during their turn without resetting the game state or move history.
  * *Enabling Preconditions:* Enabled **strictly** when: `IsVsComputer == true` AND `IsHumanTurn == true` AND `IsGameOver == false` AND `IsSearching == false`.
  * *Side Effect:* Immediately marks the game as modified (`IsGameModified = true`), triggering the crimson red `#E74C3C` `"STATUS"` header.
* *(Separator)*
* **`Settings...` (`F2`):** Opens the modal Game Settings dialog.

#### 3. Options Menu (or View + Options)
* **`Analysis` (`Ctrl+A`):** Toggles visibility of the real-time AI Search Analysis telemetry card.
* **`Sound`:** Toggles procedural audio feedback (piece selection, placement, captures, errors, game win/loss fanfare).
* *Board Styles:* Radio items for the game's board looks (see §2.4).
* *Game-Specific Extension Slot:* Projects insert game-specific toggles here (e.g. `Coordinate Labels`, `Ghost Stone`, `3D Discs`, `Animate Drops`).
* *Undo / Redo are not menu items:* they are the quick action buttons (§5) and the `Ctrl+Z` / `Ctrl+Y` shortcuts (§9).

#### 4. Help Menu
* **`User guide` (`F1`):** Navigates immediately to the graphical interface and control manual (`/docs/14-gui-and-settings` or local viewer).
* **`Rules & Documentation` (`Shift+F1`):** Navigates directly to the comprehensive engine architecture, game rules, and technical documentation.
* **`About [Game]...`:** Opens the modal About dialog containing author information, game summary, technical engine stack (.NET version, search algorithms, evaluation heuristics), and author portrait.

---

### 3.2 Responsive Mobile Drawer Navigation ($\le 768\text{ px}$)

```mermaid
sequenceDiagram
    autonumber
    actor User as Mobile User (Touch)
    participant Header as Mobile Header (☰)
    participant Drawer as Slide-Out Nav Drawer
    participant VM as MainViewModel
    participant Dialog as DialogHost

    User->>Header: Tap Hamburger (☰)
    Header->>Drawer: Open drawer with backdrop overlay
    Note over Drawer: Sections: File • Game • Options • Help<br/>Touch targets ≥ 44px
    User->>Drawer: Tap "vs Computer (Black)"
    Drawer->>VM: Check if game in progress
    alt Game In Progress
        VM->>Dialog: Show NewGameConfirmDialog
        Dialog-->>User: "Abandon current match?"
        User->>Dialog: Confirms "OK"
    end
    VM->>VM: Set GameMode, flip board, reset match
    Drawer->>Drawer: Close drawer & backdrop
```

#### Mobile Implementation Requirements:
1. **Header Bar:** At $\le 768\text{ px}$, replace the horizontal menu bar with a compact header featuring the application title (e.g., `Checkers`) on the left and a hamburger button (`☰`) on the right, within easy reach of the right thumb.
2. **Slide-Out Drawer:** Tapping the hamburger slides out a navigation drawer from the right (width: $280\text{ px}$ to $320\text{ px}$) with a semi-transparent dark backdrop overlay (`rgba(0, 0, 0, 0.5)`).
3. **Touch Sizing:** Every menu item must have a minimum vertical touch height of $44\text{ px}$ with generous horizontal padding.
4. **State Indicators:** Current selection radio states must be visibly indicated with `●` (e.g., `● vs Computer (White)`), and toggle states with `✓` (e.g., `✓ Sound`).
5. **Backdrop & Escape Dismissal:** Clicking the backdrop, pressing `Escape`, or selecting any executable action command automatically closes the drawer.

---

## 4. Dual-Card Scoreboard & Dynamic Status Board

The scoreboard sits directly adjacent to or above the game board, presenting real-time game status, turn guidance, chess clocks, and player statistics.

### 4.1 Visual Structure & Layout

```
┌────────────────────────────────────────────────────────────────────────┐
│ STATUS              [Variant Badge]                       5 min / game │  <-- Header Row
├────────────────────────────────────────────────────────────────────────┤
│ White's Turn — Click a piece to move it                                │  <-- Turn Banner
│ ┌─────────────────────────────┐   ┌──────────────────────────────────┐ │
│ │ ○ White                4:58 │   │ ● Black                     5:00 │ │  <-- Card Layer 1
│ │          Human              │   │             Computer             │ │  <-- Card Layer 2
│ │           12                │   │                12                │ │  <-- Card Layer 3
│ │        Kings: 0             │   │             Kings: 0             │ │  <-- Card Layer 4
│ └─────────────────────────────┘   └──────────────────────────────────┘ │
│ [   Undo   ]             [   Redo   ]             [ Move Now (Ctrl+M)] │  <-- Quick Actions
└────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Header Row & Status Badges
1. **Status Label (`STATUS`):**
   - **Default State:** Rendered in neutral muted slate (`#7F8C8D` / `#64748B`).
   - **Modified State:** When `IsVsComputer && IsGameModified` is true, changes to bold **Crimson Red (`#E74C3C`)**.
2. **Variant Badge (Center):**
   - Pill-shaped badge displaying the active rules variant (e.g., `[International]` in teal `#16A085`, `[English]` in navy `#2C3E50`, or `[Renju]` in amber `#D97706`).
3. **Computer Limit Badge (Right):**
   - When playing against the computer, displays the active engine thinking limit (e.g., `5 min / game`, `5s / move`, `8 plies`).
   - **Two-Player Behavior:** When playing `vs Human (Two players)`, this badge is automatically hidden.

### 4.3 Modified Game Specification (`#E74C3C`)

To guarantee competitive transparency, the game must track whether the human player has taken any action that compromises standard tournament conditions or provides an unfair advantage.

```mermaid
stateDiagram-v2
    [*] --> Unmodified: Start New Game vs Computer
    Unmodified: STATUS text is Slate Gray (#7F8C8D)
    Modified: STATUS text is Crimson Red (#E74C3C)

    Unmodified --> Modified: Undo Move Used
    Unmodified --> Modified: Switch Sides Used
    Unmodified --> Modified: Settings Changed Mid-Game
    Unmodified --> Modified: Continue After Timeout (Human)

    Modified --> Unmodified: New Game Started (Ctrl+N)
```

#### The Four Modification Triggers:
1. **Taking Back Moves (`Undo`):** The player reverts a move to explore an alternative or correct a blunder.
2. **Switching Sides (`Switch sides`):** The player takes over the computer's superior position mid-match.
3. **Mid-Game Settings Adjustment:** The player opens Settings and lowers engine thinking time or depth after moves have already been played.
4. **Continuing After Timeout:** The player runs out of clock time against the computer but elects to continue playing.

*Note: In Human vs Human matches, `IsGameModified` remains false because both sides have equal access to the physical interface.*

---

### 4.4 The 4-Layer Player Score Card Contract

Each player (White/Player 1 and Black/Player 2) has an identical, elevated card surface (`#FFFFFF`). The player whose turn it is to move receives the **Active Turn Highlight**: an elevated border stroke in sky-blue (`#3498DB`), a $2\text{ px}$ glow, and slightly raised card elevation.

```
┌───────────────────────────────────────────────┐
│ [Glyph] [Player Name]                 [Clock] │  <-- Layer 1: Identity & Time
│                    [Role]                     │  <-- Layer 2: Player Role Subtitle
│                   [Metric 1]                  │  <-- Layer 3: Primary Game Metric
│                   [Metric 2]                  │  <-- Layer 4: Secondary Game Metric
└───────────────────────────────────────────────┘
```

#### Layer-by-Layer Contract:
* **Layer 1 (Identity & Clock):**
  - **Color Glyph:** High-contrast circular swatch (`○` White / Ivory, `●` Black / Obsidian).
  - **Player Name:** `"White"` or `"Black"` (or custom player handle).
  - **Digital Clock:** Digital countdown formatted as `M:SS` (e.g., `4:58`). Visible only when a game time limit is active.
* **Layer 2 (Role Subtitle):**
  - In `vs Computer (White)`: White displays `"Human"`; Black displays `"Computer"`.
  - In `vs Computer (Black)`: White displays `"Computer"`; Black displays `"Human"`.
  - In `vs Human (Two players)`: White displays `"Player 1"`; Black displays `"Player 2"`.
* **Layer 3 (Primary Game Metric):**
  - Large prominent number representing primary material or progress.
  - *Checkers:* Pieces remaining on board (`12` down to `0`).
  - *Gomoku:* Total stones placed (`0` to `225`).
  - *Chess:* Material score differential (e.g., `+3` or captured piece icons).
* **Layer 4 (Secondary Game Metric):**
  - Supplementary strategic attribute.
  - *Checkers:* Kings count (`Kings: 2`).
  - *Gomoku:* Threat counts (e.g. `Fours: 1`, `Open Threes: 2`).
  - *Chess / Reversi:* Discs owned, captures made, or pawn promotions.

---

### 4.5 Chess Clocks & Timeout Continuation Flow

When **Time per Game** is configured:
1. **Clock Initialization:** Both clocks are initialized to full duration (e.g., `5:00`).
2. **Clock Ticking:** The clock of the side whose turn it is counts down every $250\text{ ms}$. Clocks do not begin ticking until the opening move of the match is executed.
3. **Time Refund on Undo:** When the human player uses `Undo`, the elapsed time consumed during that turn is refunded back to the player's clock.
4. **Computer Timeout:** If the computer clock reaches `0:00`, the match terminates immediately as a victory on time for the human player (`"White/Black won on time"`).
5. **Human Timeout Prompt (The Continuation Dialog):**
   If the human player reaches `0:00` against the computer, the game does **not** terminate abruptly. Instead, a modal dialog appears:

```mermaid
flowchart TD
    Timeout["Human Player Clock Reaches 0:00"] --> Dialog{"Modal Dialog:<br/>'You ran out of time!<br/>Do you want to continue playing?'"}
    Dialog -- "No" --> Defeat["Game Over: Loss on Time<br/>Match Terminated"]
    Dialog -- "Yes" --> Continue["Continue Match<br/>• Clock frozen at 0:00<br/>• IsGameModified = true<br/>• STATUS turns Crimson Red (#E74C3C)"]
```

---

## 5. Quick Action Controls

Positioned immediately beneath the dual-card scoreboard:

| Action Button | Shortcut | Enabling Conditions | Execution Behavior |
|---|---|---|---|
| **`Undo`** | `Ctrl+Z` | `HistoryCount > 0` AND `!IsSearching` | In vs Computer, reverts 2 plies (both sides). In 2-player, reverts 1 ply. Refunds clock time. Marks game Modified. |
| **`Redo`** | `Ctrl+Y` | `RedoStackCount > 0` AND `!IsSearching` | Re-executes the next ply from the forward stack. |
| **`Move Now`** | `Ctrl+M` | **`IsSearching == true`** (Computer calculating) | Cancels the active background search token (`_cts.Cancel()`). Engine immediately plays best move from completed iteration. |

> [!IMPORTANT]
> The **Move Now** button must be visually distinct (e.g. Primary Blue `#3498DB` or glowing Amber) and **disabled** whenever the computer is not actively searching. Clicking it must immediately signal cancellation to the engine thread without locking the UI.

---

## 6. Real-Time AI Search Analysis Panel & Threading Contract

The AI Analysis panel displays live telemetry streamed from the engine during calculation, giving players visibility into the computer's thinking process.

### 6.1 Visual Card Layout

```
┌────────────────────────────────────────────────────────┐
│ ANALYSIS                                               │
├────────────────────────────┬───────────────────────────┤
│ Move:                      │ 6                         │
│ Depth:                     │ 9 plies                   │
│ Score:                     │ +34                       │
│ Best move:                 │ 18                        │
│ Nodes:                     │ 666,790                   │
│ Speed:                     │ 345,666 nps               │
│ Time:                      │ 0:01.9                    │
└────────────────────────────┴───────────────────────────┘
```

### 6.2 The 7 Telemetry Fields

| Field Name | Type / Format | Example Value | Description & Rules |
|---|---|---|---|
| **`Move`** | Algebraic notation | `18`, `d3`, `4` | The move the engine is examining right now at the root of the search (changes rapidly while thinking; afterwards the move played). |
| **`Depth`** | String | `9 plies` or `"Book"` | Current iterative deepening depth completed, or `"Book"` if playing from opening book. |
| **`Score`** | Formatted string | `+34`, `-120`, `+Win in 5` | Heuristic evaluation from computer's perspective in centipawns (`+` advantage, `-` disadvantage). |
| **`Best move`** | Algebraic notation | `18`, `H8`, `e2-e4` | Leading candidate move coordinate from the principal variation of the deepest completed iteration. |
| **`Nodes`** | Integer (N0) | `666,790` | Cumulative game tree positions evaluated during this search, with thousands separators. |
| **`Speed`** | String | `345,666 nps` | Nodes evaluated per second ($\text{nps} = \frac{\text{Nodes}}{\text{ElapsedSeconds}}$). |
| **`Time`** | String (`m:ss.f`) | `0:01.9` or `1.93s` | Elapsed wall-clock calculation duration for the active turn. |

---

### 6.3 Threading & Execution Architecture

To prevent browser tab freezing (WebAssembly) or desktop UI locking (WPF), AI engines must follow this execution and reporting contract:

```mermaid
sequenceDiagram
    autonumber
    participant UI as UI Thread (ViewModel / Razor / WPF)
    participant Worker as Background Search Worker (Web Worker / Task.Run)
    participant Engine as Search Engine (Alpha-Beta / Negamax)

    UI->>Worker: StartSearch(boardState, limits, cancellationToken)
    Worker->>Engine: Begin Iterative Deepening (Depth 1..N)

    loop Every Depth Iteration or Heartbeat (≥150ms)
        Engine->>Worker: Emit SearchAnalysis snapshot
        Worker->>UI: Progress.Report(snapshot) / postMessage
        UI->>UI: Update AnalysisViewModel & StateHasChanged()
        opt WebAssembly Single-Thread Mode
            Engine->>Engine: await Task.Delay(1) (Yield to browser event loop)
        end
    end

    alt Player clicks 'Move Now'
        UI->>Worker: cancellationTokenSource.Cancel()
        Engine->>Engine: Detects IsCancellationRequested
        Engine-->>Worker: Return best move from last completed depth
    else Search Limit Reached
        Engine-->>Worker: Return best move
    end

    Worker-->>UI: OnSearchCompleted(bestMove)
    UI->>UI: ApplyMoveToBoard(bestMove)
```

#### Key Implementation Details:
1. **Immutable Snapshot Transfer:** Engine telemetry must be encapsulated in an immutable data structure (`SearchAnalysis` or `SearchProgressInfo`) containing `Move`, `Depth`, `Score`, `BestMove`, `Nodes`, `Nps`, and `ElapsedMs`.
2. **Heartbeat Throttle ($\ge 150\text{ ms}$):** To prevent UI rendering starvation, progress updates should not dispatch more frequently than once every $100\text{–}150\text{ ms}$, plus at the exact boundary of each completed depth iteration.
3. **Natural Feel Delay ($250\text{ ms}$):** If the AI finds an instantaneous book move or forced reply ($< 50\text{ ms}$), insert an intentional $250\text{ ms}$ presentation delay before executing the move so the player can perceive the response.
4. **WebAssembly Cooperative Yielding:** In single-threaded browser runtimes where Web Workers are not employed, the engine loop must periodically execute `await Task.Delay(1, cancellationToken)` to yield execution back to the browser event loop for 60 FPS animation and input responsiveness.

---

## 7. Move History & Game Record Persistence

The move history panel gives players a permanent, readable transcript of the match, supports reviewing earlier positions, and serializes to standard game file formats.

### 7.1 Visual Layout & Auto-Scroll

```
┌────────────────────────────────────────────────────────┐
│ MOVE HISTORY                                           │
├───────┬────────────────────────┬───────────────────────┤
│ #     │ White                  │ Black                 │
├───────┼────────────────────────┼───────────────────────┤
│ 1.    │ 11-15                  │ 24-20                 │
│ 2.    │ 8-11                   │ 28-24                 │
│ 3.    │ 9-14                   │ 22-17                 │
│ 4.    │ 15-18                  │ (thinking...)         │
└───────┴────────────────────────┴───────────────────────┘
```

#### UI Behavior Rules:
1. **Two-Column Turn Grouping:** Numbered sequentially by round (`1.`, `2.`, `3.`), with the side that moves first in column 1 and the other side in column 2. The header counts individual moves of both players: `MOVE HISTORY (8 moves)`, `(1 move)`.
2. **Active Move Highlighting:** The most recent move (one cell, not the whole row) is highlighted with a subtle sky-blue background (`#EBF5FB`).
3. **Auto-Scroll to Bottom:** As new moves are committed, the history list scrolls itself smoothly to the latest move (`list.scrollTo({ top: list.scrollHeight, behavior: 'smooth' })`). Only the list scrolls, never the page, so the board stays in view on mobile.
4. **Click-to-Preview History:** Clicking on any historical move row temporarily displays the board position at that point in time for analysis, showing a floating warning badge: *"Browsing History — Click Resume to play"*.

---

### 7.2 Standard Game Record Serialization

The application must support loading and saving standard plain-text match records:

#### Metadata Header Tags
Every exported record begins with standard key-value headers:
```ini
[Event "Casual Match"]
[Site "Checkers .NET"]
[Date "2026.10.23"]
[Round "1"]
[White "Human"]
[Black "Computer (Flying Kings)"]
[Result "1-0"]
[Variant "International"]
[TimeControl "300+0"]
```

#### Notation Standards by Game:
* **Checkers / Draughts:** Portable Draughts Notation (`.pdn`) using square indices (`11-15` for moves, `29x18x4` for multi-jumps).
* **Gomoku / Renju:** Smart Game Format (`.sgf`) with `GM[11]` or standard algebraic coordinate sequences (`H8 G7 H7 J9`).
* **Chess:** Portable Game Notation (`.pgn`) using Standard Algebraic Notation (`1. e4 e5 2. Nf3 Nc6`).

---

## 8. Game Settings Architecture & Extension Pattern

Accessed via **Game $\rightarrow$ Settings...** (`F2`), the settings dialog standardizes match configuration into sequential, collapsible cards.

```mermaid
graph TD
    SettingsDialog["Modal Settings Dialog"]
    SettingsDialog --> Sec1["1. Rules &amp; Game Variant<br/>Primary variant radio selection"]
    SettingsDialog --> Sec2["2. AI Opponent Selection<br/>Advanced AI vs. Simple AI"]
    Sec2 -- "Simple AI selected" --> Simplified["Hide Advanced Search Controls"]
    Sec2 -- "Advanced AI selected" --> Sec3["3. Computer Thinking Limits<br/>Fixed Depth • Time per Move • Time per Game"]
    Sec2 -- "Advanced AI selected" --> Sec4["4. Memory &amp; Transposition Table<br/>Table size allocation (e.g. 16 MB to 256 MB)"]
    Sec2 -- "Advanced AI selected" --> Sec5["5. Opening Book Toggle<br/>Enable/Disable pre-calculated opening books"]
```

### 8.1 Section Breakdown

1. **Rule Variant Selection:** Radio group selecting the game variant (e.g., International vs English Checkers, Freestyle vs Renju Gomoku).
2. **AI Engine Selection:** Lets users toggle between full tree-search AI (Negamax/Alpha-Beta) and instant heuristic AI.
3. **Computer Thinking Limits (Three Standard Modes):**
   - **Fixed Depth ($1\dots 20$ plies):** Constant deterministic ply depth.
   - **Time per Move ($1\dots 60$ seconds):** Constant thinking time per turn with soft-limit safety margin ($\frac{2}{3}$).
   - **Time per Game ($1\dots 60$ minutes):** Total clock bank with dynamic move-time allocation based on game phase.
4. **Memory / Transposition Table:** Configures hash table capacity (e.g., $2^{20}$ to $2^{24}$ entries, $16\text{ MB}$ to $256\text{ MB}$).
5. **Opening Book Toggle:** Enables or disables lookup of pre-compiled opening databases.

> [!NOTE] Dynamic Visibility Pattern
> When a casual or instant AI engine (like Gomoku's *Simple AI*) is chosen, the dialog dynamically hides the thinking limit, transposition table, and opening book sections to avoid cluttering casual players with irrelevant tournament parameters.

---

## 9. Keyboard Shortcuts Master Map

Implementing clients must bind these standard shortcut keys across both Desktop (`KeyBinding`) and Web (`window.addEventListener('keydown')`):

| Key Binding | Scope | Action Description |
|---|---|---|
| **`Ctrl+N`** | Global | Start a New Game (with confirmation safeguard if in-progress) |
| **`Ctrl+O`** | Global | Open saved game record file |
| **`Ctrl+S`** | Global | Save current match record |
| **`Ctrl+Z`** | Board | **Undo** last move (2 plies vs computer; refunds clock time). Also the Undo button; not a menu item |
| **`Ctrl+Y`** | Board | **Redo** previously undone move |
| **`Ctrl+A`** | Global | Toggle **AI Analysis Panel** visibility |
| **`Ctrl+M`** | Engine | **Move Now:** Cancel AI search and force immediate move |
| **`F1`** | Global | Open **User guide** documentation chapter |
| **`Shift+F1`** | Global | Open **Rules & Architectural manual** |
| **`F2`** | Global | Open **Game Settings** dialog |
| **`Escape`** | Global | Close the active modal dialog, an open menu, or the mobile drawer |

---

## 10. Implementation Checklist for New Projects

Use this step-by-step engineering checklist when constructing an identical GUI for a new board game project:

### Phase 1: Presentation Core (`.App` / ViewModels)
- [ ] Create `MainViewModel` coordinating game modes (`vs Computer (White)`, `vs Computer (Black)`, `vs Human`), turn sequencing, and clock timers.
- [ ] Create `AnalysisViewModel` with properties: `Move`, `Depth`, `Score`, `BestMove`, `Nodes`, `Nps`, `ElapsedFormatted`.
- [ ] Create `SettingsViewModel` with reactive properties for variants, time modes, TT memory, and opening book toggles.
- [ ] Implement `IsGameModified` logic tracking Undo, Switch sides, mid-game setting edits, and timeout continuation.
- [ ] Implement `CancellationTokenSource` management for the "Move Now" operation.

### Phase 2: Shell Layout & Responsive Navigation
- [ ] Web: use GameShell's `ShellLayout` (menu bar, drawer, shortcuts, layout) instead of building the shell.
- [ ] Implement top menu bar with the 4 standard menus: `File`, `Game`, `Options`, `Help`.
- [ ] Implement mid-game confirmation dialog when triggering `New Game` or switching game modes mid-match.
- [ ] Implement responsive mobile header and slide-out navigation drawer with touch targets $\ge 44\text{ px}$ for screens $\le 768\text{ px}$.
- [ ] Bind global keyboard shortcuts (`Ctrl+N`, `Ctrl+O`, `Ctrl+S`, `Ctrl+Z`, `Ctrl+Y`, `Ctrl+A`, `Ctrl+M`, `F1`, `Shift+F1`, `F2`, `Escape`).

### Phase 3: Dual-Card Scoreboard & Actions
- [ ] Construct top status header with section title (`STATUS`), pill variant badge, and computer time setting.
- [ ] Implement dynamic crimson red `#E74C3C` styling on `"STATUS"` when `IsVsComputer && IsGameModified`.
- [ ] Build dual player cards with the 4-layer contract (Identity+Clock, Role, Metric 1, Metric 2).
- [ ] Add the sky-blue `#3498DB` active turn elevated border and glow.
- [ ] Add the Quick Action button bar (`Undo`, `Redo`, `Move Now`).
- [ ] Implement chess clock countdown ($250\text{ ms}$ timer) and the modal **Timeout Continuation Dialog**.

### Phase 4: Telemetry & Asynchronous Search Engine
- [ ] Define immutable `SearchAnalysis` telemetry snapshot record.
- [ ] Implement `IProgress<SearchAnalysis>` reporting in the search engine loop throttled to $\ge 150\text{ ms}$.
- [ ] Connect Web Worker (WebAssembly) or `Task.Run` (Desktop) to isolate search calculations from the UI thread.
- [ ] Add cooperative `await Task.Delay(1)` yielding for single-threaded environments.
- [ ] Insert a natural-feel delay ($250\text{ ms}$) for instantaneous book or forced moves.

### Phase 5: Move History & Serialization
- [ ] Implement two-column move history table (White plies / Black plies).
- [ ] Implement automatic scrolling to the latest move row upon move completion.
- [ ] Add click-to-preview history state inspection.
- [ ] Implement export and import parsers for standard game notation (`.pdn`, `.sgf`, `.pgn`).

---

## 11. Cross-Project Source Reference Map

### GameShell (shared web GUI, this repository)
| Component | Source File | Responsibility |
|---|---|---|
| **Shell layout** | `src/GameShell/ShellLayout.razor` | Desktop menu bar, mobile ☰ drawer, board and side-panel slots, keyboard shortcuts, menu alignment |
| **Menus** | `src/GameShell/Menus.cs` | `MenuDefinition` / `MenuEntry` (shortcut text, ✓ toggles, ● radio groups, separators) |
| **Status panel** | `src/GameShell/StatusPanel.razor`, `PlayerCard.razor` | STATUS header and badges, turn message, thinking bar, 4-layer player cards, Undo / Redo / Move Now |
| **Analysis panel** | `src/GameShell/AnalysisPanel.razor` | The 7 telemetry fields from an `IAnalysisView` adapter, rendered at most once per frame |
| **Move history** | `src/GameShell/MoveHistoryPanel.razor` | Paired rows, latest-move highlight, auto-scroll, `N moves` header |
| **Styles** | `src/GameShell/wwwroot/gameshell.css` | The shared look; board size through CSS variables |
| **Browser helpers** | `src/GameShell/wwwroot/gameshell.js` | Shortcut listener, menu alignment, history scrolling, text file open / download |
| **Dialogs** | `src/GameShell/Dialogs/ShellDialogs.cs`, `DialogHost.razor` | One modal dialog at a time: title, message, About picture, text box, buttons (first = Enter, last = Escape) and a body slot for the game's Settings form |
| **Documentation** | `src/GameShell/Docs/BrainDocs.cs`, `DocsViewer.razor`, `wwwroot/docs.css`, `wwwroot/docs.js` | The in-app `/docs` viewer for the game's `docs/brain` Markdown, with formulas (KaTeX) and diagrams (Mermaid) |

### Games (each a separate repository, GameShell as a git submodule)
| Game | Web project | Game-specific parts |
|---|---|---|
| **Checkers .NET** | `src/Checkers.Web` | Board (selection, captures, PDN numbers, board styles), Settings form, engine worker |
| **Connect 4** | `Connect4.Net/Connect4.Web` | Board (drop animation, board styles), Settings form, engine worker |
| **Gomoku .NET** | `src/Gomoku.Web` | SVG board (themes, Renju markers), Settings form, threaded engine host |
| **Stello** | `Stello.Net/Stello.Web` | Board (flip animation, board styles), Settings form, opening-book learning |

Each game also keeps its menus, its dialog service (the App layer's `IDialogService` on top of `ShellDialogs`) and its `/docs` route page. The platform-neutral presentation logic (`MainViewModel`, `AnalysisViewModel`, `SettingsViewModel`) stays in each game's `.App` project, shared with its WPF desktop version.

---

## 12. Document History & Maintenance

| Version | Date | Author | Summary of Changes |
|---|---|---|---|
| **1.1** | 2026-10-26 | Claus Jørgen Pedersen | Moved to the GameShell repository. Aligned with the four games: Undo / Redo as buttons and shortcuts only, ☰ and drawer on the right, Yes / No for questions, Analysis `Move` = move being searched, width-and-shape layout rules (§2.4), centred layout, vs Computer by default, board styles in the Options menu, paired history with `N moves` header and list-only auto-scroll, `F2` and `Escape` everywhere. |
| **1.0** | 2026-10-23 | Claus Jørgen Pedersen | Initial release of the General Game GUI Specification and Checklist. Standardized 4-menu hierarchy, mobile drawer ($\le 768\text{px}$), 4-layer player card contract, dynamic modified status (`#E74C3C`), 7-field AI analysis telemetry, and cross-project reference maps. |

