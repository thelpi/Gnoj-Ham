using System;
using System.Collections.Concurrent;
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
        public IReadOnlyList<int> PlayerIndexHistory => _playerIndexHistory;

        /// <summary>
        /// Wall tiles.
        /// </summary>
        public IReadOnlyList<TilePivot> WallTiles => _wallTiles;

        /// <summary>
        /// List of compensation tiles. 4 at the beginning, between 0 and 4 at the end.
        /// </summary>
        public IReadOnlyList<TilePivot> CompensationTiles => _compensationTiles;

        /// <summary>
        /// List of dora indicator tiles. Always 5 (doesn't mean they're all visible).
        /// </summary>
        public IReadOnlyList<TilePivot> DoraIndicatorTiles => _doraIndicatorTiles;

        /// <summary>
        /// List of ura-dora indicator tiles. Always 5 (doesn't mean they're all visible).
        /// </summary>
        public IReadOnlyList<TilePivot> UraDoraIndicatorTiles => _uraDoraIndicatorTiles;

        /// <summary>
        /// Other tiles of the treasure Always 4 minus the number of tiles of <see cref="_compensationTiles"/>.
        /// </summary>
        public IReadOnlyList<TilePivot> DeadTreasureTiles => _deadTreasureTiles;

        /// <summary>
        /// Riichi informations of four players.
        /// </summary>
        /// <remarks>The list if filled by default with <c>Null</c> for every players.</remarks>
        public IReadOnlyList<RiichiPivot> Riichis => _riichis;

        /// <summary>
        /// The current player index, between 0 and 3.
        /// </summary>
        public int CurrentPlayerIndex { get; private set; }

        /// <summary>
        /// IA manager.
        /// </summary>
        public IaManagerPivot IaManager { get; private set; }

        /// <summary>
        /// The game in which this instance happens.
        /// </summary>
        internal GamePivot Game { get; private set; }

        /// <summary>
        /// The player index where the wall is opened.
        /// </summary>
        public int WallOpeningIndex { get; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the current player is the human player.
        /// </summary>
        public bool IsHumanPlayer => CurrentPlayerIndex == GamePivot.HUMAN_INDEX && !Game.CpuVs;

        /// <summary>
        /// Inferred; indicates if the previous player is the human player.
        /// </summary>
        public bool PreviousIsHumanPlayer => PreviousPlayerIndex == GamePivot.HUMAN_INDEX && !Game.CpuVs;

        /// <summary>
        /// Inferred; indicates the index of the player before <see cref="CurrentPlayerIndex"/>.
        /// </summary>
        public int PreviousPlayerIndex => CurrentPlayerIndex.RelativePlayerIndex(-1);

        /// <summary>
        /// Inferred; indicates if the current round is over by wall exhaustion.
        /// </summary>
        public bool IsWallExhaustion => WallTiles.Count == 0;

        /// <summary>
        /// Inferred; count of visible doras.
        /// </summary>
        public int VisibleDorasCount => 1 + (4 - _compensationTiles.Count);

        /// <summary>
        /// All tiles from the treasure (concealed or not).
        /// </summary>
        public IReadOnlyList<TilePivot> AllTreasureTiles => DoraIndicatorTiles.Concat(UraDoraIndicatorTiles).Concat(CompensationTiles).Concat(DeadTreasureTiles).ToList();

        #endregion Inferred properties

        #region Events

        /// <summary>
        /// Event triggered when the tiles count in the wall changes.
        /// </summary>
        public event EventHandler NotifyWallCount;

        /// <summary>
        /// Event triggered when a tile is picked.
        /// </summary>
        public event TileEventHandler NotifyPick;

        #endregion Events

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game">The <see cref="Game"/> value.</param>
        /// <param name="firstPlayerIndex">The initial <see cref="CurrentPlayerIndex"/> value.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="firstPlayerIndex"/> value should be between <c>0</c> and <c>3</c>.</exception>
        internal RoundPivot(GamePivot game, int firstPlayerIndex)
        {
            if (firstPlayerIndex < 0 || firstPlayerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(firstPlayerIndex));
            }

            Game = game;

            WallOpeningIndex = GlobalTools.Randomizer.Next(0, 4);

            _fullTilesList = TilePivot
                                .GetCompleteSet(Game.Ruleset.UseRedDoras)
                                .OrderBy(t => GlobalTools.Randomizer.NextDouble())
                                .ToList();

            // Add below specific calls to sort the draw
            // DrivenDrawPivot.HumanTenpai(_fullTilesList);

            _hands = Enumerable.Range(0, 4).Select(i => new HandPivot(_fullTilesList.GetRange(i * 13, 13))).ToList();
            _discards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>(20)).ToList();
            _virtualDiscards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>(20)).ToList();
            _riichis = Enumerable.Range(0, 4).Select(i => (RiichiPivot)null).ToList();
            _wallTiles = _fullTilesList.GetRange(52, 70);
            _compensationTiles = _fullTilesList.GetRange(122, 4);
            _doraIndicatorTiles = _fullTilesList.GetRange(126, 5);
            _uraDoraIndicatorTiles = _fullTilesList.GetRange(131, 5);
            _deadTreasureTiles = new List<TilePivot>(14);
            CurrentPlayerIndex = firstPlayerIndex;
            _stealingInProgress = false;
            _closedKanInProgress = null;
            _openedKanInProgress = null;
            _waitForDiscard = false;
            _playerIndexHistory = new List<int>(10);
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

            var tile = _wallTiles.First();
            _wallTiles.Remove(tile);
            NotifyWallCount?.Invoke(null, null);
            _hands[CurrentPlayerIndex].Pick(tile);
            NotifyPick?.Invoke(new TileEventArgs(CurrentPlayerIndex, tile));
            _waitForDiscard = true;
            return tile;
        }

        /// <summary>
        /// Checks if calling chii is allowed for the specified player.
        /// </summary>
        /// <returns>
        /// A dictionnary, where each <see cref="KeyValuePair{TilePivot, Boolean}"/> is an indication of the chii which can be made:
        /// - The key is the first tile (ie the lowest number) of <see cref="HandPivot.ConcealedTiles"/> to use in the sequence.
        /// - The value indicates if the key is used as lowest number in the sequence (<c>False</c>) or second (<c>True</c>, ie the tile stolen is the lowest number).
        /// The list is empty if calling chii is impossible.
        /// </returns>
        public Dictionary<TilePivot, bool> CanCallChii()
        {
            if (_wallTiles.Count == 0 || _discards[PreviousPlayerIndex].Count == 0 || _waitForDiscard || IsRiichi(CurrentPlayerIndex))
            {
                return new Dictionary<TilePivot, bool>();
            }

            var tile = _discards[PreviousPlayerIndex].Last();
            if (tile.IsHonor)
            {
                return new Dictionary<TilePivot, bool>();
            }

            var potentialTiles =
                _hands[CurrentPlayerIndex]
                    .ConcealedTiles
                    .Where(t => t.Family == tile.Family && t.Number != tile.Number && (t.Number >= tile.Number - 2 || t.Number <= tile.Number + 2))
                    .Distinct()
                    .ToList();

            var tileRelativePositionMinus2 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number - 2);
            var tileRelativePositionMinus1 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number - 1);
            var tileRelativePositionBonus1 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number + 1);
            var tileRelativePositionBonus2 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number + 2);

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
            return _wallTiles.Count != 0
                && PreviousPlayerIndex != playerIndex
                && _discards[PreviousPlayerIndex].Count != 0
                && !_waitForDiscard && !IsRiichi(playerIndex)
                && _hands[playerIndex].ConcealedTiles.Where(t => t == _discards[PreviousPlayerIndex].Last()).Count() >= 2;
        }

        /// <summary>
        /// Checks if calling kan is allowed for the specified player in this context.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>A tile from every possible kans.</returns>
        public IReadOnlyList<TilePivot> CanCallKan(int playerIndex)
        {
            if (_compensationTiles.Count == 0 || _wallTiles.Count == 0)
            {
                return new List<TilePivot>();
            }

            if (CurrentPlayerIndex == playerIndex && _waitForDiscard)
            {
                var kansFromConcealed =
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
                    var disposableForRiichi = ExtractDiscardChoicesFromTenpai(playerIndex);
                    if (disposableForRiichi.Any(t => t != _hands[playerIndex].LatestPick))
                    {
                        return new List<TilePivot>();
                    }
                    kansFromConcealed = kansFromConcealed.Where(t => t == _hands[playerIndex].LatestPick);
                }

                var kansFromPons =
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

                var referenceTileFromDiscard = _discards[PreviousPlayerIndex].Last();
                return _hands[playerIndex].ConcealedTiles.Where(t => t == referenceTileFromDiscard).Count() >= 3
                    ? new List<TilePivot>
                    {
                        referenceTileFromDiscard
                    }
                    : new List<TilePivot>();
            }
        }

        /// <summary>
        /// Tries to call chii for the specified player.
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
                Game.GetPlayerCurrentWind(PreviousPlayerIndex),
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
                Game.GetPlayerCurrentWind(PreviousPlayerIndex)
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

            var fromPreviousPon = tileChoice == null ? null :
                _hands[playerIndex].DeclaredCombinations.FirstOrDefault(c => c.IsBrelan && c.OpenTile == tileChoice);

            if (CurrentPlayerIndex == playerIndex
                && _waitForDiscard
                && tileChoice != null
                && !_hands[playerIndex].ConcealedTiles.GroupBy(t => t).Any(t => t.Count() == 4 && t.Key == tileChoice)
                && fromPreviousPon == null)
            {
                throw new ArgumentException(Messages.InvalidKanTileChoice, nameof(tileChoice));
            }

            var isClosedKan = false;
            if (CurrentPlayerIndex == playerIndex && _waitForDiscard)
            {
                // Forces a decision, even if there're several possibilities.
                if (tileChoice == null)
                {
                    tileChoice = _hands[playerIndex].ConcealedTiles.GroupBy(t => t).FirstOrDefault(t => t.Count() == 4)?.Key;
                    if (tileChoice == null)
                    {
                        tileChoice = _hands[playerIndex].ConcealedTiles.First(t => _hands[playerIndex].DeclaredCombinations.Any(c => c.IsBrelan && c.OpenTile == t));
                        fromPreviousPon = _hands[playerIndex].DeclaredCombinations.First(c => c.OpenTile == tileChoice);
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
                    Game.GetPlayerCurrentWind(PreviousPlayerIndex),
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
        /// <param name="tile">The discarded tile.</param>
        /// <exception cref="InvalidOperationException"><see cref="Messages.UnexpectedDiscardFail"/></exception>
        public bool CallRiichi(TilePivot tile)
        {
            // Computes before discard, but proceeds after.
            // Otherwise, the discard will fail.
            var riichiTurnsCount = _discards[CurrentPlayerIndex].Count;
            var isUninterruptedFirstTurn = _discards[CurrentPlayerIndex].Count == 0 && IsUninterruptedHistory(CurrentPlayerIndex);

            if (!Discard(tile))
            {
                throw new InvalidOperationException(Messages.UnexpectedDiscardFail);
            }

            _riichis[PreviousPlayerIndex] = new RiichiPivot(riichiTurnsCount, isUninterruptedFirstTurn, tile,
                Enumerable.Range(0, 4).Where(i => i != PreviousPlayerIndex).Select(i => new KeyValuePair<int, int>(i, _virtualDiscards[i].Count)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
            Game.AddPendingRiichi(PreviousPlayerIndex);

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
        /// Checks if the current player can call riichi.
        /// </summary>
        /// <returns>Tiles the player can discard; empty list if riichi is impossible.</returns>
        public IReadOnlyList<TilePivot> CanCallRiichi()
        {
            if (!_waitForDiscard
                || IsRiichi(CurrentPlayerIndex)
                || !_hands[CurrentPlayerIndex].IsConcealed
                || _wallTiles.Count < 4
                || Game.Players.ElementAt(CurrentPlayerIndex).Points < ScoreTools.RIICHI_COST)
            {
                return new List<TilePivot>();
            }

            // TODO: if already 3 riichi calls, what to do ?

            return ExtractDiscardChoicesFromTenpai(CurrentPlayerIndex);
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
            var tile = _waitForDiscard ? null : _discards[PreviousPlayerIndex].LastOrDefault();
            var forKokushiOnly = false;
            var isChanka = false;
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

            return _hands[playerIndex].IsComplete
                && !_hands[playerIndex].CancelYakusIfFuriten(_discards[playerIndex], GetTilesFromVirtualDiscardsAtRank(playerIndex, tile))
                && !_hands[playerIndex].CancelYakusIfTemporaryFuriten(this, playerIndex);
        }

        /// <summary>
        /// Checks if the hand of the specified player is tenpai.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="tileToRemoveFromConcealed">A tile to remove from the hand first; only if <see cref="HandPivot.IsFullHand"/> is <c>True</c> for this hand.</param>
        /// <returns><c>True</c> if tenpai; <c>False</c> otherwise.</returns>
        /// <exception cref="ArgumentException">A tile to remove has to be specified in this context of hand.</exception>
        /// <exception cref="ArgumentException">A tile to remove can't be specified in this context of hand.</exception>
        public bool IsTenpai(int playerIndex, TilePivot tileToRemoveFromConcealed)
        {
            var hand = _hands[playerIndex];
            if (hand.IsFullHand && (tileToRemoveFromConcealed == null || !hand.ConcealedTiles.Contains(tileToRemoveFromConcealed)))
            {
                throw new ArgumentException("A tile to remove has to be specified in this context of hand.", nameof(tileToRemoveFromConcealed));
            }
            else if (!hand.IsFullHand && tileToRemoveFromConcealed != null)
            {
                throw new ArgumentException("A tile to remove can't be specified in this context of hand.", nameof(tileToRemoveFromConcealed));
            }

            // TODO : there're (maybe) specific rules about it:
            // for instance, what if I have a single wait on tile "4 circle" but every tiles "4 circle" are already in my hand ?
            return hand.IsTenpai(_fullTilesList, tileToRemoveFromConcealed);
        }

        /// <summary>
        /// Checks if the specified player is riichi.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <returns><c>True</c> if riichi; <c>False</c> otherwise.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerIndex"/> is out of range.</exception>
        public bool IsRiichi(int playerIndex)
        {
            return playerIndex < 0 || playerIndex > 3
                ? throw new ArgumentOutOfRangeException(nameof(playerIndex))
                : _riichis[playerIndex] != null;
        }

        /// <summary>
        /// Checks, for a specified player, if the specified rank is the one when the riichi call has been made.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="rank">The rank.</param>
        /// <returns><c>True</c> if the specified rank is the riichi one.</returns>
        public bool IsRiichiRank(int playerIndex, int rank)
        {
            return playerIndex < 0 || playerIndex > 3
                ? throw new ArgumentOutOfRangeException(nameof(playerIndex))
                : _riichis[playerIndex] != null && _riichis[playerIndex].DiscardRank == rank;
        }

        /// <summary>
        /// Checks if the human player can auto-discard.
        /// </summary>
        /// <returns><c>True</c> if he can; <c>False</c> otherwise.</returns>
        public bool HumanCanAutoDiscard()
        {
            return IsRiichi(GamePivot.HUMAN_INDEX)
                && (Game.CpuVs || CanCallKan(GamePivot.HUMAN_INDEX).Count == 0)
                && _waitForDiscard;
        }

        /// <summary>
        /// Undoes the pick of a compensation tile after a kan.
        /// </summary>
        public void UndoPickCompensationTile()
        {
            var compensationTile = _closedKanInProgress ?? _openedKanInProgress;
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
        /// <param name="isSelfKan">If the method returns <c>True</c>, this indicates a self kan if <c>True</c>.</param>
        /// <returns><c>True</c> if call available; <c>False otherwise</c>.</returns>
        public bool CanCallPonOrKan(int playerIndex, out bool isSelfKan)
        {
            isSelfKan = _waitForDiscard;
            return CanCallKan(playerIndex).Count > 0 || CanCallPon(playerIndex);
        }

        /// <summary>
        /// Checks if a kan call can be made by any opponent of the human player.
        /// </summary>
        /// <param name="concealed"><c>True</c> to check only concealed kan (or from a previous pon); <c>False</c> to check the opposite; <c>Null</c> for both.</param>
        /// <returns>The player index who can make the kan call, and the possible tiles; <c>Null</c> is none.</returns>
        public Tuple<int, IReadOnlyList<TilePivot>> OpponentsCanCallKan(bool? concealed)
        {
            for (var i = 0; i < 4; i++)
            {
                if (i != GamePivot.HUMAN_INDEX || Game.CpuVs)
                {
                    var kanTiles = CanCallKanWithChoices(i, concealed);
                    if (kanTiles.Count > 0)
                    {
                        return new Tuple<int, IReadOnlyList<TilePivot>>(i, kanTiles);
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
        public IReadOnlyList<TilePivot> CanCallKanWithChoices(int playerId, bool? concealed)
        {
            var tiles = CanCallKan(playerId);
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
            var opponentsIndex = Enumerable.Range(0, 4).Where(i =>
            {
                return (i != GamePivot.HUMAN_INDEX || Game.CpuVs) && CanCallPon(i);
            }).ToList();

            return opponentsIndex.Count > 0 ? opponentsIndex[0] : -1;
        }

        /// <summary>
        /// Checks if a chii call can be made by any opponent of the human player.
        /// </summary>
        /// <returns>Same type of return than the method <see cref="CanCallChii()"/>, for the opponent who can call chii.</returns>
        public Dictionary<TilePivot, bool> OpponentsCanCallChii()
        {
            if (!IsHumanPlayer)
            {
                var chiiTiles = CanCallChii();
                if (chiiTiles.Count > 0)
                {
                    return chiiTiles;
                }
            }

            return new Dictionary<TilePivot, bool>();
        }

        /// <summary>
        /// Checks if the specified tile is allowed for discard for the current player.
        /// </summary>
        /// <param name="tile">The tile to check.</param>
        /// <returns><c>True</c> if the tile is discardable; <c>False</c> otherwise.</returns>
        public bool CanDiscard(TilePivot tile)
        {
            return _waitForDiscard && (!IsRiichi(CurrentPlayerIndex) || ReferenceEquals(tile, _hands[CurrentPlayerIndex].LatestPick))
&& _hands[CurrentPlayerIndex].CanDiscardTile(tile, _stealingInProgress);
        }

        /// <summary>
        /// Gets the discard of a specified player.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <returns>Collection of discarded <see cref="TilePivot"/> instances.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerIndex"/> should be between 0 and 3.</exception>
        public IReadOnlyList<TilePivot> GetDiscard(int playerIndex)
        {
            CheckPlayerIndex(playerIndex);

            return _discards[playerIndex];
        }

        /// <summary>
        /// Gets the hand of a specified player.
        /// </summary>
        /// <param name="playerIndex">Player index.</param>
        /// <returns>Instance of <see cref="HandPivot"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerIndex"/> should be between 0 and 3.</exception>
        public HandPivot GetHand(int playerIndex)
        {
            return _hands[playerIndex];
        }

        #endregion Public methods

        #region Private methods

        // Gets every tiles from every opponents virtual discards after the riichi call of the specified player.
        private IReadOnlyList<TilePivot> GetTilesFromVirtualDiscardsAtRank(int riichiPlayerIndex, TilePivot exceptTile)
        {
            var fullList = new List<TilePivot>(20);

            if (_riichis[riichiPlayerIndex] == null)
            {
                return fullList;
            }

            for (var i = 0; i < 4; i++)
            {
                if (i != riichiPlayerIndex)
                {
                    var opponentRank = _riichis[riichiPlayerIndex].OpponentsVirtualDiscardRank[i];
                    fullList.AddRange(_virtualDiscards[i].Skip(opponentRank));
                }
            }

            return fullList.Where(t => !ReferenceEquals(t, exceptTile)).ToList();
        }

        // Picks a compensation tile (after a kan call) for the current player.
        private TilePivot PickCompensationTile(bool isClosedKan)
        {
            var compensationTile = _compensationTiles.First();
            _compensationTiles.RemoveAt(0);

            _deadTreasureTiles.Add(_wallTiles.Last());

            _wallTiles.RemoveAt(_wallTiles.Count - 1);
            NotifyWallCount?.Invoke(null, null);

            _hands[CurrentPlayerIndex].Pick(compensationTile);
            NotifyPick?.Invoke(new TileEventArgs(CurrentPlayerIndex, compensationTile));

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
            var historySinceLastTime = _playerIndexHistory.TakeWhile(i => i != playerIndex).ToList();

            var rank = 1;
            for (var i = (historySinceLastTime.Count - 1); i >= 0; i--)
            {
                var nextPIndex = playerIndex.RelativePlayerIndex(rank);
                if (nextPIndex != historySinceLastTime[i])
                {
                    return false;
                }
                rank++;
            }

            return true;
        }

        // Creates the context and calls "SetYakus" for the specified player.
        private void SetYakus(int playerIndex, TilePivot tile, DrawTypePivot drawType)
        {
            _hands[playerIndex].SetYakus(new WinContextPivot(
                latestTile: tile,
                drawType: drawType,
                dominantWind: Game.DominantWind,
                playerWind: Game.GetPlayerCurrentWind(playerIndex),
                isFirstOrLast: IsWallExhaustion ? (bool?)null : (_discards[playerIndex].Count == 0 && IsUninterruptedHistory(playerIndex)),
                isRiichi: IsRiichi(playerIndex) ? (_riichis[playerIndex].IsDaburu ? (bool?)null : true) : false,
                isIppatsu: IsIppatsu(playerIndex)
            ));
        }

        // Checks if the specified player is ippatsu.
        private bool IsIppatsu(int playerIndex)
        {
            return IsRiichi(playerIndex)
                && _discards[playerIndex].Count > 0
                && ReferenceEquals(_discards[playerIndex].Last(), _riichis[playerIndex].Tile)
                && IsUninterruptedHistory(playerIndex);
        }

        // Gets the concealed tile of the round from the point of view of a specified player.
        private IReadOnlyList<TilePivot> GetConcealedTilesFromPlayerPointOfView(int playerIndex)
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
        private IReadOnlyList<int> CheckForNagashiMangan()
        {
            var playerIndexList = new List<int>(4);

            for (var i = 0; i < 4; i++)
            {
                var fullTerminalsOrHonors = _discards[i].All(t => t.IsHonorOrTerminal);
                var noPlayerStealing = _hands[i].IsConcealed;
                var noOpponentStealing = !_hands.Where(h => _hands.IndexOf(h) != i).Any(h => h.DeclaredCombinations.Any(c => c.StolenFrom == Game.GetPlayerCurrentWind(i)));
                if (fullTerminalsOrHonors && noPlayerStealing && noOpponentStealing)
                {
                    _hands[i].SetYakus(new WinContextPivot());
                    playerIndexList.Add(i);
                }
            }

            return playerIndexList;
        }

        // Checks for playerIndex argument validity.
        private void CheckPlayerIndex(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex), playerIndex, "Player index should be between 0 and 3.");
            }
        }

        // Gets the count of dora for specified tile
        private int GetDoraCountInternal(TilePivot t, IEnumerable<TilePivot> doraIndicators)
        {
            return doraIndicators.Take(VisibleDorasCount).Count(d => t.IsDoraNext(d));
        }

        #endregion Private methods

        #region Internal methods

        /// <summary>
        /// Checks if the hand of the specified player is tenpai and list tiles which can be discarded.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>The list of tiles which can be discarded.</returns>
        internal IReadOnlyList<TilePivot> ExtractDiscardChoicesFromTenpai(int playerIndex)
        {
            var distinctTilesFromOverallConcealed = GetConcealedTilesFromPlayerPointOfView(playerIndex).Distinct().ToList();

            var subPossibilities = new ConcurrentBag<TilePivot>();
            _hands[playerIndex].ConcealedTiles
                .Distinct()
                .ToList()
                .ExecuteInParallel(tileToSub =>
                {
                    var tempListConcealed = new List<TilePivot>(_hands[playerIndex].ConcealedTiles);
                    tempListConcealed.Remove(tileToSub);
                    if (HandPivot.IsTenpai(tempListConcealed, _hands[playerIndex].DeclaredCombinations, distinctTilesFromOverallConcealed))
                    {
                        subPossibilities.Add(tileToSub);
                    }
                });

            // Avoids red doras in the list returned (if possible).
            var realSubPossibilities = new List<TilePivot>(subPossibilities.Count);
            foreach (var tile in subPossibilities.Distinct())
            {
                TilePivot subTile = null;
                if (tile.IsRedDora)
                {
                    subTile = _hands[playerIndex].ConcealedTiles.FirstOrDefault(t => t == tile && !t.IsRedDora);
                }

                if (!realSubPossibilities.Contains(subTile ?? tile))
                    realSubPossibilities.Add(subTile ?? tile);
            }

            return realSubPossibilities;
        }

        /// <summary>
        /// Manages the end of a round.
        /// </summary>
        /// <param name="ronPlayerIndex">The player index on who the call has been made; <c>Null</c> if tsumo or ryuukyoku.</param>
        /// <returns>An instance of <see cref="EndOfRoundInformationsPivot"/>.</returns>
        internal EndOfRoundInformationsPivot EndOfRound(int? ronPlayerIndex)
        {
            var turnWind = false;
            var ryuukyoku = true;
            var displayUraDoraTiles = false;

            var winners = _hands.Where(h => h.IsComplete).Select(w => _hands.IndexOf(w)).ToList();

            if (winners.Count == 0 && Game.Ruleset.UseNagashiMangan)
            {
                var iNagashiList = CheckForNagashiMangan();
                if (iNagashiList.Count > 0)
                {
                    winners.AddRange(iNagashiList);
                }
            }

            var playerInfos = new List<EndOfRoundInformationsPivot.PlayerInformationsPivot>(4);

            // Ryuukyoku (no winner).
            if (winners.Count == 0)
            {
                var tenpaiPlayersIndex = Enumerable.Range(0, 4).Where(i => IsTenpai(i, null)).ToList();
                var notTenpaiPlayersIndex = Enumerable.Range(0, 4).Except(tenpaiPlayersIndex).ToList();

                // Wind turns if East is not tenpai.
                turnWind = notTenpaiPlayersIndex.Any(tpi => Game.GetPlayerCurrentWind(tpi) == WindPivot.East);

                var points = ScoreTools.GetRyuukyokuPoints(tenpaiPlayersIndex.Count);

                tenpaiPlayersIndex.ForEach(i => playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(i, 0, 0, _hands[i], points.Item1, 0, 0, 0, points.Item1)));
                notTenpaiPlayersIndex.ForEach(i => playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(i, points.Item2)));
            }
            else
            {
                turnWind = !winners.Any(w => Game.GetPlayerCurrentWind(w) == WindPivot.East);

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
                var liablePlayersLost = new Dictionary<int, int>();

                // These two are negative points.
                var eastOrLoserLostCumul = 0;
                var notEastLostCumul = 0;

                foreach (var pIndex in winners)
                {
                    var phand = _hands[pIndex];

                    // In case of ron, fix the "LatestPick" property of the winning hand
                    if (ronPlayerIndex.HasValue)
                    {
                        phand.SetFromRon(_discards[ronPlayerIndex.Value].Last());
                    }

                    int? liablePlayerId = null;
                    if (phand.Yakus.Contains(YakuPivot.Daisangen)
                        && phand.DeclaredCombinations.Count(c => c.Family == FamilyPivot.Dragon) == 3
                        && phand.DeclaredCombinations.Last(c => c.Family == FamilyPivot.Dragon).StolenFrom.HasValue)
                    {
                        liablePlayerId = Game.GetPlayerIndexByCurrentWind(phand.DeclaredCombinations.Last(c => c.Family == FamilyPivot.Dragon).StolenFrom.Value);
                    }
                    else if (phand.Yakus.Contains(YakuPivot.Daisuushii)
                        && phand.DeclaredCombinations.Count(c => c.Family == FamilyPivot.Wind) == 4
                        && phand.DeclaredCombinations.Last(c => c.Family == FamilyPivot.Wind).StolenFrom.HasValue)
                    {
                        liablePlayerId = Game.GetPlayerIndexByCurrentWind(phand.DeclaredCombinations.Last(c => c.Family == FamilyPivot.Wind).StolenFrom.Value);
                    }

                    var isRiichi = phand.Yakus.Contains(YakuPivot.Riichi) || phand.Yakus.Contains(YakuPivot.DaburuRiichi);

                    var dorasCount = phand.AllTiles.Sum(t => GetDoraCount(t));
                    var uraDorasCount = isRiichi ? phand.AllTiles.Sum(t => GetUraDoraCount(t)) : 0;
                    var redDorasCount = phand.AllTiles.Count(t => t.IsRedDora);

                    if (isRiichi)
                    {
                        displayUraDoraTiles = true;
                    }

                    var fanCount = ScoreTools.GetFanCount(phand.Yakus, phand.IsConcealed, dorasCount, uraDorasCount, redDorasCount);
                    var fuCount = ScoreTools.GetFuCount(phand, !ronPlayerIndex.HasValue, Game.DominantWind, Game.GetPlayerCurrentWind(pIndex));

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

                    var finalScore = ScoreTools.GetPoints(fanCount, fuCount, !ronPlayerIndex.HasValue, Game.GetPlayerCurrentWind(pIndex));

                    var basePoints = finalScore.Item1 + finalScore.Item2 * 2;

                    var riichiPart = Game.PendingRiichiCount * ScoreTools.RIICHI_COST;

                    var honbaPoints = ScoreTools.GetHonbaPoints(Game.HonbaCountBeforeScoring, winners.Count, !ronPlayerIndex.HasValue);

                    // In case of ron with multiple winners, only the one who comes right next to "ronPlayerIndex" takes the stack of riichi.
                    if (winners.Count > 1)
                    {
                        for (var i = 1; i <= 3; i++)
                        {
                            var nextPlayerId = ronPlayerIndex.Value.RelativePlayerIndex(i);
                            if (winners.Contains(nextPlayerId) && pIndex != nextPlayerId)
                            {
                                riichiPart = 0;
                            }
                        }
                    }

                    playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                        pIndex, fanCount, fuCount, phand, basePoints + riichiPart + honbaPoints,
                        dorasCount, uraDorasCount, redDorasCount, basePoints));

                    notEastLostCumul -= finalScore.Item2;

                    // If there's is a liable player (only in a case of ron on other player than the one liable)...
                    if (liablePlayerId.HasValue)
                    {
                        if (!liablePlayersLost.ContainsKey(liablePlayerId.Value))
                        {
                            liablePlayersLost.Add(liablePlayerId.Value, 0);
                        }
                        // ... he takes half of the points from the ron player for this hand.
                        eastOrLoserLostCumul -= finalScore.Item1 / 2;
                        liablePlayersLost[liablePlayerId.Value] -= finalScore.Item1 / 2;
                    }
                    else
                    {
                        // Otherwise, the ron player takes all.
                        eastOrLoserLostCumul -= finalScore.Item1;
                    }
                }

                // Note : "liablePlayersLost" is empty in case of tsumo transformed into ron.
                if (liablePlayersLost.Count > 0)
                {
                    var pointsNotOnRonPlayer = 0;
                    foreach (var liablePlayerId in liablePlayersLost.Keys)
                    {
                        pointsNotOnRonPlayer += (liablePlayerId != ronPlayerIndex.Value ? liablePlayersLost[liablePlayerId] : 0);
                        if (playerInfos.Any(pi => pi.Index == liablePlayerId))
                        {
                            playerInfos.First(pi => pi.Index == liablePlayerId).AddPoints(liablePlayersLost[liablePlayerId]);
                        }
                        else
                        {
                            playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                                liablePlayerId, liablePlayersLost[liablePlayerId]));
                        }
                    }
                    if (playerInfos.Any(pi => pi.Index == ronPlayerIndex.Value))
                    {
                        playerInfos.First(pi => pi.Index == ronPlayerIndex.Value).AddPoints(-pointsNotOnRonPlayer);
                    }
                    else
                    {
                        playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                            ronPlayerIndex.Value, eastOrLoserLostCumul - pointsNotOnRonPlayer));
                    }
                }
                else if (ronPlayerIndex.HasValue)
                {
                    playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(ronPlayerIndex.Value, eastOrLoserLostCumul));
                }
                else
                {
                    for (var pIndex = 0; pIndex < 4; pIndex++)
                    {
                        if (!winners.Contains(pIndex))
                        {
                            playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(pIndex, Game.GetPlayerCurrentWind(pIndex) == WindPivot.East ? eastOrLoserLostCumul : notEastLostCumul));
                        }
                    }
                }

                ryuukyoku = false;
            }

            foreach (var p in playerInfos)
            {
                Game.Players.ElementAt(p.Index).AddPoints(p.PointsGain);
            }

            return new EndOfRoundInformationsPivot(ryuukyoku, turnWind, displayUraDoraTiles, playerInfos, Game.HonbaCountBeforeScoring,
                Game.PendingRiichiCount, DoraIndicatorTiles, UraDoraIndicatorTiles, VisibleDorasCount);
        }

        /// <summary>
        /// Computes the list of every tiles whose fate is sealed from the point of view of a specific player.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>Tiles enumeration.</returns>
        internal IEnumerable<TilePivot> DeadTilesFromIndexPointOfView(int playerIndex)
        {
            return _fullTilesList.Except(GetConcealedTilesFromPlayerPointOfView(playerIndex));
        }

        // Gets dora count if the specvified tile is a dora
        internal int GetDoraCount(TilePivot t) => GetDoraCountInternal(t, DoraIndicatorTiles);

        // Gets dora count if the specvified tile is an uradora
        internal int GetUraDoraCount(TilePivot t) => GetDoraCountInternal(t, UraDoraIndicatorTiles);

        #endregion Internal methods
    }
}