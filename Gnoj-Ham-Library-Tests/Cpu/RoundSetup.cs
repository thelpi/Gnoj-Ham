using System.Reflection;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library_Tests;

/// <summary>
/// Puts a round in the situation a CPU test needs, without playing up to it.
/// </summary>
internal static class RoundSetup
{
    internal static RoundPivot NewRound(int seed)
        => new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed)).Round;

    /// <summary>
    /// Gives a player exactly these concealed tiles.
    /// </summary>
    internal static void SetHand(RoundPivot round, PlayerIndices playerIndex, List<TilePivot> concealedTiles)
    {
        var hands = (List<HandPivot>)typeof(RoundPivot)
            .GetField("_hands", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        hands[(int)playerIndex] = new HandPivot(concealedTiles);
    }

    /// <summary>
    /// Makes the current player one who has to discard.
    /// </summary>
    internal static void SetWaitForDiscard(RoundPivot round, bool value)
    {
        typeof(RoundPivot)
            .GetField("_waitForDiscard", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(round, value);
    }

    /// <summary>
    /// Marks a player as riichi (the "dangerous opponent" signal a CPU reacts to) without playing through
    /// the actual riichi call.
    /// </summary>
    internal static void SetRiichi(RoundPivot round, PlayerIndices playerIndex)
    {
        var riichis = (List<RiichiPivot?>)typeof(RoundPivot)
            .GetField("_riichis", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        var ranks = Enum.GetValues<PlayerIndices>().Where(p => p != playerIndex).ToDictionary(p => p, _ => 0);
        riichis[(int)playerIndex] = new RiichiPivot(0, false, TilePivot.GetCompleteSet(false)[0], ranks);
    }

    /// <summary>
    /// Adds a tile to the discard pile of a player, which doesn't change anything else.
    /// </summary>
    internal static void AddToDiscard(RoundPivot round, PlayerIndices playerIndex, TilePivot tile)
    {
        var discardHistory = typeof(RoundPivot)
            .GetField("_discardHistory", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(round)!;
        var discards = (List<List<TilePivot>>)typeof(DiscardHistoryPivot)
            .GetField("_discards", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(discardHistory)!;
        discards[(int)playerIndex].Add(tile);
    }
}
