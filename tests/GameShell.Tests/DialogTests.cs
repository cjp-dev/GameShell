using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace GameShell.Tests;

public sealed class ShellDialogsTests
{
    [Fact]
    public async Task Dialogs_AreShownOneAtATimeInOrder()
    {
        var dialogs = new ShellDialogs();
        var first = new DialogRequest { Title = "First", Buttons = ["OK"] };
        var second = new DialogRequest { Title = "Second", Buttons = ["Yes", "No"] };

        Task<int> firstResult = dialogs.ShowAsync(first);
        Task<int> secondResult = dialogs.ShowAsync(second);
        Assert.Same(first, dialogs.Current);

        dialogs.Close(first, 0);
        Assert.Equal(0, await firstResult);
        Assert.Same(second, dialogs.Current);

        dialogs.Close(second, 1);
        Assert.Equal(1, await secondResult);
        Assert.Null(dialogs.Current);
    }

    [Fact]
    public void Close_OfADialogNotShown_DoesNothing()
    {
        var dialogs = new ShellDialogs();
        var first = new DialogRequest { Title = "First", Buttons = ["OK"] };
        var second = new DialogRequest { Title = "Second", Buttons = ["OK"] };
        _ = dialogs.ShowAsync(first);
        Task<int> secondResult = dialogs.ShowAsync(second);

        dialogs.Close(second, 0);

        Assert.Same(first, dialogs.Current);
        Assert.False(secondResult.IsCompleted);
    }

    [Fact]
    public void Changed_IsRaisedWhenShownAndClosed()
    {
        var dialogs = new ShellDialogs();
        int changes = 0;
        dialogs.Changed += () => changes++;
        var request = new DialogRequest { Title = "T", Buttons = ["OK"] };

        _ = dialogs.ShowAsync(request);
        dialogs.Close(request, 0);

        Assert.Equal(2, changes);
    }
}

public sealed class DialogHostTests : BunitContext
{
    private readonly ShellDialogs _dialogs = new();

    public DialogHostTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(_dialogs);
    }

    [Fact]
    public void NoDialog_RendersNothing()
    {
        IRenderedComponent<DialogHost> host = Render<DialogHost>();

        Assert.Empty(host.FindAll(".dialog"));
    }

    [Fact]
    public async Task Message_KeepsItsTextAndTheFirstButtonIsTheDefault()
    {
        IRenderedComponent<DialogHost> host = Render<DialogHost>();
        Task<int> result = _dialogs.ShowAsync(new DialogRequest { Title = "Time Expired", Message = "You ran out of time!\n\nContinue?", Buttons = ["Yes", "No"] });

        host.WaitForAssertion(() => Assert.Equal("Time Expired", host.Find("#dialog-title").TextContent));
        Assert.Equal("You ran out of time!\n\nContinue?", host.Find(".dialog-message").TextContent);
        Assert.Equal(["Yes", "No"], host.FindAll(".dialog-buttons button").Select(b => b.TextContent));
        Assert.Contains("default", host.FindAll(".dialog-buttons button")[0].ClassList);

        host.FindAll(".dialog-buttons button")[1].Click();

        Assert.Equal(1, await result);
        Assert.Empty(host.FindAll(".dialog"));
    }

    [Fact]
    public async Task Escape_ChoosesTheLastButton()
    {
        IRenderedComponent<DialogHost> host = Render<DialogHost>();
        Task<int> result = _dialogs.ShowAsync(new DialogRequest { Title = "Settings", Buttons = ["OK", "Cancel"] });
        host.WaitForAssertion(() => Assert.NotNull(host.Find(".dialog")));

        host.Find(".dialog").KeyDown(key: "Escape");

        Assert.Equal(1, await result);
    }

    [Fact]
    public async Task TextBox_EnterChoosesTheFirstButtonAndKeepsTheText()
    {
        IRenderedComponent<DialogHost> host = Render<DialogHost>();
        var request = new DialogRequest { Title = "Save", Message = "Save the game as:", Text = "Game.txt", Buttons = ["OK", "Cancel"] };
        Task<int> result = _dialogs.ShowAsync(request);
        host.WaitForAssertion(() => Assert.NotNull(host.Find("input.dialog-text")));

        host.Find("input.dialog-text").Input("Mine.txt");
        host.Find("input.dialog-text").KeyDown(key: "Enter");

        Assert.Equal(0, await result);
        Assert.Equal("Mine.txt", request.Text);
    }

    [Fact]
    public void Body_ShowsTheGamesOwnContent()
    {
        IRenderedComponent<DialogHost> host = Render<DialogHost>();
        RenderFragment body = builder => builder.AddMarkupContent(0, "<fieldset class='settings'>Game settings</fieldset>");

        _ = _dialogs.ShowAsync(new DialogRequest { Title = "Settings", Body = body, Buttons = ["OK", "Cancel"] });

        host.WaitForAssertion(() => Assert.Equal("Game settings", host.Find(".dialog fieldset.settings").TextContent));
    }

    [Fact]
    public void Picture_UsesThePictureLayout()
    {
        IRenderedComponent<DialogHost> host = Render<DialogHost>();

        _ = _dialogs.ShowAsync(new DialogRequest
        {
            Title = "About",
            Heading = "Version 1.0",
            Paragraphs = ["One.", "Two."],
            Picture = "images/me.jpg",
            PictureCaption = "Me\nAuthor",
            Buttons = ["OK"],
        });

        host.WaitForAssertion(() => Assert.Contains("dialog-with-picture", host.Find(".dialog").ClassList));
        Assert.Equal("images/me.jpg", host.Find(".dialog-picture img").GetAttribute("src"));
        Assert.Equal("Version 1.0", host.Find(".dialog-heading").TextContent);
        Assert.Equal(["One.", "Two."], host.FindAll(".dialog-body p:not(.dialog-heading)").Select(p => p.TextContent));
    }
}
