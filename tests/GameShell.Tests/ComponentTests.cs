using CommunityToolkit.Mvvm.Input;

namespace GameShell.Tests;

public sealed class MoveHistoryTests : BunitContext
{
    public MoveHistoryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Pair_GroupsMovesIntoRounds()
    {
        IReadOnlyList<MoveHistoryRow> rows = MoveHistoryRow.Pair(["d3", "c5", "f6"]);

        Assert.Equal([new MoveHistoryRow(1, "d3", "c5"), new MoveHistoryRow(2, "f6")], rows);
    }

    [Fact]
    public void Empty_ShowsPlaceholderAndZeroMoves()
    {
        IRenderedComponent<MoveHistoryPanel> panel = Render<MoveHistoryPanel>(p => p.Add(x => x.Rows, []));

        Assert.Equal("MOVE HISTORY (0 moves)", panel.Find("h2").TextContent);
        Assert.Equal("No moves played yet.", panel.Find(".history-empty").TextContent);
    }

    [Fact]
    public void OddMoveCount_HighlightsFirstMoveOfLastRound()
    {
        IRenderedComponent<MoveHistoryPanel> panel = Render<MoveHistoryPanel>(p => p.Add(x => x.Rows, MoveHistoryRow.Pair(["4", "4", "3"])));

        Assert.Equal("MOVE HISTORY (3 moves)", panel.Find("h2").TextContent);
        Assert.Equal("3", Assert.Single(panel.FindAll(".move-ply.latest")).TextContent);
        Assert.Equal(["1.", "2."], panel.FindAll(".move-num").Select(e => e.TextContent));
    }

    [Fact]
    public void EvenMoveCount_HighlightsSecondMoveOfLastRound()
    {
        IRenderedComponent<MoveHistoryPanel> panel = Render<MoveHistoryPanel>(p => p.Add(x => x.Rows, MoveHistoryRow.Pair(["H8", "I9"])));

        Assert.Equal("MOVE HISTORY (2 moves)", panel.Find("h2").TextContent);
        Assert.Equal("I9", Assert.Single(panel.FindAll(".move-ply.latest")).TextContent);
    }

    [Fact]
    public void OneMove_UsesSingular()
    {
        IRenderedComponent<MoveHistoryPanel> panel = Render<MoveHistoryPanel>(p => p.Add(x => x.Rows, MoveHistoryRow.Pair(["22-18"])));

        Assert.Equal("MOVE HISTORY (1 move)", panel.Find("h2").TextContent);
    }

    [Fact]
    public void NewMove_ScrollsTheListToTheEnd()
    {
        IRenderedComponent<MoveHistoryPanel> panel = Render<MoveHistoryPanel>(p => p.Add(x => x.Rows, MoveHistoryRow.Pair(["a"])));
        int before = JSInterop.Invocations.Count(i => i.Identifier == "gameShell.scrollHistoryToEnd");

        panel.Render(p => p.Add(x => x.Rows, MoveHistoryRow.Pair(["a", "b"])));

        Assert.Equal(before + 1, JSInterop.Invocations.Count(i => i.Identifier == "gameShell.scrollHistoryToEnd"));
    }
}

public sealed class StatusPanelTests : BunitContext
{
    private static readonly PlayerInfo Red = new("Red", "red-dot", "Human", "3", "18 left", Clock: "4:58", IsActive: true);
    private static readonly PlayerInfo Yellow = new("Yellow", "yellow-dot", "Computer", "3", "18 left");

    private IRenderedComponent<StatusPanel> RenderPanel(string? settingBadge, RelayCommand? moveNow, bool modified = false) =>
        Render<StatusPanel>(p => p
            .Add(x => x.IsModified, modified)
            .Add(x => x.VariantBadge, "Standard")
            .Add(x => x.SettingBadge, settingBadge)
            .Add(x => x.StatusText, "Red's Turn — Drop a disc")
            .Add(x => x.First, Red)
            .Add(x => x.Second, Yellow)
            .Add(x => x.Undo, new RelayCommand(() => { }))
            .Add(x => x.Redo, new RelayCommand(() => { }))
            .Add(x => x.MoveNow, moveNow));

    [Fact]
    public void VsComputer_ShowsLimitBadgeAndMoveNow()
    {
        IRenderedComponent<StatusPanel> panel = RenderPanel("5s / move", new RelayCommand(() => { }));

        Assert.Equal("5s / move", panel.Find(".setting-text").TextContent);
        Assert.Contains(panel.FindAll("button"), b => b.TextContent == "Move Now");
    }

    [Fact]
    public void TwoPlayers_HidesLimitBadgeAndMoveNow()
    {
        IRenderedComponent<StatusPanel> panel = RenderPanel(null, null);

        Assert.Empty(panel.FindAll(".setting-text"));
        Assert.DoesNotContain(panel.FindAll("button"), b => b.TextContent == "Move Now");
    }

    [Fact]
    public void Modified_ShowsCrimsonStatus()
    {
        Assert.Empty(RenderPanel(null, null).FindAll("h2.status-modified"));
        Assert.Single(RenderPanel(null, null, modified: true).FindAll("h2.status-modified"));
    }

    [Fact]
    public void PlayerCards_FollowTheFourLayerContract()
    {
        IRenderedComponent<StatusPanel> panel = RenderPanel(null, null);
        var cards = panel.FindAll(".player-card");

        Assert.Equal(2, cards.Count);
        Assert.Contains("active-turn", cards[0].ClassList);
        Assert.DoesNotContain("active-turn", cards[1].ClassList);
        Assert.Equal("4:58", cards[0].QuerySelector(".player-clock")!.TextContent);
        Assert.Null(cards[1].QuerySelector(".player-clock"));
        Assert.Equal("Computer", cards[1].QuerySelector(".player-role")!.TextContent);
        Assert.Equal("18 left", cards[1].QuerySelector(".player-detail")!.TextContent);
    }
}

public sealed class AnalysisPanelTests : BunitContext
{
    private sealed class FakeAnalysis : IAnalysisView
    {
        public string Move { get; set; } = "";
        public string Depth { get; set; } = "";
        public string Score { get; set; } = "";
        public string BestMove { get; set; } = "";
        public string Nodes { get; set; } = "";
        public string Speed { get; set; } = "";
        public string Time { get; set; } = "";
        public event Action? Updated;
        public void Raise() => Updated?.Invoke();
    }

    [Fact]
    public void EmptyFields_ShowDashes()
    {
        IRenderedComponent<AnalysisPanel> panel = Render<AnalysisPanel>(p => p.Add(x => x.Analysis, new FakeAnalysis()));

        Assert.All(panel.FindAll("dd"), dd => Assert.Equal("-", dd.TextContent));
        Assert.Equal(7, panel.FindAll("dd").Count);
    }

    [Fact]
    public void Update_RendersNewValues()
    {
        var analysis = new FakeAnalysis();
        IRenderedComponent<AnalysisPanel> panel = Render<AnalysisPanel>(p => p.Add(x => x.Analysis, analysis));

        analysis.Depth = "9 plies";
        analysis.Raise();

        panel.WaitForAssertion(() => Assert.Equal("9 plies", panel.FindAll("dd")[1].TextContent));
    }
}

public sealed class ShellLayoutTests : BunitContext
{
    private int _newGames;
    private bool _soundOn = true;
    private readonly RelayCommand _newGame;
    private readonly RelayCommand _disabled = new(() => { }, () => false);

    public ShellLayoutTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _newGame = new RelayCommand(() => _newGames++);
    }

    private IReadOnlyList<MenuDefinition> Menus() =>
    [
        new("File", [new("New Game", _newGame, "Ctrl+N"), new("Save", _disabled, "Ctrl+S")]),
        new("Options", [new("Sound", new RelayCommand(() => _soundOn = !_soundOn), IsChecked: () => _soundOn),
                        new("Blue Board", new RelayCommand(() => { }), IsChecked: () => true, Radio: true, SeparatorBefore: true)]),
    ];

    private IRenderedComponent<ShellLayout> RenderShell(bool inert = false, Action? onCommand = null) =>
        Render<ShellLayout>(p => p
            .Add(x => x.Brand, "Connect 4")
            .Add(x => x.Menus, Menus())
            .Add(x => x.Inert, inert)
            .Add(x => x.Shortcuts, new Dictionary<string, Func<Task>> { ["n"] = () => ShellCommands.RunAsync(_newGame) })
            .Add(x => x.OnCommand, onCommand ?? (() => { }))
            .Add(x => x.BoardArea, "<div id='board'></div>")
            .Add(x => x.SidePanels, "<div id='panels'></div>"));

    [Fact]
    public void RendersBrandBoardAndPanels()
    {
        IRenderedComponent<ShellLayout> shell = RenderShell();

        Assert.Equal("Connect 4", shell.Find(".app-brand").TextContent);
        Assert.NotNull(shell.Find(".board-area #board"));
        Assert.NotNull(shell.Find(".side-panels #panels"));
    }

    [Fact]
    public void RegistersTheGamesShortcutLetters()
    {
        RenderShell();

        JSRuntimeInvocation call = Assert.Single(JSInterop.Invocations, i => i.Identifier == "gameShell.registerShortcuts");
        Assert.Equal("n", call.Arguments[1]);
    }

    [Fact]
    public void MenuItem_RunsCommandClosesMenuAndRaisesOnCommand()
    {
        int commands = 0;
        IRenderedComponent<ShellLayout> shell = RenderShell(onCommand: () => commands++);

        shell.FindAll(".menu-title")[0].Click();
        shell.FindAll(".menu-items button")[0].Click();

        Assert.Equal(1, _newGames);
        Assert.Equal(1, commands);
        Assert.Empty(shell.FindAll(".menu-items"));
    }

    [Fact]
    public void MenuItem_ShowsShortcutDisabledStateAndChecks()
    {
        IRenderedComponent<ShellLayout> shell = RenderShell();

        shell.FindAll(".menu-title")[0].Click();
        var file = shell.FindAll(".menu-items button");
        Assert.Equal("Ctrl+N", file[0].QuerySelector(".shortcut")!.TextContent);
        Assert.True(file[1].HasAttribute("disabled"));

        shell.FindAll(".menu-title")[1].Click();
        var options = shell.FindAll(".menu-items button");
        Assert.Equal("✓", options[0].QuerySelector(".check")!.TextContent);
        Assert.Equal("●", options[1].QuerySelector(".check")!.TextContent);
        Assert.Contains("separator-before", options[1].ClassList);
    }

    [Fact]
    public async Task Escape_ClosesOpenMenuAndDrawer()
    {
        IRenderedComponent<ShellLayout> shell = RenderShell();
        shell.FindAll(".menu-title")[0].Click();
        shell.Find(".hamburger-btn").Click();

        await shell.InvokeAsync(() => shell.Instance.OnShortcut("escape"));

        Assert.Empty(shell.FindAll(".menu-items"));
        Assert.Empty(shell.FindAll(".mobile-drawer.open"));
    }

    [Fact]
    public async Task Shortcut_RunsTheBoundAction()
    {
        int commands = 0;
        IRenderedComponent<ShellLayout> shell = RenderShell(onCommand: () => commands++);

        await shell.InvokeAsync(() => shell.Instance.OnShortcut("n"));
        await shell.InvokeAsync(() => shell.Instance.OnShortcut("q"));

        Assert.Equal(1, _newGames);
        Assert.Equal(1, commands);
    }

    [Fact]
    public async Task Inert_IgnoresShortcuts()
    {
        IRenderedComponent<ShellLayout> shell = RenderShell(inert: true);

        await shell.InvokeAsync(() => shell.Instance.OnShortcut("n"));

        Assert.Equal(0, _newGames);
        Assert.True(shell.Find(".app").HasAttribute("inert"));
    }

    [Fact]
    public void Drawer_ActionClosesIt()
    {
        IRenderedComponent<ShellLayout> shell = RenderShell();
        shell.Find(".hamburger-btn").Click();
        Assert.Single(shell.FindAll(".mobile-drawer.open"));

        shell.FindAll(".mobile-menu-item")[0].Click();

        Assert.Equal(1, _newGames);
        Assert.Empty(shell.FindAll(".mobile-drawer.open"));
    }
}
