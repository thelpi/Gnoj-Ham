using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Manages IA (decisions made by the CPU).
    /// </summary>
    /// <remarks>
    /// Some stuff we should improve:
    /// <list type="bullet">
    /// <item>Kokushi musou</item>
    /// <item>Open on chii in very specific cases</item>
    /// <item>To not discard doras</item>
    /// <item>Defense on opponents when opened hand.</item>
    /// <item>Rinshan kaihou even if an opponent is riichi</item>
    /// <item>Change playstyle depending on ranking</item>
    /// <item>To no defende when the hand is very good</item>
    /// </list>
    /// </remarks>
    public class IaManagerPivot
    {
        private RoundPivot _round;

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
        /// <returns>The tile to discard.</returns>
        public TilePivot DiscardDecision()
        {
            if (_round.IsHumanPlayer)
            {
                return null;
            }

            List<TilePivot> concealedTiles = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.ToList();

            List<TilePivot> discardableTiles = concealedTiles
                                                .Where(t => _round.CanDiscard(t))
                                                .Distinct()
                                                .ToList();

            if (discardableTiles.Count == 1)
            {
                return discardableTiles.First();
            }

            List<TilePivot> discards = _round.ExtractDiscardChoicesFromTenpai(_round.CurrentPlayerIndex);
            if (discards.Count > 0)
            {
                return discards.First();
            }

            List<TilePivot> deadTiles = _round.DeadTilesFromIndexPointOfView(_round.CurrentPlayerIndex).ToList();

            var tilesSafety = new Dictionary<TilePivot, List<TileSafety>>();

            bool oneRiichi = false;
            foreach (TilePivot tile in discardableTiles)
            {
                tilesSafety.Add(tile, new List<TileSafety>());
                foreach (int i in Enumerable.Range(0, 4).Where(i => i != _round.CurrentPlayerIndex))
                {
                    if (_round.IsRiichi(i))
                    {
                        oneRiichi = true;
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
                    else
                    {
                        tilesSafety[tile].Add(TileSafety.AverageOrUnknown);
                    }
                }
            }

            if (oneRiichi)
            {
                return tilesSafety.OrderBy(t => t.Value.Sum(s => (int)s)).First().Key;
            }

            // it's a bit flawed because it allows to discard a dora
            // in at least two cases we should not :
            // - when we have a choice between similar probabilities
            // - when dora was the precondition to open the hand in the first place
            var tilesGroup =
                concealedTiles
                    .GroupBy(t => t)
                    .OrderByDescending(t => t.Count())
                    .ThenByDescending(t =>
                    {
                        bool m2 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number - 2);
                        bool m1 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number - 1);
                        bool p1 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number + 1);
                        bool p2 = concealedTiles.Any(tb => tb.Family == t.Key.Family && tb.Number == t.Key.Number + 2);

                        return ((m1 ? 1 : 0) * 2 + (p1 ? 1 : 0) * 2 + (p2 ? 1 : 0) + (m2 ? 1 : 0));
                    })
                    .Reverse();

            return tilesGroup.First(tg => discardableTiles.Contains(tg.Key)).Key;
        }

        /// <summary>
        /// Checks if the current CPU player can make a riichi call, and computes the decision to do so.
        /// </summary>
        /// <returns>The tile to discard; <c>Null</c> if no decision made.</returns>
        public TilePivot RiichiDecision()
        {
            if (_round.IsHumanPlayer)
            {
                return null;
            }

            List<TilePivot> riichiTiles = _round.CanCallRiichi();
            if (riichiTiles.Count > 0)
            {
                return riichiTiles.First();
            }

            return null;
        }

        /// <summary>
        /// Checks if any CPU player can make a pon call, and computes its decision if any.
        /// </summary>
        /// <returns>The player index who makes the call; <c>-1</c> is none.</returns>
        public int PonDecision()
        {
            int opponentPlayerId = _round.OpponentsCanCallPon();
            if (opponentPlayerId > -1)
            {
                TilePivot tile = _round.GetDiscard(_round.PreviousPlayerIndex).Last();
                // Call the pon if :
                // - the hand is already open
                // - it's valuable (see "HandCanBeOpened")
                var opponentHand = _round.GetHand(opponentPlayerId);
                if (!opponentHand.IsConcealed || HandCanBeOpened(opponentPlayerId, tile, opponentHand))
                {
                    return opponentPlayerId;
                }
                opponentPlayerId = -1;
            }

            return opponentPlayerId;
        }

        /// <summary>
        /// Checks if the current CPU player can make a tsumo call, and computes the decision to do so.
        /// </summary>
        /// <param name="isKanCompensation"><c>True</c> if it's while a kan call is in progress; <c>False</c> otherwise.</param>
        /// <returns><c>True</c> if the decision is made; <c>False</c> otherwise.</returns>
        public bool TsumoDecision(bool isKanCompensation)
        {
            if (_round.IsHumanPlayer)
            {
                return false;
            }

            if (!_round.CanCallTsumo(isKanCompensation))
            {
                return false;
            }
            
            return true;
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
        public Tuple<int, TilePivot> KanDecision(bool checkConcealedOnly)
        {
            Tuple<int, List<TilePivot>> opponentPlayerIdWithTiles = _round.OpponentsCanCallKan(checkConcealedOnly);
            if (opponentPlayerIdWithTiles != null)
            {
                // riichi from opponents: no call
                // TODO: fix tenpai check
                if (_round.Riichis.Count > 0) //  && !_round.IsTenpai(opponentPlayerIdWithTiles.Item1)
                {
                    return null;
                }

                foreach (TilePivot tile in opponentPlayerIdWithTiles.Item2)
                {
                    // Call the kan if :
                    // - it's a concealed one
                    // - the hand is already open
                    // - it's valuable for "Yakuhai"
                    if (checkConcealedOnly
                        || !_round.GetHand(opponentPlayerIdWithTiles.Item1).IsConcealed
                        || tile.Family == FamilyPivot.Dragon
                        || (tile.Family == FamilyPivot.Wind
                            && (tile.Wind == _round.Game.GetPlayerCurrentWind(opponentPlayerIdWithTiles.Item1)
                                || tile.Wind == _round.Game.DominantWind)))
                    {
                        return new Tuple<int, TilePivot>(opponentPlayerIdWithTiles.Item1, tile);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks for CPU players who can make a ron call, and computes the decision to call it.
        /// </summary>
        /// <remarks>If any player, including human, calls ron, every players who can call ron will do.</remarks>
        /// <param name="ronCalled">Indicates if the human player has already made a ron call.</param>
        /// <returns>List of player index, other than human player, who decide to call ron.</returns>
        public List<int> RonDecision(bool ronCalled)
        {
            var callers = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                if (i != GamePivot.HUMAN_INDEX && _round.CanCallRon(i))
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
        public Tuple<TilePivot, bool> ChiiDecision()
        {
            if (_round.IsHumanPlayer)
            {
                return null;
            }

            Dictionary<TilePivot, bool> chiiTiles = _round.OpponentsCanCallChii();
            if (chiiTiles.Count > 0)
            {
                // Proceeds to chii if :
                // - The hand is already open (we assume it's open for a good reason)
                // - The sequence does not already exist in the end
                if (!_round.GetHand(_round.CurrentPlayerIndex).IsConcealed)
                {
                    Tuple<TilePivot, bool> tileChoice = null;
                    foreach (TilePivot tileKey in chiiTiles.Keys)
                    {
                        bool m2 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 2);
                        bool m1 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 1);
                        bool m0 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t == tileKey);
                        bool p1 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 1);
                        bool p2 = _round.GetHand(_round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 2);

                        if (!((m2 && m1 && m0) || (m1 && m0 && p1) || (m0 && p1 && p2)))
                        {
                            tileChoice = new Tuple<TilePivot, bool>(tileKey, chiiTiles[tileKey]);
                        }
                    }

                    return tileChoice;
                }
            }

            return null;
        }

        #endregion Public methods

        #region Private methods

        private bool IsDiscardedOrUnusable(TilePivot tile, int opponentPlayerIndex, List<TilePivot> deadtiles)
        {
            return _round.GetDiscard(opponentPlayerIndex).Contains(tile) || (
                tile.IsHonor
                && deadtiles.Count(t => t == tile) == 4
                && deadtiles.GroupBy(t => t).Any(t => t.Key != tile && t.Key.IsHonor && t.Count() == 4)
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
            if (tile.IsHonor)
            {
                return Enumerable.Empty<TilePivot>();
            }

            return _round
                .GetDiscard(opponentPlayerIndex)
                .Where(_ => _.Family == tile.Family && (_.Number == tile.Number + 3 || _.Number == tile.Number - 3));
        }

        // a family is over-represented in the opponent discard (at least 9 overal, and at least 3 distinct values)
        private bool IsMaxedFamilyInDiscard(TilePivot tile, int opponentPlayerIndex)
        {
            return !tile.IsHonor && _round.GetDiscard(opponentPlayerIndex).Count(_ => _.Family == tile.Family) >= 9
                && _round.GetDiscard(opponentPlayerIndex).Where(_ => _.Family == tile.Family).Distinct().Count() >= 3;
        }

        private bool HandCanBeOpened(int playerId, TilePivot tile, HandPivot hand)
        {
            var valuableWinds = new[] { _round.Game.GetPlayerCurrentWind(playerId), _round.Game.DominantWind };

            var canPonForYakuhai = IsDragonOrValuableWind(tile, valuableWinds);

            // >= 75% of the tile family
            var closeToChinitsu = hand.ConcealedTiles.Count(_ => _.Family == tile.Family) >= 11;

            // how much pair (or better) of valuable honors ?
            var valuableHonorPairs = hand.ConcealedTiles.GroupBy(_ => _)
                .Count(_ => _.Key.IsHonor && _.Count() >= 2 && IsDragonOrValuableWind(_.Key, valuableWinds));
            
            if (!canPonForYakuhai && valuableHonorPairs < 2 && !closeToChinitsu)
            {
                return false;
            }

            int dorasCount = hand.ConcealedTiles
                .Sum(t => _round.DoraIndicatorTiles
                    .Take(_round.VisibleDorasCount)
                    .Count(d => t.IsDoraNext(d)));
            int redDorasCount = hand.ConcealedTiles.Count(t => t.IsRedDora);

            var hasValuablePair = hand.ConcealedTiles.GroupBy(_ => _)
                .Any(_ => _.Count() >= 2 && _.Key != tile && IsDragonOrValuableWind(_.Key, valuableWinds));

            // >= 66% of one family or honor
            var closeToHonitsu = new[] { FamilyPivot.Bamboo, FamilyPivot.Caracter, FamilyPivot.Circle }
                .Any(f => hand.ConcealedTiles.Count(t => t.Family == f || t.IsHonor) > 9);

            return hasValuablePair
                || (dorasCount + redDorasCount) > 0
                || closeToHonitsu
                || valuableWinds[0] == WindPivot.East;
        }

        private static bool IsDragonOrValuableWind(TilePivot tile, WindPivot[] winds)
        {
            return tile.Family == FamilyPivot.Dragon
                || (tile.Family == FamilyPivot.Wind && winds.Contains(tile.Wind.Value));
        }

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
