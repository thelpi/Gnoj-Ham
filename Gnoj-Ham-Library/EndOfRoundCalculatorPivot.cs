using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Computes the outcome and score of a round once it's over: a win (tsumo, ron, possibly by several
/// simultaneous winners), a ryuukyoku (exhaustive draw), or an abortive draw.
/// </summary>
internal class EndOfRoundCalculatorPivot
{
    private readonly RoundPivot _round;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="round">The round to compute the end-of-round outcome for.</param>
    internal EndOfRoundCalculatorPivot(RoundPivot round)
    {
        _round = round;
    }

    /// <summary>
    /// Computes the end-of-round outcome.
    /// </summary>
    /// <param name="ronPlayerIndex">The player index on who the call has been made; <c>Null</c> if tsumo or ryuukyoku.</param>
    /// <returns>An instance of <see cref="EndOfRoundInformationsPivot"/>.</returns>
    internal EndOfRoundInformationsPivot Compute(PlayerIndices? ronPlayerIndex)
    {
        var turnWind = false;
        var ryuukyoku = true;
        var displayUraDoraTiles = false;
        var isAbortiveDraw = _round.IsSuuchaRiichi || _round.IsKyuushuKyuuhai || _round.IsSuukaikan || _round.IsSuufonRenda;

        var winners = isAbortiveDraw
            ? new List<PlayerIndices>()
            : Enum.GetValues<PlayerIndices>().Where(i => _round.GetHand(i).IsComplete).ToList();

        if (winners.Count == 0 && !isAbortiveDraw && _round.Game.Ruleset.UseNagashiMangan)
        {
            var iNagashiList = CheckForNagashiMangan();
            if (iNagashiList.Count > 0)
            {
                winners.AddRange(iNagashiList);
            }
        }

        var playerInfos = new List<EndOfRoundInformationsPivot.PlayerInformationsPivot>(4);

        // Abortive draw (e.g. suucha riichi): no tenpai/noten payment, dealer always repeats (renchan),
        // riichi sticks carry over (handled by the caller through the "Ryuukyoku" flag), honba still
        // increments (also handled by the caller).
        if (isAbortiveDraw)
        {
            // turnWind stays false: the dealer is not affected by an abortive draw.
        }
        // Ryuukyoku (no winner).
        else if (winners.Count == 0)
        {
            var tenpaiPlayersIndex = Enum.GetValues<PlayerIndices>().Where(i => _round.IsTenpai(i, null)).ToList();
            var notTenpaiPlayersIndex = Enum.GetValues<PlayerIndices>().Except(tenpaiPlayersIndex).ToList();

            // Wind turns if East is not tenpai.
            turnWind = notTenpaiPlayersIndex.Any(tpi => _round.Game.GetPlayerCurrentWind(tpi) == Winds.East);

            var (tenpai, nonTenpai) = ScoreTools.GetRyuukyokuPoints(tenpaiPlayersIndex.Count);

            tenpaiPlayersIndex.ForEach(i =>
                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot
                {
                    Index = i,
                    IsCpu = _round.Game.IsCpu(i),
                    Hand = _round.GetHand(i),
                    PointsGain = tenpai,
                    HandPointsGain = tenpai
                }));
            notTenpaiPlayersIndex.ForEach(i =>
                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot { Index = i, IsCpu = _round.Game.IsCpu(i), PointsGain = nonTenpai }));
        }
        else
        {
            turnWind = !winners.Any(w => _round.Game.GetPlayerCurrentWind(w) == Winds.East);

            // Why this list ? Consider the following :
            // - Player 1 and 2 ron on player 3
            // - Player 1 is "Daisangen"
            // - Player 2 is "Daisuushii"
            // - Player 4 is liable for player 1
            // - Player 1 is liable for player 2
            // In that case :
            // - P1 pays half of P2 yakuman
            // - P4 pays half of P1 yakuman
            // - P3 pays half of both yakuman
            var liablePlayersLost = new Dictionary<PlayerIndices, int>();

            // These two are negative points.
            var eastOrLoserLostCumul = 0;
            var notEastLostCumul = 0;
            var honbaPoints = ScoreTools.GetHonbaPoints(_round.Game.HonbaCountBeforeScoring);

            foreach (var pIndex in winners)
            {
                var phand = _round.GetHand(pIndex);

                // In case of multiple simultaneous ron, only the winner closest to the discarder
                // (going around the table starting right after them) collects the honba and the
                // pending riichi sticks; the other winner(s) get their hand value only. Doesn't apply
                // to simultaneous nagashi mangan winners: there's no discarder to compare against.
                var isClosestWinnerOnMultipleRon = !ronPlayerIndex.HasValue || winners.Count <= 1
                    || IsClosestWinnerToDiscarder(pIndex, ronPlayerIndex.Value, winners);

                var winnerHonba = isClosestWinnerOnMultipleRon ? honbaPoints : 0;

                // In case of ron, fix the "LatestPick" property of the winning hand
                if (ronPlayerIndex.HasValue)
                {
                    phand.SetFromRon(_round.GetDiscard(ronPlayerIndex.Value)[^1]);
                }

                PlayerIndices? liablePlayerId = null;
                if (phand.Yakus!.Contains(YakuPivot.Daisangen)
                    && phand.DeclaredCombinations.Count(c => c.Family == Families.Dragon) == 3
                    && phand.DeclaredCombinations.Last(c => c.Family == Families.Dragon).StolenFrom.HasValue)
                {
                    liablePlayerId = _round.Game.GetPlayerIndexByCurrentWind(phand.DeclaredCombinations.Last(c => c.Family == Families.Dragon).StolenFrom!.Value);
                }
                else if (phand.Yakus!.Contains(YakuPivot.Daisuushii)
                    && phand.DeclaredCombinations.Count(c => c.Family == Families.Wind) == 4
                    && phand.DeclaredCombinations.Last(c => c.Family == Families.Wind).StolenFrom.HasValue)
                {
                    liablePlayerId = _round.Game.GetPlayerIndexByCurrentWind(phand.DeclaredCombinations.Last(c => c.Family == Families.Wind).StolenFrom!.Value);
                }

                var isRiichi = phand.Yakus!.Contains(YakuPivot.Riichi) || phand.Yakus!.Contains(YakuPivot.DaburuRiichi);

                var dorasCount = phand.AllTiles.Sum(_round.GetDoraCount);
                var uraDorasCount = isRiichi ? phand.AllTiles.Sum(_round.GetUraDoraCount) : 0;
                var redDorasCount = phand.AllTiles.Count(t => t.IsRedDora);

                if (isRiichi)
                {
                    displayUraDoraTiles = true;
                }

                var fanCount = ScoreTools.GetFanCount(phand.Yakus!, phand.IsConcealed, _round.Game.Ruleset.UseMultipleYakumans, _round.Game.Ruleset.UseKazoeYakuman, _round.Game.Ruleset.UseDoubleYakuman, dorasCount, uraDorasCount, redDorasCount);
                var fuCount = ScoreTools.GetFuCount(phand, !ronPlayerIndex.HasValue, _round.Game.DominantWind, _round.Game.GetPlayerCurrentWind(pIndex));

                if (liablePlayerId.HasValue)
                {
                    if (!ronPlayerIndex.HasValue)
                    {
                        // Sekinin barai : transforms the tsumo into a ron on the liable player.
                        ronPlayerIndex = liablePlayerId;
                        liablePlayerId = null;
                    }
                    else if (ronPlayerIndex.Value == liablePlayerId.Value)
                    {
                        // Sekinin barai : no consequence as ron player and liable player are the same.
                        liablePlayerId = null;
                    }
                }

                var (east, notEast) = ScoreTools.GetPoints(fanCount, fuCount, !ronPlayerIndex.HasValue, _round.Game.GetPlayerCurrentWind(pIndex));

                var basePoints = east + (notEast * 2);

                var riichiPart = isClosestWinnerOnMultipleRon ? _round.Game.PendingRiichiCount * ScoreTools.RIICHI_COST : 0;

                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot
                {
                    Index = pIndex,
                    IsCpu = _round.Game.IsCpu(pIndex),
                    FanCount = fanCount,
                    FuCount = fuCount,
                    Hand = phand,
                    PointsGain = basePoints + riichiPart + winnerHonba,
                    DoraCount = dorasCount,
                    UraDoraCount = uraDorasCount,
                    RedDoraCount = redDorasCount,
                    HandPointsGain = basePoints
                });

                notEastLostCumul -= notEast;

                // If there's is a liable player (only in a case of ron on other player than the one liable)...
                if (liablePlayerId.HasValue)
                {
                    liablePlayersLost.TryAdd(liablePlayerId.Value, 0);
                    // ... he takes half of the points from the ron player for this hand.
                    eastOrLoserLostCumul -= east / 2;
                    liablePlayersLost[liablePlayerId.Value] -= east / 2;
                }
                else
                {
                    // Otherwise, the ron player takes all.
                    eastOrLoserLostCumul -= east;
                }
            }

            // Note : "liablePlayersLost" is empty in case of tsumo transformed into ron. A liable
            // player can never be "ronPlayerIndex" itself (that case is nulled out earlier, as "no
            // consequence"), so "eastOrLoserLostCumul" - accumulated only from non-liable shares -
            // already reflects exactly what the discarder owes; no further adjustment is needed here.
            foreach (var liablePlayerId in liablePlayersLost.Keys)
            {
                if (playerInfos.Any(pi => pi.Index == liablePlayerId))
                {
                    playerInfos.First(pi => pi.Index == liablePlayerId).AddPoints(liablePlayersLost[liablePlayerId]);
                }
                else
                {
                    // Only the discarder pays honba: a liable player's share is never affected by it.
                    playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot
                    {
                        Index = liablePlayerId,
                        IsCpu = _round.Game.IsCpu(liablePlayerId),
                        PointsGain = liablePlayersLost[liablePlayerId]
                    });
                }
            }

            if (ronPlayerIndex.HasValue)
            {
                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot
                {
                    Index = ronPlayerIndex.Value,
                    IsCpu = _round.Game.IsCpu(ronPlayerIndex.Value),
                    PointsGain = eastOrLoserLostCumul - honbaPoints
                });
            }
            else
            {
                foreach (var pIndex in Enum.GetValues<PlayerIndices>())
                {
                    if (!winners.Contains(pIndex))
                    {
                        playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot
                        {
                            Index = pIndex,
                            IsCpu = _round.Game.IsCpu(pIndex),
                            PointsGain = (_round.Game.GetPlayerCurrentWind(pIndex) == Winds.East ? eastOrLoserLostCumul : notEastLostCumul) - (honbaPoints / 3)
                        });
                    }
                }
            }

            ryuukyoku = false;
        }

        foreach (var p in playerInfos)
        {
            _round.Game.Players[(int)p.Index].AddPoints(p.PointsGain);
        }

        return new EndOfRoundInformationsPivot(ryuukyoku, turnWind, displayUraDoraTiles, playerInfos, _round.Game.HonbaCountBeforeScoring,
            _round.Game.PendingRiichiCount, _round.DoraIndicatorTiles, _round.UraDoraIndicatorTiles, _round.VisibleDorasCount);
    }

    // Checks for players with nagashi mangan.
    private List<PlayerIndices> CheckForNagashiMangan()
    {
        var playerIndexList = new List<PlayerIndices>(4);

        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            var fullTerminalsOrHonors = _round.GetDiscard(i).All(t => t.IsHonorOrTerminal);
            var noPlayerStealing = _round.GetHand(i).IsConcealed;
            var noOpponentStealing = !Enum.GetValues<PlayerIndices>()
                .Where(j => j != i)
                .Any(j => _round.GetHand(j).DeclaredCombinations.Any(c => c.StolenFrom == _round.Game.GetPlayerCurrentWind(i)));
            if (fullTerminalsOrHonors && noPlayerStealing && noOpponentStealing)
            {
                _round.GetHand(i).SetYakus(new WinContextPivot());
                playerIndexList.Add(i);
            }
        }

        if (playerIndexList.Count > 1)
        {
            // Atama-hane: only the winner closest to the dealer (turn order East -> South -> West -> North) is kept.
            playerIndexList = new List<PlayerIndices> { playerIndexList.OrderBy(i => (int)_round.Game.GetPlayerCurrentWind(i)).First() };
        }

        return playerIndexList;
    }

    /// <summary>
    /// In case of multiple simultaneous ron, only one winner collects the honba and the pending
    /// riichi sticks: the one seated closest to the discarder, going around the table starting right
    /// after them.
    /// </summary>
    /// <param name="candidate">The candidate winner.</param>
    /// <param name="discarder">The discarder (<c>ronPlayerIndex</c>).</param>
    /// <param name="winners">Every winner of this ron.</param>
    /// <returns><c>True</c> if <paramref name="candidate"/> is the one who collects them.</returns>
    private static bool IsClosestWinnerToDiscarder(PlayerIndices candidate, PlayerIndices discarder, IReadOnlyList<PlayerIndices> winners)
    {
        for (var i = 1; i <= 3; i++)
        {
            var closerWinner = discarder.RelativePlayerIndex(i);
            if (winners.Contains(closerWinner))
            {
                return closerWinner == candidate;
            }
        }

        return false;
    }
}
