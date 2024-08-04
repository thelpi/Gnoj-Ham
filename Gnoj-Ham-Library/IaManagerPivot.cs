using System;
using System.Collections.Generic;
using System.Linq;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library
{
    /// <summary>
    /// Manages IA (decisions made by the CPU).
    /// </summary>
    /// <remarks>
    /// Some stuff we should improve:
    /// <list type="bullet">
    /// <item>Kokushi musou</item>
    /// <item>Open on chii in very specific cases</item>
    /// <item>Rinshan kaihou even if an opponent is riichi</item>
    /// <item>Change playstyle depending on ranking</item>
    /// <item>To no defende when the hand is very good</item>
    /// </list>
    /// </remarks>
    public class IaManagerPivot
    {
        private readonly RoundPivot _round;

        // indicates an honiisou (or chiniisou) in progress
        private Families? _itsuFamily;

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="round">The <see cref="_round"/> value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="round"/> is <c>Null</c>.</exception>
        internal IaManagerPivot(RoundPivot round)
        {
            _round = round ?? throw new ArgumentNullException(nameof(round));
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Computes the discard decision of the current CPU player.
        /// </summary>
        /// <param name="tenpaiPotentialDiscards">Results of the call to <see cref="RoundPivot.CanCallRiichi"/> that has been made before; set <c>null</c> to force a recompute.</param>
        /// <returns>The tile to discard.</returns>
        public TilePivot DiscardDecision(IReadOnlyList<TilePivot>? tenpaiPotentialDiscards)
        {
            var concealedTiles = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles;

            var discardableTiles = concealedTiles
                .Where(t => _round.CanDiscard(t))
                .Distinct()
                .ToList();

            // there's no choice
            if (discardableTiles.Count == 1)
            {
                return discardableTiles.First();
            }

            var deadTiles = _round.DeadTilesFromIndexPointOfView(_round.CurrentPlayerIndex);

            var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(discardableTiles, deadTiles);

            // tenpai: let's go anyway...
            tenpaiPotentialDiscards = tenpaiPotentialDiscards ?? _round.ExtractDiscardChoicesFromTenpai(_round.CurrentPlayerIndex);
            if (tenpaiPotentialDiscards.Count > 0)
            {
                return GetBestDiscardFromList(tenpaiPotentialDiscards, tilesSafety, stopCurrentHand);
            }

            if (stopCurrentHand)
            {
                return tilesSafety.First().tile;
            }

            var tilesGroup =
                concealedTiles
                    .GroupBy(t => t)
                    // keeps brelan/square
                    .OrderByDescending(t =>
                    {
                        var count = t.Count();
                        return count > 2 ? count : 0;
                    })
                    // keeps pair of valuable honor
                    .ThenByDescending(t => t.Count() > 1
                        && (t.Key.Family == Families.Dragon
                            || t.Key.Wind == _round.Game.DominantWind
                            || t.Key.Wind == _round.Game.GetPlayerCurrentWind(_round.CurrentPlayerIndex))
                        && deadTiles.Count(_ => _ == t.Key) < 2)
                    // keeps group of following numbers
                    .ThenByDescending(t =>
                    {
                        var m2 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number - 2);
                        var m1 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number - 1);
                        var p1 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number + 1);
                        var p2 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number + 2);

                        return ((m1 ? 1 : 0) * 2) + ((p1 ? 1 : 0) * 2) + (p2 ? 1 : 0) + (m2 ? 1 : 0);
                    })
                    // doras are better than "not dora"
                    .ThenByDescending(t => _round.GetDoraCount(t.Key) + (t.Key.IsRedDora ? 1 : 0))
                    // keeps pair
                    .ThenByDescending(t => t.Count())
                    // all things being equal, wind are the best to discard
                    .ThenBy(t => t.Key.Family == Families.Wind)
                    // all things being equal, the closer to side the better
                    .ThenBy(t => t.Key.DistanceToMiddle(false))
                    .Reverse();

            return tilesGroup.First(tg => discardableTiles.Contains(tg.Key)).Key;
        }

        /// <summary>
        /// Checks if the current CPU player can make a riichi call, and computes the decision to do so.
        /// </summary>
        /// <returns>A tuple with the tile to discard (<c>Null</c> if no decision made) and all the tiles 'discardable'.</returns>
        public (TilePivot? choice, IReadOnlyList<TilePivot> potentials) RiichiDecision()
        {
            var riichiTiles = _round.CanCallRiichi();

            if (riichiTiles.Count < 2)
                return (riichiTiles.Count > 0 ? riichiTiles.First() : null, riichiTiles);

            var deadTiles = _round.DeadTilesFromIndexPointOfView(_round.CurrentPlayerIndex);

            var (tilesSafety, stopCurrentHand) = ComputeTilesSafety(riichiTiles, deadTiles);

            var tileSelected = GetBestDiscardFromList(riichiTiles, tilesSafety, stopCurrentHand);

            return (tileSelected, riichiTiles);
        }

        /// <summary>
        /// Checks if any CPU player can make a pon call, and computes its decision if any.
        /// </summary>
        /// <returns>The player index who makes the call; <c>-1</c> is none.</returns>
        internal int PonDecision()
        {
            var opponentPlayerId = _round.OpponentsCanCallPon();
            if (opponentPlayerId > -1)
            {
                opponentPlayerId = PonDecisionInternal(opponentPlayerId);
            }

            return opponentPlayerId;
        }

        /// <summary>
        /// Checks if the current CPU player can make a tsumo call, and computes the decision to do so.
        /// </summary>
        /// <param name="isKanCompensation"><c>True</c> if it's while a kan call is in progress; <c>False</c> otherwise.</param>
        /// <returns><c>True</c> if the decision is made; <c>False</c> otherwise.</returns>
        internal bool TsumoDecision(bool isKanCompensation)
        {
            return _round.CanCallTsumo(isKanCompensation);
        }

        /// <summary>
        /// Checks for CPU players who can make a kan call, and computes the decision to call it.
        /// </summary>
        /// <param name="checkConcealedOnly">
        /// <c>True</c> to check only concealed kan (or from a previous pon);
        /// <c>False</c> to check the opposite.
        /// </param>
        /// <returns>
        /// If the decision is made, a tuple :
        /// - The first item indicates the player index who call the kan.
        /// - The second item indicates the base tile of the kand (several choices are possible).
        /// <c>Null</c> otherwise.
        /// </returns>
        internal Tuple<int, TilePivot> KanDecision(bool checkConcealedOnly)
        {
            var opponentPlayerIdWithTiles = _round.OpponentsCanCallKan(checkConcealedOnly);
            return opponentPlayerIdWithTiles != null
                ? KanDecisionInternal(opponentPlayerIdWithTiles.Item1, opponentPlayerIdWithTiles.Item2, checkConcealedOnly)
                : null;
        }

        /// <summary>
        /// Checks for CPU players who can make a ron call, and computes the decision to call it.
        /// </summary>
        /// <remarks>If any player, including human, calls ron, every players who can call ron will do.</remarks>
        /// <param name="ronCalled">Indicates if the human player has already made a ron call.</param>
        /// <returns>List of player index, other than human player, who decide to call ron.</returns>
        public IReadOnlyList<int> RonDecision(bool ronCalled)
        {
            var callers = new List<int>(4);

            for (var i = 0; i < 4; i++)
            {
                if ((i != GamePivot.HUMAN_INDEX || _round.Game.CpuVs) && _round.CanCallRon(i))
                {
                    if (ronCalled || callers.Count > 0)
                    {
                        callers.Add(i);
                    }
                    else
                    {
                        // Same code as the "if" statement : expected for now.
                        callers.Add(i);
                    }
                }
            }

            return callers;
        }

        /// <summary>
        /// Checks if the current CPU player can make a chii call, and computes the decision to do so.
        /// </summary>
        /// <returns>
        /// If the decision is made, a tuple :
        /// - The first item indicates the first tile to use, in the sequence order, in the concealed hand of the player.
        /// - The second item indicates if this tile represents the first number in the sequence (<c>True</c>) or the second (<c>False</c>).
        /// <c>Null</c> otherwise.
        /// </returns>
        internal Tuple<TilePivot, bool> ChiiDecision()
        {
            var chiiTiles = _round.OpponentsCanCallChii();
            return chiiTiles.Count > 0 ? ChiiDecisionInternal(chiiTiles) : null;
        }

        /// <summary>
        /// Computes an advice for the human player to call a Kan or not; assumes the Kan is possible.
        /// </summary>
        /// <param name="kanPossibilities">The first tile of every possible Kan at the moment.</param>
        /// <param name="concealedKan"><c>True</c> if the context is a concealed Kan.</param>
        /// <returns><c>True</c> if Kan is advised.</returns>
        public bool KanDecisionAdvice(IReadOnlyList<TilePivot> kanPossibilities, bool concealedKan)
            => KanDecisionInternal(GamePivot.HUMAN_INDEX, kanPossibilities, concealedKan) != null;

        /// <summary>
        /// Computes an advice for the human player to call a Pon or not; assumes the Pon is possible.
        /// </summary>
        /// <returns><c>True</c> if Pon is advised.</returns>
        public bool PonDecisionAdvice()
            => PonDecisionInternal(GamePivot.HUMAN_INDEX) > -1;

        /// <summary>
        /// Computes an advice for the human player to call a Chii or not; assumes the Chii is possible.
        /// </summary>
        /// <param name="chiiPossibilities">See the result of the method <see cref="RoundPivot.CanCallChii"/>.</param>
        /// <returns><c>True</c> if Chii is advised.</returns>
        public bool ChiiDecisionAdvice(Dictionary<TilePivot, bool> chiiPossibilities)
            => ChiiDecisionInternal(chiiPossibilities) != null;

        #endregion Public methods

        #region Private methods

        private TilePivot GetBestDiscardFromList(IReadOnlyList<TilePivot> tenpaiPotentialDiscards,
            IReadOnlyList<(TilePivot tile, int unsafePoints)> tilesSafety, bool playsSafe)
        {
            if (tenpaiPotentialDiscards.Count == 1)
                return tenpaiPotentialDiscards.First();

            int safetyFunc(TilePivot t) => tilesSafety.First(_ => _.tile == t).unsafePoints;
            int dorasFunc(TilePivot t) => _round.GetDoraCount(t) + (t.IsRedDora ? 1 : 0);

            var orderedTiles = playsSafe
                ? tenpaiPotentialDiscards
                    .OrderBy(safetyFunc)
                    .ThenBy(dorasFunc)
                : tenpaiPotentialDiscards
                    .OrderBy(dorasFunc)
                    .ThenBy(safetyFunc);

            return orderedTiles
                .ThenByDescending(t => t.DistanceToMiddle(true))
                .ThenBy(t => t)
                .First();
        }

        private int PonDecisionInternal(int playerIndex)
        {
            var tile = _round.GetDiscard(_round.PreviousPlayerIndex).Last();

            var hand = _round.GetHand(playerIndex);

            var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(playerIndex);

            // if the hand is already opened and no opponent tenpai: takes it
            if (!hand.IsConcealed && tenpaiOpponentIndexes.Count == 0 && (!_itsuFamily.HasValue || _itsuFamily == tile.Family))
            {
                return playerIndex;
            }

            var valuableWinds = new[] { _round.Game.GetPlayerCurrentWind(playerIndex), _round.Game.DominantWind };

            var canPonForYakuhai = IsDragonOrValuableWind(tile, valuableWinds);

            // >= 75% of the tile family
            var closeToChinitsu = hand.ConcealedTiles.Count(_ => _.Family == tile.Family) >= 11;

            // how much pair (or better) of valuable honors ?
            var valuableHonorPairs = hand.ConcealedTiles.GroupBy(_ => _)
                .Count(_ => _.Key.IsHonor && _.Count() >= 2 && IsDragonOrValuableWind(_.Key, valuableWinds));

            if (!canPonForYakuhai && valuableHonorPairs < 2 && !closeToChinitsu)
            {
                return -1;
            }

            var dorasCount = hand.ConcealedTiles
                .Sum(t => _round.GetDoraCount(t));
            var redDorasCount = hand.ConcealedTiles.Count(t => t.IsRedDora);

            var hasValuablePair = hand.ConcealedTiles.GroupBy(_ => _)
                .Any(_ => _.Count() >= 2 && _.Key != tile && IsDragonOrValuableWind(_.Key, valuableWinds));

            // >= 66% of one family or honor
            var closeToHonitsuFamily = new Families?[] { Families.Bamboo, Families.Caracter, Families.Circle }
                .FirstOrDefault(f => hand.ConcealedTiles.Count(t => t.Family == f || t.IsHonor) > 9);

            _itsuFamily = _itsuFamily ?? closeToHonitsuFamily;

            return hasValuablePair
                || (dorasCount + redDorasCount) > 0
                || closeToHonitsuFamily.HasValue
                || valuableWinds[0] == Winds.East
                ? playerIndex
                : -1;
        }

        private Tuple<int, TilePivot> KanDecisionInternal(int playerId, IReadOnlyList<TilePivot> kanPossibilities, bool concealed)
        {
            // Rinshan kaihou possibility: call
            var tileToRemove = _round.GetHand(playerId).IsFullHand
                ? kanPossibilities.First()
                : null;

            var meIsTenpai = _round.IsTenpai(playerId, tileToRemove);
            if (!meIsTenpai && GetTenpaiOpponentIndexes(playerId).Count > 0)
            {
                // riichi or opponents close to win: no call
                return null;
            }

            foreach (var tile in kanPossibilities)
            {
                // Call the kan if :
                // - it's a concealed one
                // - the hand is already open
                if (concealed
                    || (!_round.GetHand(playerId).IsConcealed && (!_itsuFamily.HasValue || _itsuFamily == tile.Family)))
                {
                    return new Tuple<int, TilePivot>(playerId, tile);
                }
            }

            return null;
        }

        private Tuple<TilePivot, bool> ChiiDecisionInternal(Dictionary<TilePivot, bool> chiiTiles)
        {
            // Proceeds to chii if :
            // - The hand is already open (we assume it's open for a good reason)
            // - The sequence does not already exist in the end
            // - Nobody is tenpai
            // - if a honiisou or better is in progress, tile to chii should be of this family
            var tenpaiOppenentIndexes = GetTenpaiOpponentIndexes(_round.CurrentPlayerIndex);

            if (_round.GetHand(_round.CurrentPlayerIndex).IsConcealed
                || tenpaiOppenentIndexes.Count > 0
                || (_itsuFamily.HasValue && _itsuFamily != chiiTiles.Keys.First().Family))
            {
                return null;
            }

            Tuple<TilePivot, bool> tileChoice = null;
            foreach (var tileKey in chiiTiles.Keys)
            {
                var m2 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 2);
                var m1 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 1);
                var m0 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t == tileKey);
                var p1 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 1);
                var p2 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 2);

                if (!((m2 && m1 && m0) || (m1 && m0 && p1) || (m0 && p1 && p2)))
                {
                    tileChoice = new Tuple<TilePivot, bool>(tileKey, chiiTiles[tileKey]);
                }
            }

            return tileChoice;
        }

        private bool IsDiscardedOrUnusable(TilePivot tile, int opponentPlayerIndex, IReadOnlyList<TilePivot> deadtiles)
        {
            return _round.GetDiscard(opponentPlayerIndex).Contains(tile) || (
                tile.IsHonor
                && deadtiles.Count(t => t == tile) == 3
                // this last line is to avoid giving Kokushi musou tile
                && deadtiles.GroupBy(t => t).Any(t => t.Key != tile && t.Key.IsHonor && t.Count() > 2)
            );
        }

        // suji on the middle (least safe)
        private bool IsInsiderSuji(TilePivot tile, int opponentPlayerIndex)
        {
            return GetSujisFromDiscard(tile, opponentPlayerIndex)
                .Any(_ => !new[] { 4, 5, 6 }.Contains(_.Number));
        }

        // suji on the edge (safer)
        private bool IsOutsiderSuji(TilePivot tile, int opponentPlayerIndex)
        {
            return GetSujisFromDiscard(tile, opponentPlayerIndex)
                .Any(_ => new[] { 4, 5, 6 }.Contains(_.Number));
        }

        // suji on both side
        private bool IsDoubleInsiderSuji(TilePivot tile, int opponentPlayerIndex)
        {
            return GetSujisFromDiscard(tile, opponentPlayerIndex).Distinct().Count() > 1;
        }

        private IEnumerable<TilePivot> GetSujisFromDiscard(TilePivot tile, int opponentPlayerIndex)
        {
            return tile.IsHonor
                ? Enumerable.Empty<TilePivot>()
                : _round
                .GetDiscard(opponentPlayerIndex)
                .Where(_ => _.Family == tile.Family && (_.Number == tile.Number + 3 || _.Number == tile.Number - 3));
        }

        // a family is over-represented in the opponent discard (at least 9 overal, and at least 3 distinct values)
        private bool IsMaxedFamilyInDiscard(TilePivot tile, int opponentPlayerIndex)
        {
            return !tile.IsHonor && _round.GetDiscard(opponentPlayerIndex).Count(_ => _.Family == tile.Family) >= 9
                && _round.GetDiscard(opponentPlayerIndex).Where(_ => _.Family == tile.Family).Distinct().Count() >= 3;
        }

        private static bool IsDragonOrValuableWind(TilePivot tile, Winds[] winds)
        {
            return tile.Family == Families.Dragon
                || (tile.Family == Families.Wind && winds.Contains(tile.Wind.Value));
        }

        private (IReadOnlyList<(TilePivot tile, int unsafePoints)> bestToWorstChoices, bool shouldGiveUp) ComputeTilesSafety(IReadOnlyList<TilePivot> discardableTiles, IReadOnlyList<TilePivot> deadTiles)
        {
            var tilesSafety = new Dictionary<TilePivot, List<TileSafety>>();

            // computes once
            var tenpaiOpponentIndexes = GetTenpaiOpponentIndexes(_round.CurrentPlayerIndex);

            var stopCurrentHand = false;
            foreach (var tile in discardableTiles)
            {
                tilesSafety.Add(tile, new List<TileSafety>(3));
                foreach (var i in Enumerable.Range(0, 4).Where(i => i != _round.CurrentPlayerIndex))
                {
                    // stop the building of the hand is opponent is riichi or has 3 or more combinations visible
                    if (tenpaiOpponentIndexes.Contains(i))
                    {
                        stopCurrentHand = true;
                        if (IsDiscardedOrUnusable(tile, i, deadTiles))
                        {
                            tilesSafety[tile].Add(TileSafety.Safe);
                        }
                        else if (IsOutsiderSuji(tile, i) || IsDoubleInsiderSuji(tile, i) || IsMaxedFamilyInDiscard(tile, i))
                        {
                            tilesSafety[tile].Add(TileSafety.QuiteSafe);
                        }
                        else if (IsInsiderSuji(tile, i) || tile.IsHonorOrTerminal)
                        {
                            tilesSafety[tile].Add(TileSafety.QuiteUnsafe);
                        }
                        else
                        {
                            tilesSafety[tile].Add(TileSafety.Unsafe);
                        }
                    }
                    else if (_round.GetDoraCount(tile) > 0)
                    {
                        // dora are bit unsafe at first, then unsafe at middle game
                        tilesSafety[tile].Add(_round.WallTiles.Count > 42 ? TileSafety.QuiteUnsafe : TileSafety.Unsafe);
                    }
                    else
                    {
                        tilesSafety[tile].Add(TileSafety.AverageOrUnknown);
                    }
                }
            }

            return (tilesSafety.OrderBy(t => t.Value.Sum(s => (int)s)).Select(t => (t.Key, t.Value.Sum(s => (int)s))).ToList(), stopCurrentHand);
        }

        private IReadOnlyList<int> GetTenpaiOpponentIndexes(int playerIndex) => Enumerable.Range(0, 4).Where(i => i != playerIndex && PlayerIsCloseToWin(i)).ToList();

        private bool PlayerIsCloseToWin(int i) => _round.IsRiichi(i) || _round.GetHand(i).DeclaredCombinations.Count > 2;

        #endregion Private methods

        /// <summary>
        /// Enumeration of level of safety for tile discard.
        /// </summary>
        internal enum TileSafety
        {
            /// <summary>
            /// The tile is 100% safe.
            /// </summary>
            Safe,
            /// <summary>
            /// The tile seems safe.
            /// </summary>
            QuiteSafe,
            /// <summary>
            /// Unable to detect the safety level of the tile.
            /// </summary>
            AverageOrUnknown,
            /// <summary>
            /// The tile seems unsafe.
            /// </summary>
            QuiteUnsafe,
            /// <summary>
            /// The tile is unsafe.
            /// </summary>
            Unsafe
        }
    }
}
