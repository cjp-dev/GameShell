using Microsoft.AspNetCore.Components;

namespace GameShell;

/// <summary>
/// Modal dialogs, shown one at a time by the <see cref="DialogHost"/> component (place it once in the layout).
/// Each game's dialog service builds the requests; while one is shown the page is inert
/// (<c>ShellLayout Inert="@(Dialogs.Current is not null)"</c>).
/// </summary>
public sealed class ShellDialogs
{
    private readonly Queue<DialogRequest> _queue = new();

    public event Action? Changed;

    /// <summary>The dialog on screen, or null.</summary>
    public DialogRequest? Current => _queue.TryPeek(out DialogRequest? request) ? request : null;

    /// <summary>Shows the dialog after any already waiting.</summary>
    /// <returns>The index of the button that closed it.</returns>
    public Task<int> ShowAsync(DialogRequest request)
    {
        _queue.Enqueue(request);
        Changed?.Invoke();
        return request.Result;
    }

    /// <summary>Closes the dialog if it is still the one shown.</summary>
    public void Close(DialogRequest request, int button)
    {
        if (Current != request)
        {
            return;
        }

        _queue.Dequeue();
        request.Complete(button);
        Changed?.Invoke();
    }
}

/// <summary>One dialog. The first button is the default (Enter), the last one cancels (Escape).</summary>
public sealed class DialogRequest
{
    private readonly TaskCompletionSource<int> _result = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public required string Title { get; init; }

    /// <summary>A bold line above the message, e.g. the version in the About box.</summary>
    public string? Heading { get; init; }

    /// <summary>The message; line breaks ("\n") are kept.</summary>
    public string? Message { get; init; }

    public IReadOnlyList<string> Paragraphs { get; init; } = [];

    /// <summary>A picture shown to the left of the text, with a caption below it.</summary>
    public string? Picture { get; init; }

    public string? PictureCaption { get; init; }

    /// <summary>The text in the input box; null for dialogs without one.</summary>
    public string? Text { get; set; }

    /// <summary>The game's own content below the text, e.g. its Settings form.</summary>
    public RenderFragment? Body { get; init; }

    public required IReadOnlyList<string> Buttons { get; init; }

    internal Task<int> Result => _result.Task;

    internal void Complete(int button) => _result.TrySetResult(button);
}
