using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.AutoIdentifiers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace GameShell;

/// <summary>
/// The brain documentation (docs/brain, copied to wwwroot/content/brain by the game's build) as HTML. The README is
/// the front page, and its links to the chapter files give the chapters and their names.
/// Register it with <see cref="DocsServiceCollectionExtensions.AddBrainDocs"/>.
/// </summary>
public sealed partial class BrainDocs(HttpClient http, BrainDocsOptions options)
{
    private const string ContentPath = "content/brain/";
    private const string Route = "docs";

    // GitHub heading ids, so links such as "05-evaluation.md#threats" work; the docs have no raw HTML.
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private readonly Dictionary<string, Task<DocsPage>> _pages = [];
    private Task<string>? _readme;
    private Task<IReadOnlyList<DocsChapter>>? _chapters;

    public BrainDocsOptions Options => options;

    public static string PageRoute(string slug) => slug.Length == 0 ? Route : $"{Route}/{slug}";

    public Task<IReadOnlyList<DocsChapter>> GetChaptersAsync() => _chapters ??= LoadChaptersAsync();

    /// <param name="slug">A chapter's file name without ".md", or "" for the front page.</param>
    /// <returns>The page, or null if there is no such chapter.</returns>
    /// <exception cref="HttpRequestException">The page could not be downloaded.</exception>
    public async Task<DocsPage?> GetPageAsync(string slug)
    {
        // Only files listed in the README are fetched.
        if (slug.Length > 0 && !(await GetChaptersAsync()).Any(chapter => chapter.Slug == slug))
        {
            return null;
        }

        if (!_pages.TryGetValue(slug, out Task<DocsPage>? page) || page.IsFaulted)
        {
            page = LoadPageAsync(slug);
            _pages[slug] = page;
        }

        return await page;
    }

    private Task<string> ReadmeAsync()
    {
        if (_readme is null || _readme.IsFaulted)
        {
            _readme = http.GetStringAsync(ContentPath + "README.md");
        }

        return _readme;
    }

    private async Task<IReadOnlyList<DocsChapter>> LoadChaptersAsync()
    {
        try
        {
            MarkdownDocument readme = Markdown.Parse(await ReadmeAsync(), Pipeline);
            return readme.Descendants<LinkInline>()
                .Where(link => !link.IsImage && link.Url is not null && ChapterFile().IsMatch(link.Url))
                .Select(link => new DocsChapter(ChapterFile().Match(link.Url!).Groups[1].Value, PlainText(link)))
                .DistinctBy(chapter => chapter.Slug)
                .ToList();
        }
        catch
        {
            _chapters = null;
            throw;
        }
    }

    private async Task<DocsPage> LoadPageAsync(string slug)
    {
        string markdown = slug.Length == 0 ? await ReadmeAsync() : await http.GetStringAsync($"{ContentPath}{slug}.md");
        MarkdownDocument document = Markdown.Parse(markdown, Pipeline);
        RewriteLinks(document, slug);
        string title = document.Descendants<HeadingBlock>().FirstOrDefault(heading => heading.Level == 1)?.Inline is { } heading
            ? PlainText(heading)
            : options.Title;
        return new DocsPage(slug, title, document.ToHtml(Pipeline));
    }

    private void RewriteLinks(MarkdownDocument document, string slug)
    {
        foreach (LinkInline link in document.Descendants<LinkInline>())
        {
            if (link.Url is not { Length: > 0 } url)
            {
                continue;
            }

            link.Url = RewriteUrl(url, slug, link.IsImage, out bool external);
            if (external)
            {
                HtmlAttributes attributes = link.GetAttributes();
                attributes.AddPropertyIfNotExist("target", "_blank");
                attributes.AddPropertyIfNotExist("rel", "noopener");
            }
        }
    }

    private string RewriteUrl(string url, string slug, bool image, out bool external)
    {
        external = false;
        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absolute) && absolute.Scheme is "http" or "https" or "mailto")
        {
            external = true;
            return url;
        }

        if (url.StartsWith('#'))
        {
            return PageRoute(slug) + url;
        }

        int hash = url.IndexOf('#');
        string path = hash < 0 ? url : url[..hash];
        string fragment = hash < 0 ? "" : url[hash..];

        if (path.StartsWith("../", StringComparison.Ordinal))
        {
            external = true;
            // Links from docs/brain to other files in the repository open them on GitHub.
            return new Uri(options.RepositoryFolder, url).AbsoluteUri;
        }

        if (!image && ChapterFile().Match(path) is { Success: true } chapter)
        {
            return PageRoute(chapter.Groups[1].Value) + fragment;
        }

        if (!image && path == "README.md")
        {
            return PageRoute("") + fragment;
        }

        return ContentPath + url;
    }

    private static string PlainText(ContainerInline inline) => string.Concat(inline.Descendants<Inline>().Select(child => child switch
    {
        LiteralInline literal => literal.Content.ToString(),
        CodeInline code => code.Content,
        _ => "",
    }));

    // Chapters are "05-evaluation.md"; appendices "appendix-a-engine-comparison-report.md".
    [GeneratedRegex(@"^((?:\d\d|appendix-[a-z])[-_][a-z0-9_-]+)\.md$")]
    private static partial Regex ChapterFile();
}

/// <param name="Slug">The file name without ".md", e.g. "05-evaluation".</param>
/// <param name="Title">The name in the README's list of chapters, e.g. "Evaluation".</param>
public sealed record DocsChapter(string Slug, string Title)
{
    /// <summary>"05" for a chapter, "A" for an appendix.</summary>
    public string Number => Slug.StartsWith("appendix-", StringComparison.OrdinalIgnoreCase)
        ? Slug[9..10].ToUpperInvariant()
        : Slug[..2];
}

/// <param name="Title">The page's first heading.</param>
public sealed record DocsPage(string Slug, string Title, string Html);

/// <summary>What the documentation is called and where its repository is.</summary>
/// <param name="Title">The documentation's name, e.g. "The Connect 4 brain".</param>
/// <param name="GameName">The game's name for the "Play ..." link, e.g. "Connect 4".</param>
/// <param name="RepositoryFolder">The docs/brain folder on GitHub, for links to other files in the repository.</param>
public sealed record BrainDocsOptions(string Title, string GameName, Uri RepositoryFolder);
