using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Manages IA (decisions made by the CPU).
    /// </summary>
    public class IaManagerPivot
    {
        #region Embedded properties

        /// <summary>
        /// Current round.
        /// </summary>
        public RoundPivot Round { get; private set; }

        #endregion Embedded properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="round">The <see cref="Round"/> value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="round"/> is <c>Null</c>.</exception>
        internal IaManagerPivot(RoundPivot round)
        {
            Round = round ?? throw new ArgumentNullException(nameof(round));
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Computes the discard decision of the current CPU player.
        /// </summary>
        /// <returns>The tile to discard.</returns>
        public TilePivot DiscardDecision()
        {
            if (Round.IsHumanPlayer)
            {
                return null;
            }

            List<TilePivot> concealedTiles = Round.Hands.ElementAt(Round.CurrentPlayerIndex).ConcealedTiles.ToList();

            List<TilePivot> discardableTiles = concealedTiles
                                                .Where(t => Round.CanDiscard(t))
                                                .Distinct()
                                                .ToList();

            if (discardableTiles.Count == 1)
            {
                return discardableTiles.First();
            }

            List<TilePivot> discards = Round.ExtractDiscardChoicesFromTenpai(Round.CurrentPlayerIndex);
            if (discards.Count > 0)
            {
                return discards.First();
            }

            List<TilePivot> deadtiles = Round.DeadTilesFromIndexPointOfView(Round.CurrentPlayerIndex).ToList();

            var tilesSafety = new Dictionary<TilePivot, List<TileSafety>>();

            bool oneRiichi = false;
            foreach (TilePivot tile in discardableTiles)
            {
                tilesSafety.Add(tile, new List<TileSafety>());
                foreach (int i in Enumerable.Range(0, 4).Where(i => i != Round.CurrentPlayerIndex))
                {
                    if (Round.IsRiichi(i))
                    {
                        oneRiichi = true;
                        if (IsSafeForPlayer(tile, i, deadtiles))
                        {
                            tilesSafety[tile].Add(TileSafety.Safe);
                        }
                        else
                        {
                            tilesSafety[tile].Add(TileSafety.Unsafe);
                        }
                    }
                    else
                    {
                        tilesSafety[tile].Add(TileSafety.Unknown);
                    }
                }
            }

            if (oneRiichi)
            {
                return tilesSafety.OrderBy(t => t.Value.Sum(s => (int)s)).First().Key;
            }

            IEnumerable<IGrouping<TilePivot, TilePivot>> tilesGroup =
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
            if (Round.IsHumanPlayer)
            {
                return null;
            }

            List<TilePivot> riichiTiles = Round.CanCallRiichi();
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
            int opponentPlayerId = Round.OpponentsCanCallPon();
            if (opponentPlayerId > -1)
            {
                TilePivot tile = Round.Discards.ElementAt(Round.PreviousPlayerIndex).Last();
                // Call the pon if :
                // - the hand is already open
                // - it's valuable for "Yakuhai"
                if (!Round.Hands.ElementAt(opponentPlayerId).IsConcealed
                    || tile.Family == FamilyPivot.Dragon
                    || (tile.Family == FamilyPivot.Wind
                        && (tile.Wind == Round.Game.GetPlayerCurrentWind(opponentPlayerId)
                            || tile.Wind == Round.Game.DominantWind)))
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
            if (Round.IsHumanPlayer)
            {
                return false;
            }

            if (!Round.CanCallTsumo(isKanCompensation))
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
            Tuple<int, List<TilePivot>> opponentPlayerIdWithTiles = Round.OpponentsCanCallKan(checkConcealedOnly);
            if (opponentPlayerIdWithTiles != null)
            {
                foreach (TilePivot tile in opponentPlayerIdWithTiles.Item2)
                {
                    // Call the kan if :
                    // - it's a concealed one
                    // - the hand is already open
                    // - it's valuable for "Yakuhai"
                    if (checkConcealedOnly
                        || !Round.Hands.ElementAt(opponentPlayerIdWithTiles.Item1).IsConcealed
                        || tile.Family == FamilyPivot.Dragon
                        || (tile.Family == FamilyPivot.Wind
                            && (tile.Wind == Round.Game.GetPlayerCurrentWind(opponentPlayerIdWithTiles.Item1)
                                || tile.Wind == Round.Game.DominantWind)))
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
                if (i != GamePivot.HUMAN_INDEX && Round.CanCallRon(i))
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
            if (Round.IsHumanPlayer)
            {
                return null;
            }

            Dictionary<TilePivot, bool> chiiTiles = Round.OpponentsCanCallChii();
            if (chiiTiles.Count > 0)
            {
                // Proceeds to chii if :
                // - The hand is already open (we assume it's open for a good reason)
                // - The sequence does not already exist in the end
                if (!Round.Hands.ElementAt(Round.CurrentPlayerIndex).IsConcealed)
                {
                    Tuple<TilePivot, bool> tileChoice = null;
                    foreach (TilePivot tileKey in chiiTiles.Keys)
                    {
                        bool m2 = Round.Hands.ElementAt(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 2);
                        bool m1 = Round.Hands.ElementAt(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number - 1);
                        bool m0 = Round.Hands.ElementAt(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t == tileKey);
                        bool p1 = Round.Hands.ElementAt(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 1);
                        bool p2 = Round.Hands.ElementAt(Round.CurrentPlayerIndex).ConcealedTiles.Any(t => t.Family == tileKey.Family && t.Number == tileKey.Number + 2);

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

        private bool IsSafeForPlayer(TilePivot tile, int opponentPlayerIndex, List<TilePivot> deadtiles)
        {
            return Round.Discards.ElementAt(opponentPlayerIndex).Contains(tile) || (
                tile.IsHonor
                && deadtiles.Count(t => t == tile) == 4
                && deadtiles.GroupBy(t => t).Any(t => t.Key != tile && t.Key.IsHonor && t.Count() == 4)
            );
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
            Unknown,
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
