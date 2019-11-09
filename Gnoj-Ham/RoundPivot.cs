using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a round in a game.
    /// </summary>
    public class RoundPivot
    {
        #region Embedded properties

        private bool _stealingInProgress = false;
        private bool _waitForDiscard = false;
        private readonly GamePivot _game;

        private readonly List<TilePivot> _wallTiles;
        private readonly List<HandPivot> _hands;
        private readonly List<TilePivot> _compensationTiles;
        private readonly List<TilePivot> _doraIndicatorTiles;
        private readonly List<TilePivot> _uraDoraIndicatorTiles;
        private readonly List<TilePivot> _deadTreasureTiles;
        private readonly List<List<TilePivot>> _discards;
        private readonly List<int> _riichiPositionInDiscard;

        /// <summary>
        /// Wall tiles.
        /// </summary>
        public IReadOnlyCollection<TilePivot> WallTiles
        {
            get
            {
                return _wallTiles;
            }
        }

        /// <summary>
        /// Hands of four players. The first one is east.
        /// </summary>
        public IReadOnlyCollection<HandPivot> Hands
        {
            get
            {
                return _hands;
            }
        }

        /// <summary>
        /// List of compensation tiles. 4 at the beginning, between 0 and 4 at the end.
        /// </summary>
        public IReadOnlyCollection<TilePivot> CompensationTiles
        {
            get
            {
                return _compensationTiles;
            }
        }

        /// <summary>
        /// List of dora indicator tiles. Always 5 (doesn't mean they're all visible).
        /// </summary>
        public IReadOnlyCollection<TilePivot> DoraIndicatorTiles
        {
            get
            {
                return _doraIndicatorTiles;
            }
        }

        /// <summary>
        /// List of ura-dora indicator tiles. Always 5 (doesn't mean they're all visible).
        /// </summary>
        public IReadOnlyCollection<TilePivot> UraDoraIndicatorTiles
        {
            get
            {
                return _uraDoraIndicatorTiles;
            }
        }

        /// <summary>
        /// Other tiles of the treasure Always 4 minus the number of tiles of <see cref="_compensationTiles"/>.
        /// </summary>
        public IReadOnlyCollection<TilePivot> DeadTreasureTiles
        {
            get
            {
                return _deadTreasureTiles;
            }
        }

        /// <summary>
        /// Discards of four players. The first one is east.
        /// </summary>
        public IReadOnlyCollection<IReadOnlyCollection<TilePivot>> Discards
        {
            get
            {
                return _discards.Select(d => d as IReadOnlyCollection<TilePivot>).ToList();
            }
        }

        /// <summary>
        /// Riichi mark in the discard of each player; <c>-1</c> if the player is not riichi. The first one is east.
        /// </summary>
        public IReadOnlyCollection<int> RiichiPositionInDiscard
        {
            get
            {
                return _riichiPositionInDiscard;
            }
        }

        /// <summary>
        /// The current player index, between 0 and 3.
        /// </summary>
        public int CurrentPlayerIndex { get; private set; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the current player is the human player.
        /// </summary>
        public bool IsHumanPlayer
        {
            get
            {
                return CurrentPlayerIndex == GamePivot.HUMAN_INDEX;
            }
        }

        /// <summary>
        /// Inferred; indicates the index of the player before <see cref="CurrentPlayerIndex"/>.
        /// </summary>
        public int PreviousPlayerIndex
        {
            get
            {
                return CurrentPlayerIndex == 0 ? 3 : CurrentPlayerIndex - 1;
            }
        }

        #endregion Inferred properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game">The <see cref="GamePivot"/>.</param>
        /// <param name="firstPlayerIndex">The initial <see cref="CurrentPlayerIndex"/> value.</param>
        /// <param name="withRedDoras">Optionnal; indicates if the set used for the game should contain red doras; default value is <c>False</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstPlayerIndex"/> value should be between <c>0</c> and <c>3</c>.</exception>
        internal RoundPivot(GamePivot game, int firstPlayerIndex, bool withRedDoras = false)
        {
            if (firstPlayerIndex < 0 || firstPlayerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(firstPlayerIndex));
            }

            List<TilePivot> tiles = TilePivot
                                    .GetCompleteSet(withRedDoras)
                                    .OrderBy(t => GlobalTools.Randomizer.NextDouble())
                                    .ToList();

            _hands = Enumerable.Range(0, 4).Select(i => new HandPivot(tiles.GetRange(i * 13, 13))).ToList();
            _discards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>()).ToList();
            _riichiPositionInDiscard = Enumerable.Range(0, 4).Select(i => -1).ToList();
            _wallTiles = tiles.GetRange(52, 70);
            _compensationTiles = tiles.GetRange(122, 4);
            _doraIndicatorTiles = tiles.GetRange(126, 5);
            _uraDoraIndicatorTiles = tiles.GetRange(131, 5);
            _deadTreasureTiles = new List<TilePivot>();
            CurrentPlayerIndex = firstPlayerIndex;

            _game = game;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Tries to pick the next tile from the wall.
        /// </summary>
        /// <returns><c>True</c> if success; <c>False</c> if failure (ie exhausted wall).</returns>
        public bool Pick()
        {
            if (_wallTiles.Count == 0 || _waitForDiscard)
            {
                return false;
            }

            TilePivot tile = _wallTiles.First();
            _wallTiles.Remove(tile);
            _hands[CurrentPlayerIndex].Pick(tile);
            _waitForDiscard = true;
            return true;
        }

        /// <summary>
        /// Checks if calling chii is allowed in this context.
        /// </summary>
        /// <returns>
        /// A dictionnary, where each <see cref="KeyValuePair{TilePivot, Boolean}"/> is an indication of the chii which can be made:
        /// - The key is the first tile (ie the lowest number) of <see cref="HandPivot.ConcealedTiles"/> to use in the sequence.
        /// - The value indicates if the key is used as lowest number in the sequence (<c>False</c>) or second (<c>True</c>, ie the tile stolen is the lowest number).
        /// The list is empty if calling chii is impossible.
        /// </returns>
        public Dictionary<TilePivot, bool> CanCallChii()
        {
            if (_wallTiles.Count == 0 || _discards[PreviousPlayerIndex].Count == 0 || _waitForDiscard)
            {
                return new Dictionary<TilePivot, bool>();
            }

            TilePivot tile = _discards[PreviousPlayerIndex].Last();
            if (tile.IsHonor)
            {
                return new Dictionary<TilePivot, bool>();
            }

            List<TilePivot> potentialTiles =
                _hands[CurrentPlayerIndex]
                    .ConcealedTiles
                    .Where(t => t.Family == tile.Family && t.Number != tile.Number && (t.Number >= tile.Number - 2 || t.Number <= tile.Number + 2))
                    .Distinct()
                    .ToList();

            TilePivot tileRelativePositionMinus2 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number - 2);
            TilePivot tileRelativePositionMinus1 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number - 1);
            TilePivot tileRelativePositionBonus1 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number + 1);
            TilePivot tileRelativePositionBonus2 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number + 2);

            var tilesFromConcealedHandWithRelativePosition = new Dictionary<TilePivot, bool>();
            if (tileRelativePositionMinus2 != null && tileRelativePositionMinus1 != null)
            {
                tilesFromConcealedHandWithRelativePosition.Add(tileRelativePositionMinus2, false);
            }
            if (tileRelativePositionMinus1 != null && tileRelativePositionBonus1 != null)
            {
                tilesFromConcealedHandWithRelativePosition.Add(tileRelativePositionMinus1, false);
            }
            if (tileRelativePositionBonus1 != null && tileRelativePositionBonus2 != null)
            {
                tilesFromConcealedHandWithRelativePosition.Add(tileRelativePositionBonus1, true);
            }

            return tilesFromConcealedHandWithRelativePosition;
        }

        /// <summary>
        /// Tries to call chii for the current player.
        /// </summary>
        /// <param name="startNumber">The number indicating the beginning of the sequence.</param>
        /// <returns><c>True</c> if success; <c>False</c> if failure.</returns>
        public bool CallChii(int startNumber)
        {
            if (CanCallChii().Keys.Count == 0)
            {
                return false;
            }

            _hands[CurrentPlayerIndex].DeclareChii(
                _discards[PreviousPlayerIndex].Last(),
                _game.GetPlayerCurrentWind(PreviousPlayerIndex),
                startNumber
            );
            _stealingInProgress = true;
            _waitForDiscard = true;
            return true;
        }

        /// <summary>
        /// Tries to discard the specified tile for the <see cref="CurrentPlayerIndex"/>.
        /// </summary>
        /// <param name="tile">The tile to discard.</param>
        /// <returns>
        /// <c>False</c> if the discard is forbidden by the tile stolen, or a discard is not expected in this context;
        /// <c>True</c> otherwise.
        /// </returns>
        public bool Discard(TilePivot tile)
        {
            if (!_waitForDiscard || !_hands[CurrentPlayerIndex].Discard(tile, _stealingInProgress))
            {
                return false;
            }

            _discards[CurrentPlayerIndex].Add(tile);
            _stealingInProgress = false;
            _waitForDiscard = false;
            SetCurrentPlayerIndex();
            return true;
        }

        /// <summary>
        /// Proceeds to default action for the current player: picks a tile from the wall and discard a random one.
        /// </summary>
        /// <returns>
        /// <c>False</c> if the wall is exhausted, or a move is not expected in this context;
        /// <c>True</c> otherwise.
        /// </returns>
        public bool AutoPickAndDiscard()
        {
            if (!Pick())
            {
                return false;
            }

            // Discards a random tile.
            // Can't fail as it's never from a stolen call.
            Discard(_hands[CurrentPlayerIndex].ConcealedTiles.Skip(GlobalTools.Randomizer.Next(0, _hands[CurrentPlayerIndex].ConcealedTiles.Count)).First());

            return true;
        }

        #endregion Public methods

        #region Private methods

        private void SetCurrentPlayerIndex()
        {
            CurrentPlayerIndex = CurrentPlayerIndex == 3 ? 0 : CurrentPlayerIndex + 1;
        }

        #endregion Private methods
    }
}
