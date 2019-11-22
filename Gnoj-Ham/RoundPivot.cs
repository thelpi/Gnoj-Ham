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

        private bool _stealingInProgress;
        private TilePivot _closedKanInProgress;
        private TilePivot _openedKanInProgress;
        private bool _waitForDiscard;
        private readonly GamePivot _game;
        private readonly List<int> _playerIndexHistory;
        private readonly List<TilePivot> _wallTiles;
        private readonly List<HandPivot> _hands;
        private readonly List<TilePivot> _compensationTiles;
        private readonly List<TilePivot> _doraIndicatorTiles;
        private readonly List<TilePivot> _uraDoraIndicatorTiles;
        private readonly List<TilePivot> _deadTreasureTiles;
        private readonly List<List<TilePivot>> _discards;
        private readonly List<List<TilePivot>> _virtualDiscards;
        private readonly List<RiichiPivot> _riichis;
        private readonly List<TilePivot> _fullTilesList;

        /// <summary>
        /// History of the latest players to play.
        /// First on the list is the latest to play.
        /// The list is cleared when a jump (ie a call) is made.
        /// </summary>
        public IReadOnlyCollection<int> PlayerIndexHistory
        {
            get
            {
                return _playerIndexHistory;
            }
        }

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
        /// Hands of four players.
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
        /// Discards of four players.
        /// </summary>
        public IReadOnlyCollection<IReadOnlyCollection<TilePivot>> Discards
        {
            get
            {
                return _discards.Select(d => d as IReadOnlyCollection<TilePivot>).ToList();
            }
        }

        /// <summary>
        /// Riichi informations of four players.
        /// </summary>
        /// <remarks>The list if filled by default with <c>Null</c> for every players.</remarks>
        public IReadOnlyCollection<RiichiPivot> Riichis
        {
            get
            {
                return _riichis;
            }
        }

        /// <summary>
        /// The current player index, between 0 and 3.
        /// </summary>
        public int CurrentPlayerIndex { get; private set; }

        /// <summary>
        /// IA manager.
        /// </summary>
        public IaManagerPivot IaManager { get; private set; }

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
                return CurrentPlayerIndex.RelativePlayerIndex(-1);
            }
        }

        /// <summary>
        /// Inferred; indicates if the current round is over by wall exhaustion.
        /// </summary>
        public bool IsWallExhaustion
        {
            get
            {
                return WallTiles.Count == 0;
            }
        }

        /// <summary>
        /// Inferred; count of visible doras.
        /// </summary>
        public int VisibleDorasCount
        {
            get
            {
                return 1 + (4 - _compensationTiles.Count);
            }
        }

        #endregion Inferred properties

        #region Events

        /// <summary>
        /// Event triggered when the tiles count in the wall changes.
        /// </summary>
        public event EventHandler NotifyWallCount;

        #endregion Events

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game">The <see cref="GamePivot"/>.</param>
        /// <param name="firstPlayerIndex">The initial <see cref="CurrentPlayerIndex"/> value.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstPlayerIndex"/> value should be between <c>0</c> and <c>3</c>.</exception>
        internal RoundPivot(GamePivot game, int firstPlayerIndex)
        {
            if (firstPlayerIndex < 0 || firstPlayerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(firstPlayerIndex));
            }

            _game = game;
            
            _fullTilesList = TilePivot
                                .GetCompleteSet(_game.WithRedDoras)
                                .OrderBy(t => _game.SortedDraw ? 1 : GlobalTools.Randomizer.NextDouble())
                                .ToList();

            _hands = Enumerable.Range(0, 4).Select(i => new HandPivot(_fullTilesList.GetRange(i * 13, 13))).ToList();
            _discards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>()).ToList();
            _virtualDiscards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>()).ToList();
            _riichis = Enumerable.Range(0, 4).Select(i => (RiichiPivot)null).ToList();
            _wallTiles = _fullTilesList.GetRange(52, 70);
            _compensationTiles = _fullTilesList.GetRange(122, 4);
            _doraIndicatorTiles = _fullTilesList.GetRange(126, 5);
            _uraDoraIndicatorTiles = _fullTilesList.GetRange(131, 5);
            _deadTreasureTiles = new List<TilePivot>();
            CurrentPlayerIndex = firstPlayerIndex;
            _stealingInProgress = false;
            _closedKanInProgress = null;
            _openedKanInProgress = null;
            _waitForDiscard = false;
            _playerIndexHistory = new List<int>();
            IaManager = new IaManagerPivot(this);
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Tries to pick the next tile from the wall.
        /// </summary>
        /// <returns>The tile if success; <c>null</c> if failure (ie exhausted wall).</returns>
        public TilePivot Pick()
        {
            if (_wallTiles.Count == 0 || _waitForDiscard)
            {
                return null;
            }

            TilePivot tile = _wallTiles.First();
            _wallTiles.Remove(tile);
            NotifyWallCount?.Invoke(null, null);
            _hands[CurrentPlayerIndex].Pick(tile);
            _waitForDiscard = true;
            return tile;
        }

        /// <summary>
        /// Checks if calling chii is allowed for the specified player.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>
        /// A dictionnary, where each <see cref="KeyValuePair{TilePivot, Boolean}"/> is an indication of the chii which can be made:
        /// - The key is the first tile (ie the lowest number) of <see cref="HandPivot.ConcealedTiles"/> to use in the sequence.
        /// - The value indicates if the key is used as lowest number in the sequence (<c>False</c>) or second (<c>True</c>, ie the tile stolen is the lowest number).
        /// The list is empty if calling chii is impossible.
        /// </returns>
        public Dictionary<TilePivot, bool> CanCallChii(int playerIndex)
        {
            if (_wallTiles.Count == 0 || CurrentPlayerIndex != playerIndex || _discards[PreviousPlayerIndex].Count == 0 || _waitForDiscard || IsRiichi(playerIndex))
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
        /// Checks if calling pon is allowed for the specified player in this context.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns><c>True</c> if calling pon is allowed in this context; <c>False otherwise.</c></returns>
        public bool CanCallPon(int playerIndex)
        {
            if (_wallTiles.Count == 0 || PreviousPlayerIndex == playerIndex || _discards[PreviousPlayerIndex].Count == 0 || _waitForDiscard || IsRiichi(playerIndex))
            {
                return false;
            }

            return _hands[playerIndex].ConcealedTiles.Where(t => t == _discards[PreviousPlayerIndex].Last()).Count() >= 2;
        }

        /// <summary>
        /// Checks if calling kan is allowed for the specified player in this context.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>A tile from every possible kans.</returns>
        public List<TilePivot> CanCallKan(int playerIndex)
        {
            if (_compensationTiles.Count == 0)
            {
                return new List<TilePivot>();
            }
            
            if (CurrentPlayerIndex == playerIndex && _waitForDiscard)
            {
                IEnumerable<TilePivot> kansFromConcealed =
                    _hands[playerIndex].ConcealedTiles
                                            .GroupBy(t => t)
                                            .Where(t => t.Count() == 4)
                                            .Select(t => t.Key)
                                            .Distinct();

                // If the player is riichi, he can only call a concealed kan:
                // - on the tile he just picks
                // - if "disposableForRiichi" contains only this tile
                if (IsRiichi(playerIndex))
                {
                    List<TilePivot> disposableForRiichi = ExtractRiichiPossibilities(playerIndex);
                    if (disposableForRiichi.Any(t => t != _hands[playerIndex].LatestPick))
                    {
                        return new List<TilePivot>();
                    }
                    kansFromConcealed = kansFromConcealed.Where(t => t == _hands[playerIndex].LatestPick);
                }

                IEnumerable<TilePivot> kansFromPons =
                    _hands[playerIndex].DeclaredCombinations
                                            .Where(c => c.IsBrelan && _hands[playerIndex].ConcealedTiles.Any(t => t == c.OpenTile))
                                            .Select(c => c.OpenTile)
                                            .Distinct();

                var everyKans = new List<TilePivot>(kansFromConcealed);
                everyKans.AddRange(kansFromPons);

                return everyKans;
            }
            else
            {
                if (_waitForDiscard || PreviousPlayerIndex == playerIndex || _discards[PreviousPlayerIndex].Count == 0 || IsRiichi(playerIndex))
                {
                    return new List<TilePivot>();
                }

                TilePivot referenceTileFromDiscard = _discards[PreviousPlayerIndex].Last();
                if (_hands[playerIndex].ConcealedTiles.Where(t => t == referenceTileFromDiscard).Count() >= 3)
                {
                    return new List<TilePivot>
                    {
                        referenceTileFromDiscard
                    };
                }

                return new List<TilePivot>();
            }
        }

        /// <summary>
        /// Tries to call chii for the specified player.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <param name="startNumber">The number indicating the beginning of the sequence.</param>
        /// <returns><c>True</c> if success; <c>False</c> if failure.</returns>
        public bool CallChii(int playerIndex, int startNumber)
        {
            if (CanCallChii(playerIndex).Keys.Count == 0)
            {
                return false;
            }

            _hands[CurrentPlayerIndex].DeclareChii(
                _discards[PreviousPlayerIndex].Last(),
                _game.GetPlayerCurrentWind(PreviousPlayerIndex),
                startNumber
            );
            _discards[PreviousPlayerIndex].RemoveAt(_discards[PreviousPlayerIndex].Count - 1);
            _stealingInProgress = true;
            _waitForDiscard = true;
            return true;
        }

        /// <summary>
        /// Tries to call pon for the specified player.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns><c>True</c> if success; <c>False</c> if failure.</returns>
        public bool CallPon(int playerIndex)
        {
            if (!CanCallPon(playerIndex))
            {
                return false;
            }

            _hands[playerIndex].DeclarePon(
                _discards[PreviousPlayerIndex].Last(),
                _game.GetPlayerCurrentWind(PreviousPlayerIndex)
            );
            _discards[PreviousPlayerIndex].RemoveAt(_discards[PreviousPlayerIndex].Count - 1);
            CurrentPlayerIndex = playerIndex;
            _stealingInProgress = true;
            _waitForDiscard = true;
            return true;
        }

        /// <summary>
        /// Tries to call kan for the specified player.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="tileChoice">Optionnal; the tile choice, if the current player is <paramref name="playerIndex"/> and he has several possible tiles in its hand; default value is <c>Null</c>.</param>
        /// <returns>The tile picked as compensation; <c>Null</c> if failure.</returns>
        public TilePivot CallKan(int playerIndex, TilePivot tileChoice = null)
        {
            if (CanCallKan(playerIndex).Count == 0)
            {
                return null;
            }

            TileComboPivot fromPreviousPon = tileChoice == null ? null :
                _hands[playerIndex].DeclaredCombinations.FirstOrDefault(c => c.IsBrelan && c.OpenTile == tileChoice);

            if (CurrentPlayerIndex == playerIndex
                && tileChoice != null
                && !_hands[playerIndex].ConcealedTiles.GroupBy(t => t).Any(t => t.Count() == 4 && t.Key == tileChoice)
                && fromPreviousPon == null)
            {
                throw new ArgumentException(Messages.InvalidKanTileChoice, nameof(tileChoice));
            }

            bool isClosedKan = false;
            if (CurrentPlayerIndex == playerIndex)
            {
                // Forces a decision, even if there're several possibilities.
                if (tileChoice == null)
                {
                    tileChoice = _hands[playerIndex].ConcealedTiles.GroupBy(t => t).FirstOrDefault(t => t.Count() == 4)?.Key;
                    if (tileChoice == null)
                    {
                        tileChoice = _hands[playerIndex].ConcealedTiles.First(t => _hands[playerIndex].DeclaredCombinations.Any(c => c.IsBrelan && c.OpenTile == t));
                        fromPreviousPon = _hands[playerIndex].DeclaredCombinations.First(c =>  c.OpenTile == tileChoice);
                    }
                }

                _hands[playerIndex].DeclareKan(tileChoice, null, fromPreviousPon);
                if (fromPreviousPon != null)
                {
                    _virtualDiscards[playerIndex].Add(tileChoice);
                }
                isClosedKan = true;
            }
            else
            {
                _hands[playerIndex].DeclareKan(
                    _discards[PreviousPlayerIndex].Last(),
                    _game.GetPlayerCurrentWind(PreviousPlayerIndex),
                    null
                );
                _discards[PreviousPlayerIndex].RemoveAt(_discards[PreviousPlayerIndex].Count - 1);
                CurrentPlayerIndex = playerIndex;
                _stealingInProgress = true;
            }

            _waitForDiscard = true;

            return PickCompensationTile(isClosedKan);
        }

        /// <summary>
        /// Proceeds to call riichi.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="tile">The discarded tile.</param>
        /// <exception cref="InvalidOperationException"><see cref="Messages.UnexpectedDiscardFail"/></exception>
        public bool CallRiichi(int playerIndex, TilePivot tile)
        {
            if (!CanCallRiichi(playerIndex).Contains(tile))
            {
                return false;
            }

            // Computes before discard, but proceeds after.
            // Otherwise, the discard will fail.
            int riichiTurnsCount = _discards[playerIndex].Count;
            bool isUninterruptedFirstTurn = _discards[playerIndex].Count == 0 && IsUninterruptedHistory(playerIndex);

            if (!Discard(tile))
            {
                throw new InvalidOperationException(Messages.UnexpectedDiscardFail);
            }
            
            _riichis[playerIndex] = new RiichiPivot(riichiTurnsCount, isUninterruptedFirstTurn, tile,
                Enumerable.Range(0, 4).Where(i => i != playerIndex).Select(i => new KeyValuePair<int, int>(i, _virtualDiscards[i].Count)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            _game.AddPendingRiichi(playerIndex);

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
            if (!CanDiscard(tile))
            {
                return false;
            }

            _hands[CurrentPlayerIndex].Discard(tile, _stealingInProgress);

            if (_stealingInProgress || _closedKanInProgress != null)
            {
                _playerIndexHistory.Clear();
            }

            _discards[CurrentPlayerIndex].Add(tile);
            _virtualDiscards[CurrentPlayerIndex].Add(tile);
            _stealingInProgress = false;
            _closedKanInProgress = null;
            _openedKanInProgress = null;
            _waitForDiscard = false;
            _playerIndexHistory.Insert(0, CurrentPlayerIndex);
            CurrentPlayerIndex = CurrentPlayerIndex.RelativePlayerIndex(1);
            return true;
        }

        /// <summary>
        /// Checks if the specified player can call riichi.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <returns>Tiles the player can discard; empty list if riichi is impossible.</returns>
        public List<TilePivot> CanCallRiichi(int playerIndex)
        {
            if (CurrentPlayerIndex != playerIndex
                || !_waitForDiscard
                || IsRiichi(playerIndex)
                || !_hands[playerIndex].IsConcealed
                || _wallTiles.Count < 4
                || _game.Players.ElementAt(playerIndex).Points < ScoreTools.RIICHI_COST)
            {
                return new List<TilePivot>();
            }

            // TODO: if already 3 riichi calls, what to do ?

            return ExtractRiichiPossibilities(playerIndex);
        }

        /// <summary>
        /// Checks if the hand of the current player is ready for calling tsumo.
        /// </summary>
        /// <param name="isKanCompensation"><c>True</c> if the latest pick comes from a kan compensation.</param>
        /// <returns><c>True</c> if ready for tsumo; <c>False</c> otherwise.</returns>
        public bool CanCallTsumo(bool isKanCompensation)
        {
            if (!_waitForDiscard)
            {
                return false;
            }

            SetYakus(CurrentPlayerIndex,
                _hands[CurrentPlayerIndex].LatestPick,
                isKanCompensation ? DrawTypePivot.Compensation : DrawTypePivot.Wall);

            return _hands[CurrentPlayerIndex].IsComplete;
        }

        /// <summary>
        /// Checks if the hand of the specified player is ready for calling ron.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns><c>True</c> if calling ron is possible; <c>False</c> otherwise.</returns>
        public bool CanCallRon(int playerIndex)
        {
            TilePivot tile = _waitForDiscard ? null : _discards[PreviousPlayerIndex].LastOrDefault();
            bool forKokushiOnly = false;
            bool isChanka = false;
            if (CurrentPlayerIndex != playerIndex)
            {
                if (_closedKanInProgress != null)
                {
                    tile = _closedKanInProgress;
                    forKokushiOnly = true;
                    isChanka = true;
                }
                else if (_openedKanInProgress != null)
                {
                    tile = _openedKanInProgress;
                    isChanka = true;
                }
            }

            if (tile == null)
            {
                return false;
            }

            SetYakus(playerIndex, tile, forKokushiOnly ? DrawTypePivot.OpponentKanCallConcealed : (isChanka ? DrawTypePivot.OpponentKanCallOpen : DrawTypePivot.OpponentDiscard));

            if (!_hands[playerIndex].IsComplete
                || _hands[playerIndex].CancelYakusIfFuriten(_discards[playerIndex], GetTilesFromVirtualDiscardsAtRank(playerIndex, tile))
                || _hands[playerIndex].CancelYakusIfTemporaryFuriten(this, playerIndex))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the hand of the specified player is tenpai.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns><c>True</c> if tenpai; <c>False</c> otherwise.</returns>
        public bool IsTenpai(int playerIndex)
        {
            // TODO : there're (maybe) specific rules about it:
            // for instance, what if I have a single wait on tile "4 circle" but every tiles "4 circle" are already in my hand ?
            return _hands[playerIndex].IsTenpai(_fullTilesList);
        }

        /// <summary>
        /// Checks if the specified player is riichi.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <returns><c>True</c> if riichi; <c>False</c> otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerIndex"/> is out of range.</exception>
        public bool IsRiichi(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            return _riichis[playerIndex] != null;
        }

        /// <summary>
        /// Checks, for a specified player, if the specified rank is the one when the riichi call has been made.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="rank">The rank.</param>
        /// <returns><c>True</c> if the specified rank is the riichi one.</returns>
        public bool IsRiichiRank(int playerIndex, int rank)
        {
            if (playerIndex < 0 || playerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            return _riichis[playerIndex] != null && _riichis[playerIndex].DiscardRank == rank;
        }

        /// <summary>
        /// Checks if the human player can auto-discard.
        /// </summary>
        /// <returns><c>True</c> if he can; <c>False</c> otherwise.</returns>
        public bool HumanCanAutoDiscard()
        {
            return IsRiichi(GamePivot.HUMAN_INDEX)
                && CanCallKan(GamePivot.HUMAN_INDEX).Count == 0
                && _waitForDiscard;
        }

        /// <summary>
        /// Undoes the pick of a compensation tile after a kan.
        /// </summary>
        public void UndoPickCompensationTile()
        {
            TilePivot compensationTile = _closedKanInProgress ?? _openedKanInProgress;
            if (compensationTile == null)
            {
                return;
            }
            
            _compensationTiles.Insert(0, compensationTile);

            _wallTiles.Add(_deadTreasureTiles.Last());
            _deadTreasureTiles.RemoveAt(_deadTreasureTiles.Count - 1);

            // We could remove the compensation tile from the CurrentPlayerIndex hand, but it's not very useful in this context.
        }

        /// <summary>
        /// Checks if a priority call can be made by the specified player.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <returns><c>True</c> if call available; <c>False otherwise</c>.</returns>
        public bool CanCallPonOrKan(int playerIndex)
        {
            return CanCallKan(playerIndex).Count > 0 || CanCallPon(playerIndex);
        }

        /// <summary>
        /// Checks if a kan call can be made by any opponent of the human player.
        /// </summary>
        /// <param name="concealed"><c>True</c> to check only concealed kan (or from a previous pon); <c>False</c> to check the opposite; <c>Null</c> for both.</param>
        /// <returns>The player index who can make the kan call, and the possible tiles; <c>Null</c> is none.</returns>
        public Tuple<int, List<TilePivot>> OpponentsCanCallKan(bool? concealed)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i != GamePivot.HUMAN_INDEX)
                {
                    List<TilePivot> kanTiles = CanCallKanWithChoices(i, concealed);
                    if (kanTiles.Count > 0)
                    {
                        return new Tuple<int, List<TilePivot>>(i, kanTiles);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Similar to <see cref="CanCallKan(int)"/> but with the list of possible tiles depending on <paramref name="concealed"/>.
        /// </summary>
        /// <param name="playerId">The player index.</param>
        /// <param name="concealed"><c>True</c> to check only concealed kan (or from a previous pon); <c>False</c> to check the opposite; <c>Null</c> for both.</param>
        /// <returns>List of possible tiles.</returns>
        public List<TilePivot> CanCallKanWithChoices(int playerId, bool? concealed)
        {
            List<TilePivot> tiles = CanCallKan(playerId);
            if (concealed == true)
            {
                tiles = tiles.Where(t => _hands[playerId].ConcealedTiles.Count(ct => t == ct) == 4
                    || _hands[playerId].DeclaredCombinations.Any(ct => ct.IsBrelan && t == ct.OpenTile)).ToList();
            }
            else if (concealed == false)
            {
                tiles = tiles.Where(t => _hands[playerId].ConcealedTiles.Count(ct => t == ct) == 3).ToList();
            }

            return tiles;
        }

        /// <summary>
        /// Checks if a pon call can be made by any opponent of the human player.
        /// </summary>
        /// <returns>The player index who can make the pon call; <c>-1</c> is none.</returns>
        public int OpponentsCanCallPon()
        {
            List<int> opponentsIndex = Enumerable.Range(0, 4).Where(i =>
            {
                return i != GamePivot.HUMAN_INDEX && CanCallPon(i);
            }).ToList();

            return opponentsIndex.Count > 0 ? opponentsIndex[0] : -1;
        }

        /// <summary>
        /// Checks if a chii call can be made by any opponent of the human player.
        /// </summary>
        /// <returns>same type of return than the method <see cref="CanCallChii(int)"/>, for the opponent who can call chii.</returns>
        public Dictionary<TilePivot, bool> OpponentsCanCallChii()
        {
            for (int i = 0; i < 4; i++)
            {
                if (i != GamePivot.HUMAN_INDEX)
                {
                    Dictionary<TilePivot, bool> chiiTiles = CanCallChii(i);
                    if (chiiTiles.Count > 0)
                    {
                        return chiiTiles;
                    }
                }
            }

            return new Dictionary<TilePivot, bool>();
        }

        #endregion Public methods

        #region Private methods

        // Gets every tiles from every opponents virtual discards after the riichi call of the specified player.
        private List<TilePivot> GetTilesFromVirtualDiscardsAtRank(int riichiPlayerIndex, TilePivot exceptTile)
        {
            var fullList = new List<TilePivot>();

            if (_riichis[riichiPlayerIndex] == null)
            {
                return fullList;
            }

            for (int i = 0; i < 4; i++)
            {
                if (i != riichiPlayerIndex)
                {
                    int opponentRank = _riichis[riichiPlayerIndex].OpponentsVirtualDiscardRank[i];
                    fullList.AddRange(_virtualDiscards[i].Skip(opponentRank));
                }
            }

            return fullList.Where(t => !ReferenceEquals(t, exceptTile)).ToList();
        }

        // Checks if the hand of the specified player is riichi and list tiles which can be discarded.
        private List<TilePivot> ExtractRiichiPossibilities(int playerIndex)
        {
            List<TilePivot> distinctTilesFromOverallConcealed = GetConcealedTilesFromPlayerPointOfView(playerIndex).Distinct().ToList();

            var subPossibilities = new List<TilePivot>();
            foreach (TilePivot tileToSub in _hands[playerIndex].ConcealedTiles.Distinct())
            {
                var tempListConcealed = new List<TilePivot>(_hands[playerIndex].ConcealedTiles);
                tempListConcealed.Remove(tileToSub);
                if (HandPivot.IsTenpai(tempListConcealed, _hands[playerIndex].DeclaredCombinations, distinctTilesFromOverallConcealed))
                {
                    subPossibilities.Add(tileToSub);
                }
            }

            // Avoids red doras in the list returned (if possible).
            var realSubPossibilities = new List<TilePivot>();
            foreach (TilePivot tile in subPossibilities.Distinct())
            {
                TilePivot subTile = null;
                if (tile.IsRedDora)
                {
                    subTile = _hands[playerIndex].ConcealedTiles.FirstOrDefault(t => t == tile && !t.IsRedDora);
                }
                realSubPossibilities.Add(subTile ?? tile);
            }

            return realSubPossibilities.Distinct().ToList();
        }

        // Picks a compensation tile (after a kan call) for the current player.
        private TilePivot PickCompensationTile(bool isClosedKan)
        {
            TilePivot compensationTile = _compensationTiles.First();
            _compensationTiles.RemoveAt(0);

            _deadTreasureTiles.Add(_wallTiles.Last());

            _wallTiles.RemoveAt(_wallTiles.Count - 1);
            NotifyWallCount?.Invoke(null, null);

            _hands[CurrentPlayerIndex].Pick(compensationTile);

            if (isClosedKan)
            {
                _closedKanInProgress = compensationTile;
            }
            else
            {
                _openedKanInProgress = compensationTile;
            }

            return compensationTile;
        }

        // Checks there's no call interruption since the latest move of the specified player.
        private bool IsUninterruptedHistory(int playerIndex)
        {
            List<int> historySinceLastTime = _playerIndexHistory.TakeWhile(i => i != playerIndex).ToList();

            return historySinceLastTime.Count <= 3 && Enumerable.Range(0, 3).All(i => historySinceLastTime.Count <= i || historySinceLastTime[i] == playerIndex.RelativePlayerIndex(-(i + 1)));
        }

        // Creates the context and calls "SetYakus" for the specified player.
        private void SetYakus(int playerIndex, TilePivot tile, DrawTypePivot drawType)
        {
            _hands[playerIndex].SetYakus(new WinContextPivot(
                latestTile: tile,
                drawType: drawType,
                dominantWind: _game.DominantWind,
                playerWind: _game.GetPlayerCurrentWind(playerIndex),
                isFirstOrLast: IsWallExhaustion ? (bool?)null : (_discards[playerIndex].Count == 0 && IsUninterruptedHistory(playerIndex)),
                isRiichi: IsRiichi(playerIndex) ? (_riichis[playerIndex].IsDaburu ? (bool?)null : true) : false,
                isIppatsu: IsRiichi(playerIndex) && _discards[playerIndex].Count > 0 && ReferenceEquals(_discards[playerIndex].Last(), _riichis[playerIndex].Tile) && IsUninterruptedHistory(playerIndex),
                useRenhou: _game.UseRenhou
            ));
        }

        // Gets the concealed tile of the round from the point of view of a specified player.
        private IReadOnlyCollection<TilePivot> GetConcealedTilesFromPlayerPointOfView(int playerIndex)
        {
            // Wall tiles.
            var tiles = new List<TilePivot>(_wallTiles);

            // Concealed tiles from opponents.
            Enumerable.Range(0, 4)
                .Where(i => i != playerIndex)
                .Select(i => _hands[i].ConcealedTiles)
                .All(tList =>
                {
                    tiles.AddRange(tList);
                    return true;
                });

            // Compensation tiles.
            tiles.AddRange(_compensationTiles);

            // Dead treasure tiles.
            tiles.AddRange(_deadTreasureTiles);

            // Ura-dora tiles.
            tiles.AddRange(_uraDoraIndicatorTiles);

            // Dora tiles except when visible.
            tiles.AddRange(_doraIndicatorTiles.Skip(1 + (4 - _compensationTiles.Count)));

            return tiles;
        }

        // Checks for players with nagashi mangan.
        private List<int> CheckForNagashiMangan()
        {
            var playerIndexList = new List<int>();

            for (int i = 0; i < 4; i++)
            {
                bool fullTerminalsOrHonors = _discards[i].All(t => t.IsHonorOrTerminal);
                bool noPlayerStealing = _hands[i].IsConcealed;
                bool noOpponentStealing = !_hands.Where(h => _hands.IndexOf(h) != i).Any(h => h.DeclaredCombinations.Any(c => c.StolenFrom == _game.GetPlayerCurrentWind(i)));
                if (fullTerminalsOrHonors && noPlayerStealing && noOpponentStealing)
                {
                    _hands[i].SetYakus(new WinContextPivot());
                    playerIndexList.Add(i);
                }
            }

            return playerIndexList;
        }

        #endregion Private methods

        #region Internal methods

        /// <summary>
        /// Checks if the specified tile is allowed for discard for the current player.
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        /// <returns></returns>
        internal bool CanDiscard(TilePivot tile)
        {
            if (!_waitForDiscard || (IsRiichi(CurrentPlayerIndex) && !ReferenceEquals(tile, _hands[CurrentPlayerIndex].LatestPick)))
            {
                return false;
            }

            if (!_hands[CurrentPlayerIndex].CanDiscardTile(tile, _stealingInProgress))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Manages the end of a round.
        /// </summary>
        /// <param name="ronPlayerIndex">The player index on who the call has been made; <c>Null</c> if tsumo or ryuukyoku.</param>
        /// <returns>An instance of <see cref="EndOfRoundInformationsPivot"/>.</returns>
        internal EndOfRoundInformationsPivot EndOfRound(int? ronPlayerIndex)
        {
            bool turnWind = false;
            bool resetsRiichiCount = false;
            bool displayUraDoraTiles = false;

            List<int> winners = _hands.Where(h => h.IsComplete).Select(w => _hands.IndexOf(w)).ToList();

            if (winners.Count == 0 && _game.UseNagashiMangan)
            {
                List<int> iNagashiList = CheckForNagashiMangan();
                if (iNagashiList.Count > 0)
                {
                    winners.AddRange(iNagashiList);
                }
            }
            
            var playerInfos = new List<EndOfRoundInformationsPivot.PlayerInformationsPivot>();

            // Ryuukyoku (no winner).
            if (winners.Count == 0)
            {
                List<int> tenpaiPlayersIndex = Enumerable.Range(0, 4).Where(i => IsTenpai(i)).ToList();
                List<int> notTenpaiPlayersIndex = Enumerable.Range(0, 4).Except(tenpaiPlayersIndex).ToList();

                // Wind turns if East is not tenpai.
                turnWind = notTenpaiPlayersIndex.Any(tpi => _game.GetPlayerCurrentWind(tpi) == WindPivot.East);

                Tuple<int, int> points = ScoreTools.GetRyuukyokuPoints(tenpaiPlayersIndex.Count);

                tenpaiPlayersIndex.ForEach(i => playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(i, 0, 0, _hands[i], points.Item1, 0, 0, 0)));
                notTenpaiPlayersIndex.ForEach(i => playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(i, points.Item2)));
            }
            else
            {
                turnWind = !winners.Any(w => _game.GetPlayerCurrentWind(w) == WindPivot.East);

                // TODO : Sekinin barai

                int eastOrLoserLostCumul = 0;
                int notEastLostCumul = 0;
                foreach (int pIndex in winners)
                {
                    HandPivot phand = _hands[pIndex];

                    // In case of ron, fix the "LatestPick" property of the winning hand
                    if (ronPlayerIndex.HasValue)
                    {
                        phand.SetFromRon(_discards[ronPlayerIndex.Value].Last());
                    }

                    bool isRiichi = phand.Yakus.Contains(YakuPivot.Riichi) || phand.Yakus.Contains(YakuPivot.DaburuRiichi);

                    int dorasCount = phand.AllTiles.Sum(t => DoraIndicatorTiles.Take(VisibleDorasCount).Count(d => t.IsDoraNext(d)));
                    int uraDorasCount = isRiichi ? phand.AllTiles.Sum(t => UraDoraIndicatorTiles.Take(VisibleDorasCount).Count(d => t.IsDoraNext(d))) : 0;
                    int redDorasCount = phand.AllTiles.Count(t => t.IsRedDora);

                    if (isRiichi)
                    {
                        displayUraDoraTiles = true;
                    }

                    int fanCount = ScoreTools.GetFanCount(phand.Yakus, phand.IsConcealed, dorasCount, uraDorasCount, redDorasCount);
                    int fuCount = ScoreTools.GetFuCount(phand, !ronPlayerIndex.HasValue, _game.DominantWind, _game.GetPlayerCurrentWind(pIndex));

                    Tuple<int, int> finalScore = ScoreTools.GetPoints(fanCount, fuCount, _game.EastIndexTurnCount - 1, winners.Count,
                        !ronPlayerIndex.HasValue, _game.GetPlayerCurrentWind(pIndex));

                    // TODO: if RiichiPendingCount si not a multiple of 3, and there're three winners, it doesn't work well !
                    int riichiPart = _game.PendingRiichiCount * ScoreTools.RIICHI_COST / winners.Count;

                    playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                        pIndex, fanCount, fuCount, phand,
                        finalScore.Item1 + finalScore.Item2 * 2 + riichiPart,
                        dorasCount, uraDorasCount, redDorasCount));

                    eastOrLoserLostCumul -= finalScore.Item1;
                    notEastLostCumul -= finalScore.Item2;
                }

                if (ronPlayerIndex.HasValue)
                {
                    playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(ronPlayerIndex.Value, eastOrLoserLostCumul));
                }
                else
                {
                    for (int pIndex = 0; pIndex < 4; pIndex++)
                    {
                        if (!winners.Contains(pIndex))
                        {
                            playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(pIndex, _game.GetPlayerCurrentWind(pIndex) == WindPivot.East ? eastOrLoserLostCumul : notEastLostCumul));
                        }
                    }
                }

                resetsRiichiCount = true;
            }
            
            foreach (EndOfRoundInformationsPivot.PlayerInformationsPivot p in playerInfos)
            {
                _game.Players.ElementAt(p.Index).AddPoints(p.PointsGain);
            }

            return new EndOfRoundInformationsPivot(resetsRiichiCount, turnWind, displayUraDoraTiles, playerInfos, _game.EastIndexTurnCount - 1,
                _game.PendingRiichiCount, DoraIndicatorTiles, UraDoraIndicatorTiles, VisibleDorasCount);
        }

        #endregion Internal methods
    }
}