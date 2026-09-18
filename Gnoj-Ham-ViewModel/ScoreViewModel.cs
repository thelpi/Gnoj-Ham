using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// The result of a round: doras, winners' hands and the resulting ranking.
/// </summary>
public sealed class ScoreViewModel
{
    private const int DoraIndicatorsCount = 5;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="players">The four players.</param>
    /// <param name="info">Information about the end of the round.</param>
    public ScoreViewModel(IReadOnlyList<PlayerPivot> players, EndOfRoundInformationsPivot info)
    {
        HonbaCount = info.HonbaCount;
        PendingRiichiCount = info.PendingRiichiCount;
        DoraTiles = BuildIndicatorTiles(info.DoraTiles, info.DoraVisibleCount);
        UraDoraTiles = BuildIndicatorTiles(info.UraDoraTiles, info.UraDoraVisibleCount);

        Winners = info.PlayersInfo
            .Where(p => p.HandPointsGain > 0)
            .Select(p => new WinnerScoreViewModel(players[(int)p.Index].Name, p))
            .ToList();

        Ranking = players
            .Select((p, i) => (player: p, index: i))
            .OrderByDescending(x => x.player.CurrentGamePoints)
            .Select(x => new ScoreRankingRowViewModel(
                x.player.Name,
                new GainViewModel(info.GetPlayerPointsGain((PlayerIndices)x.index)),
                x.player.CurrentGamePoints))
            .ToList();
    }

    /// <summary>
    /// The honba count.
    /// </summary>
    public int HonbaCount { get; }

    /// <summary>
    /// The number of riichi sticks still on the table.
    /// </summary>
    public int PendingRiichiCount { get; }

    /// <summary>
    /// The dora indicators, in display order; those not yet revealed are face down.
    /// </summary>
    public IReadOnlyList<TileViewModel> DoraTiles { get; }

    /// <summary>
    /// The ura-dora indicators; those not revealed are face down.
    /// </summary>
    public IReadOnlyList<TileViewModel> UraDoraTiles { get; }

    /// <summary>
    /// The winners' hands; empty on a draw.
    /// </summary>
    public IReadOnlyList<WinnerScoreViewModel> Winners { get; }

    /// <summary>
    /// The players, best first.
    /// </summary>
    public IReadOnlyList<ScoreRankingRowViewModel> Ranking { get; }

    // Indicators are laid out from the last one down to the first: the first (the one revealed
    // initially) ends up rightmost, and the ones past the visible count are face down.
    private static IReadOnlyList<TileViewModel> BuildIndicatorTiles(IReadOnlyList<TilePivot> tiles, int visibleCount)
    {
        var result = new List<TileViewModel>(DoraIndicatorsCount);
        for (var i = DoraIndicatorsCount - 1; i >= 0; i--)
        {
            result.Add(new TileViewModel(tiles[i], isConcealed: visibleCount <= i));
        }

        return result;
    }
}
