namespace GameShell;

/// <summary>
/// One player card in the status panel (the spec's 4-layer contract): glyph, name and clock; role;
/// a large primary metric; a secondary detail line.
/// </summary>
/// <param name="GlyphClass">CSS class for the colour dot, defined by the game (e.g. <c>red-dot</c>).</param>
/// <param name="Clock">The remaining time as M:SS, or null when the game has no clock.</param>
public sealed record PlayerInfo(
    string Name,
    string GlyphClass,
    string Role,
    string Metric,
    string Detail,
    string? Clock = null,
    bool IsActive = false);

/// <summary>One round in the move history: the first player's move and, once played, the second player's.</summary>
public sealed record MoveHistoryRow(int Number, string First, string? Second = null)
{
    /// <summary>Pairs a list of single moves into rounds.</summary>
    public static IReadOnlyList<MoveHistoryRow> Pair(IReadOnlyList<string> moves)
    {
        var rows = new List<MoveHistoryRow>((moves.Count + 1) / 2);
        for (int i = 0; i < moves.Count; i += 2)
        {
            rows.Add(new MoveHistoryRow(i / 2 + 1, moves[i], i + 1 < moves.Count ? moves[i + 1] : null));
        }

        return rows;
    }
}

/// <summary>
/// The seven telemetry fields of the analysis panel. Each game's web project adapts its own analysis
/// view model, which lives in the platform-neutral App layer, to this interface.
/// </summary>
public interface IAnalysisView
{
    string Move { get; }
    string Depth { get; }
    string Score { get; }
    string BestMove { get; }
    string Nodes { get; }
    string Speed { get; }
    string Time { get; }

    /// <summary>Raised when any field changes; the panel re-renders itself, at most once per frame.</summary>
    event Action? Updated;
}
