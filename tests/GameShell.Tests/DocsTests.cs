using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace GameShell.Tests;

/// <summary>Serves files from a dictionary; anything else is a 404.</summary>
internal sealed class FakeFiles(Dictionary<string, string> files) : HttpMessageHandler
{
    public List<string> Requested { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string path = request.RequestUri!.AbsolutePath.TrimStart('/');
        Requested.Add(path);
        return Task.FromResult(files.TryGetValue(path, out string? text)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(text) }
            : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

internal static class DocsFixture
{
    public const string Readme = """
        # The Test brain

        Read [Overview](01-overview.md), then [Evaluation](05-evaluation.md) and the
        [engine comparison](appendix-a-engine-comparison-report.md).
        """;

    public const string Evaluation = """
        # 05 – Evaluation

        See [threats](#threats), the [overview](01-overview.md), the [code](../../src/Evaluator.cs),
        the [README](README.md), a ![diagram](images/eval.png) and [GitHub](https://github.com).

        ## Threats
        """;

    public static Dictionary<string, string> Files() => new()
    {
        ["content/brain/README.md"] = Readme,
        ["content/brain/01-overview.md"] = "# 01 – Overview\n\nHello.",
        ["content/brain/05-evaluation.md"] = Evaluation,
        ["content/brain/appendix-a-engine-comparison-report.md"] = "No heading here.",
    };

    public static BrainDocs Create(FakeFiles files) => new(
        new HttpClient(files) { BaseAddress = new Uri("https://game.test/") },
        new BrainDocsOptions("The Test brain", "Test", new Uri("https://github.com/cjp-dev/Test/blob/main/docs/brain/")));
}

public sealed class BrainDocsTests
{
    private readonly FakeFiles _files = new(DocsFixture.Files());

    [Fact]
    public async Task Chapters_ComeFromTheReadmeLinksInOrder()
    {
        IReadOnlyList<DocsChapter> chapters = await DocsFixture.Create(_files).GetChaptersAsync();

        Assert.Equal(["01-overview", "05-evaluation", "appendix-a-engine-comparison-report"], chapters.Select(c => c.Slug));
        Assert.Equal(["Overview", "Evaluation", "engine comparison"], chapters.Select(c => c.Title));
        Assert.Equal(["01", "05", "A"], chapters.Select(c => c.Number));
    }

    [Fact]
    public async Task Page_RewritesLinksForTheViewer()
    {
        DocsPage page = (await DocsFixture.Create(_files).GetPageAsync("05-evaluation"))!;

        Assert.Equal("05 – Evaluation", page.Title);
        Assert.Contains("href=\"docs/05-evaluation#threats\"", page.Html);
        Assert.Contains("href=\"docs/01-overview\"", page.Html);
        Assert.Contains("href=\"https://github.com/cjp-dev/Test/blob/main/src/Evaluator.cs\"", page.Html);
        Assert.Contains("href=\"docs\"", page.Html);
        Assert.Contains("src=\"content/brain/images/eval.png\"", page.Html);
        Assert.Contains("href=\"https://github.com\" target=\"_blank\" rel=\"noopener\"", page.Html);
        Assert.Contains("id=\"threats\"", page.Html);
    }

    [Fact]
    public async Task Formula_InAHeading_KeepsItsBraces()
    {
        var files = DocsFixture.Files();
        files["content/brain/01-overview.md"] = "# 01 – Overview\n\n### Natural Feel Delay ($250\\text{ ms}$)\n\nText {#not-an-id}";
        DocsPage page = (await DocsFixture.Create(new FakeFiles(files)).GetPageAsync("01-overview"))!;

        Assert.Contains("250\\text{ ms}", page.Html);
        Assert.DoesNotContain(" ms=\"\"", page.Html);
        Assert.Contains("{#not-an-id}", page.Html);
    }

    [Fact]
    public async Task Page_WithoutHeading_IsNamedAfterTheDocs()
    {
        DocsPage page = (await DocsFixture.Create(_files).GetPageAsync("appendix-a-engine-comparison-report"))!;

        Assert.Equal("The Test brain", page.Title);
    }

    [Fact]
    public async Task UnknownChapter_IsNotFetched()
    {
        Assert.Null(await DocsFixture.Create(_files).GetPageAsync("99-secret"));
        Assert.DoesNotContain("content/brain/99-secret.md", _files.Requested);
    }

    [Fact]
    public void PageRoute_IsUnderDocs()
    {
        Assert.Equal("docs", BrainDocs.PageRoute(""));
        Assert.Equal("docs/05-evaluation", BrainDocs.PageRoute("05-evaluation"));
    }

    [Fact]
    public void AddBrainDocs_RegistersTheOptions()
    {
        ServiceProvider services = new ServiceCollection()
            .AddSingleton(new HttpClient(_files) { BaseAddress = new Uri("https://game.test/") })
            .AddBrainDocs("The Test brain", "Test", "https://github.com/cjp-dev/Test/blob/main/docs/brain/")
            .BuildServiceProvider();

        BrainDocs docs = services.GetRequiredService<BrainDocs>();
        Assert.Equal("The Test brain", docs.Options.Title);
        Assert.Equal("Test", docs.Options.GameName);
    }
}

public sealed class DocsViewerTests : BunitContext
{
    public DocsViewerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        JSInterop.SetupModule("./_content/GameShell/docs.js").Setup<string>("render", _ => true).SetResult("");
    }

    private void UseFiles(Dictionary<string, string> files) => Services.AddSingleton(DocsFixture.Create(new FakeFiles(files)));

    [Fact]
    public void Chapter_ShowsHeaderContentsAndPager()
    {
        UseFiles(DocsFixture.Files());

        IRenderedComponent<DocsViewer> viewer = Render<DocsViewer>(p => p.Add(x => x.Slug, "05-evaluation"));

        viewer.WaitForAssertion(() => Assert.NotNull(viewer.Find("article.markdown-body h1")));
        Assert.Equal("The Test brain", viewer.Find(".docs-home").TextContent);
        Assert.Equal("Play Test", viewer.Find(".docs-play").TextContent);
        Assert.Equal(4, viewer.FindAll(".docs-toc li").Count);
        Assert.Equal("page", viewer.FindAll(".docs-toc a")[2].GetAttribute("aria-current"));
        Assert.Equal("← Overview", viewer.Find(".docs-pager .previous").TextContent);
        Assert.Equal("engine comparison →", viewer.Find(".docs-pager .next").TextContent);
    }

    [Fact]
    public void UnknownChapter_ShowsNotFound()
    {
        UseFiles(DocsFixture.Files());

        IRenderedComponent<DocsViewer> viewer = Render<DocsViewer>(p => p.Add(x => x.Slug, "99-secret"));

        viewer.WaitForAssertion(() => Assert.Equal("Page not found", viewer.Find("h1").TextContent));
    }

    [Fact]
    public void MissingFiles_ShowAnError()
    {
        UseFiles([]);

        IRenderedComponent<DocsViewer> viewer = Render<DocsViewer>();

        viewer.WaitForAssertion(() => Assert.Contains("could not be loaded", viewer.Find(".docs-notice").TextContent));
    }
}
