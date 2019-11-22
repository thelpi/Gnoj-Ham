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
        /// <exception cref=""><paramref name="round"/> is <c>Null</c>.</exception>
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

            // TODO
            return Round.Hands.ElementAt(Round.CurrentPlayerIndex)
                .ConcealedTiles
                .First(t => Round.CanDiscard(t));
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

            List<TilePivot> riichiTiles = Round.CanCallRiichi(Round.CurrentPlayerIndex);
            if (riichiTiles.Count > 0)
            {
                // TODO
                return riichiTiles.First();
            }

            return null;
        }

        /// <summary>
        /// Checks if any CPU player can make a pon call, and computes its decision if any.
        /// </summary>
        /// <returns>The player index who makes the call; <c>-1</c> is none<./returns>
        public int PonDecision()
        {
            int opponentPlayerId = Round.OpponentsCanCallPon();
            if (opponentPlayerId > -1)
            {
                // TODO
                return opponentPlayerId;
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

            // TODO
            return true;
        }

        /// <summary>
        /// Checks for CPU players who can make a kan call, and computes the decision to call it.
        /// </summary>
        /// <param name="checkConcealedOnly">
        /// <c>True</c> to check only concealed kan (or from a previous pon);
        /// <c>False</c> to check the opposite;
        /// <c>Null</c> for both.
        /// </param>
        /// <returns>
        /// If the decision is made, a tuple :
        /// - The first item indicates the player index who call the kan.
        /// - The second item indicates the base tile of the kand (several choices are possible).
        /// <c>Null</c> otherwise.
        /// </returns>
        public Tuple<int, TilePivot> KanDecision(bool? checkConcealedOnly)
        {
            Tuple<int, List<TilePivot>> opponentPlayerIdWithTiles = Round.OpponentsCanCallKan(checkConcealedOnly);
            if (opponentPlayerIdWithTiles != null)
            {
                // TODO
                return new Tuple<int, TilePivot>(opponentPlayerIdWithTiles.Item1, opponentPlayerIdWithTiles.Item2.First());
            }

            return null;
        }

        /// <summary>
        /// Checks for CPU players who can make a ron call, and computes the decision to call it.
        /// </summary>
        /// <remarks>If any player, including human, calls ron, every players who can call ron will do.</remarks>
        /// <param name="ronCalled">Indicates if the human player has already made a ron call.</param>
        /// <returns><c>True</c> if any decision made; <c>False</c> otherwise.</returns>
        public bool RonDecision(bool ronCalled)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i != GamePivot.HUMAN_INDEX && Round.CanCallRon(i))
                {
                    if (!ronCalled)
                    {
                        // TODO
                        ronCalled = true;
                    }
                }
            }
            return ronCalled;
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
                // TODO
                return new Tuple<TilePivot, bool>(chiiTiles.First().Key, chiiTiles.First().Value);
            }

            return null;
        }

        #endregion Public methods
    }
}
