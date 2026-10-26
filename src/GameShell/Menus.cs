using System.Windows.Input;

namespace GameShell;

/// <summary>A top-level menu (File, Game, Options, Help), shown in the desktop menu bar and the mobile drawer.</summary>
public sealed record MenuDefinition(string Title, IReadOnlyList<MenuEntry> Entries);

/// <summary>
/// One menu item. <paramref name="IsChecked"/> makes it a toggle (✓), or with <paramref name="Radio"/> one of a
/// group of choices (●). <paramref name="Shortcut"/> is only displayed; the key itself is bound in
/// <see cref="ShellLayout.Shortcuts"/>.
/// </summary>
public sealed record MenuEntry(
    string Text,
    ICommand Command,
    string? Shortcut = null,
    Func<bool>? IsChecked = null,
    bool Radio = false,
    bool SeparatorBefore = false);
