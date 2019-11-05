using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a draw.
    /// </summary>
    public class DrawPivot
    {
        #region Embedded properties

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

        #endregion Embedded properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="withRedDoras">Optionnal; indicates if the set used for the game should contain red doras; default value is <c>False</c>.</param>
        internal DrawPivot(bool withRedDoras = false)
        {
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
        }

        #endregion Constructors
    }
}
