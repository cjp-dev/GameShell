using Microsoft.Extensions.DependencyInjection;

namespace GameShell;

public static class DocsServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="BrainDocs"/> for the <see cref="DocsViewer"/>. Needs an <see cref="HttpClient"/> whose
    /// base address is the app's, as in the Blazor WebAssembly template.
    /// </summary>
    /// <param name="repositoryFolder">The docs/brain folder on GitHub, e.g. "https://github.com/cjp-dev/Connect-4/blob/main/docs/brain/".</param>
    public static IServiceCollection AddBrainDocs(this IServiceCollection services, string title, string gameName, string repositoryFolder) =>
        services.AddSingleton(provider => new BrainDocs(
            provider.GetRequiredService<HttpClient>(),
            new BrainDocsOptions(title, gameName, new Uri(repositoryFolder))));
}
