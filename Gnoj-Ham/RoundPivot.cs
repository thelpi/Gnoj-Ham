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
        private readonly List<int> playerIndexHistory;

        private readonly List<TilePivot> _wallTiles;
        private readonly List<HandPivot> _hands;
        private readonly List<TilePivot> _compensationTiles;
        private readonly List<TilePivot> _doraIndicatorTiles;
        private readonly List<TilePivot> _uraDoraIndicatorTiles;
        private readonly List<TilePivot> _deadTreasureTiles;
        private readonly List<List<TilePivot>> _discards;
        private readonly List<Tuple<int, TilePivot, bool>> _riichis;
        private readonly List<TilePivot> _fullTilesList;

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
        /// Riichi informations of four players. The <see cref="Tuple{T1, T2, T3}"/> contains :
        /// - The index in the discard.
        /// - The tile used to call riichi.
        /// - A boolean indicating a "daburu" riichi, always at index <c>0</c> (but the reverse is not always true).
        /// </summary>
        /// <remarks>The list if filled by default with a tuple [-1 / null / false] for every players.</remarks>
        public IReadOnlyCollection<Tuple<int, TilePivot, bool>> Riichis
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

            _fullTilesList = TilePivot.GetCompleteSet(withRedDoras).OrderBy(t => GlobalTools.Randomizer.NextDouble()).ToList();

            _hands = Enumerable.Range(0, 4).Select(i => new HandPivot(_fullTilesList.GetRange(i * 13, 13))).ToList();
            _discards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>()).ToList();
            _riichis = Enumerable.Range(0, 4).Select(i => new Tuple<int, TilePivot, bool>(-1, null, false)).ToList();
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
            playerIndexHistory = new List<int>();

            _game = game;
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
            TilePivot tile = PickCompensationTile();

            if (isClosedKan)
            {
                _closedKanInProgress = tile;
            }
            else
            {
                _openedKanInProgress = tile;
            }

            return tile;
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
            
            _riichis[playerIndex] = new Tuple<int, TilePivot, bool>(riichiTurnsCount, tile, isUninterruptedFirstTurn);

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
            if (!_waitForDiscard || (IsRiichi(CurrentPlayerIndex) && !ReferenceEquals(tile, _hands[CurrentPlayerIndex].LatestPick)))
            {
                return false;
            }

            if (!_hands[CurrentPlayerIndex].Discard(tile, _stealingInProgress))
            {
                return false;
            }

            if (_stealingInProgress || _closedKanInProgress != null)
            {
                playerIndexHistory.Clear();
            }

            _discards[CurrentPlayerIndex].Add(tile);
            _stealingInProgress = false;
            _closedKanInProgress = null;
            _openedKanInProgress = null;
            _waitForDiscard = false;
            playerIndexHistory.Insert(0, CurrentPlayerIndex);
            CurrentPlayerIndex = CurrentPlayerIndex.RelativePlayerIndex(1);
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
            if (Pick() == null)
            {
                return false;
            }

            // Discards a random tile.
            // Can't fail as it's never from a stolen call.
            Discard(_hands[CurrentPlayerIndex].ConcealedTiles.Skip(GlobalTools.Randomizer.Next(0, _hands[CurrentPlayerIndex].ConcealedTiles.Count)).First());

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
                || _game.Players.ElementAt(playerIndex).Points < 1000)
            {
                return new List<TilePivot>();
            }

            // TODO: if already 3 riichi calls, what to do ?

            return ExtractRiichiPossibilities(playerIndex);
        }

        /// <summary>
        /// Checks if the hand of the specified player is ready for calling tsumo.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="isKanCompensation"><c>True</c> if the latest pick comes from a kan compensation.</param>
        /// <returns>The optimal combination of <see cref="YakuPivot"/>; empty list if none.</returns>
        public List<YakuPivot> CanCallTsumo(int playerIndex, bool isKanCompensation)
        {
            if (CurrentPlayerIndex != playerIndex || !_waitForDiscard)
            {
                return new List<YakuPivot>();
            }
            
            List<List<YakuPivot>> yakus = GetYakus(playerIndex,
                _hands[playerIndex].LatestPick,
                isKanCompensation ? DrawTypePivot.Compensation : DrawTypePivot.Wall);

            return YakuPivot.GetBestYakusFromList(yakus, _hands[playerIndex].IsConcealed);
        }

        /// <summary>
        /// Checks if the hand of the specified player is ready for calling ron.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <returns>The optimal combination of <see cref="YakuPivot"/>; empty list if none.</returns>
        public List<YakuPivot> CanCallRon(int playerIndex)
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
                return new List<YakuPivot>();
            }

            List<List<YakuPivot>> yakus = GetYakus(playerIndex, tile,
                isChanka ? DrawTypePivot.OpponentKanCall : DrawTypePivot.OpponentDiscard);

            if (yakus.Count == 0)
            {
                return new List<YakuPivot>();
            }

            if (forKokushiOnly)
            {
                // Ron on closed kan is only possible for kokushi musou.
                yakus.RemoveAll(ys => !ys.Any(y => y == YakuPivot.KokushiMusou));
            }

            // Furiten
            if (_discards[playerIndex].Any(t =>
                HandPivot.IsCompleteFull(new List<TilePivot>(_hands[playerIndex].ConcealedTiles) { t },
                    _hands[playerIndex].DeclaredCombinations.ToList())))
            {
                return new List<YakuPivot>();
            }

            // Temporary furiten
            int i = 0;
            while (playerIndexHistory[i] == playerIndex.RelativePlayerIndex(-(i + 1))
                && playerIndex.RelativePlayerIndex(-(i + 1)) != playerIndex)
            {
                TilePivot lastFromDiscard = _discards[playerIndexHistory[i]].LastOrDefault();
                if (lastFromDiscard != null && HandPivot.IsCompleteFull(
                    new List<TilePivot>(_hands[playerIndex].ConcealedTiles) { lastFromDiscard },
                    _hands[playerIndex].DeclaredCombinations.ToList()))
                {
                    return new List<YakuPivot>();
                }
                i++;
            }

            return YakuPivot.GetBestYakusFromList(yakus, _hands[CurrentPlayerIndex].IsConcealed);
        }

        /// <summary>
        /// Indicates if the context is a skippable move from a CPU player.
        /// </summary>
        /// <param name="skipCurrentAction"><c>True</c> to force call skip.</param>
        /// <returns><c>True</c> if skippable; <c>False</c> otherwise.</returns>
        public bool IsCpuSkippable(bool skipCurrentAction)
        {
            return !IsWallExhaustion
                && !IsHumanPlayer
                && (skipCurrentAction || (
                    CanCallKan(GamePivot.HUMAN_INDEX).Count == 0
                    && !CanCallPon(GamePivot.HUMAN_INDEX)
                    && CanCallChii(GamePivot.HUMAN_INDEX).Keys.Count == 0
                ));
        }

        /// <summary>
        /// Indicates if the context is human player ready to pick.
        /// </summary>
        /// <param name="skipCurrentAction"><c>True</c> to force call skip.</param>
        /// <returns><c>True</c> if the context is human player before pick.</returns>
        public bool IsHumanTurnBeforePick(bool skipCurrentAction)
        {
            return !IsWallExhaustion
                && IsHumanPlayer
                && (
                    skipCurrentAction
                    || _game.Round.CanCallChii(GamePivot.HUMAN_INDEX).Keys.Count == 0
                );
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

        #endregion Public methods

        #region Private methods

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
        private TilePivot PickCompensationTile()
        {
            TilePivot compensationTile = _compensationTiles.First();
            _compensationTiles.RemoveAt(0);
            _deadTreasureTiles.Add(_wallTiles.Last());
            _wallTiles.RemoveAt(_wallTiles.Count - 1);
            _hands[CurrentPlayerIndex].Pick(compensationTile);
            return compensationTile;
        }

        // Checks there's no call interruption since the latest move of the specified player.
        private bool IsUninterruptedHistory(int playerIndex)
        {
            List<int> historySinceLastTime = playerIndexHistory.TakeWhile(i => i != playerIndex).ToList();

            return historySinceLastTime.Count <= 3 && Enumerable.Range(0, 3).All(i => historySinceLastTime.Count <= i || historySinceLastTime[i] == playerIndex.RelativePlayerIndex(-(i + 1)));
        }

        // Creates the context and calls "GetYakus" for the specified player.
        private List<List<YakuPivot>> GetYakus(int playerIndex, TilePivot tile, DrawTypePivot drawType)
        {
            return _hands[playerIndex].GetYakus(new WinContextPivot(
                latestTile: tile,
                drawType: drawType,
                dominantWind: _game.DominantWind,
                playerWind: _game.GetPlayerCurrentWind(playerIndex),
                isFirstOrLast: IsWallExhaustion ? (bool?)null : (_discards[playerIndex].Count == 0 && IsUninterruptedHistory(playerIndex)),
                isRiichi: IsRiichi(playerIndex) ? (_riichis[playerIndex].Item3 ? (bool?)null : true) : false,
                isIppatsu: IsRiichi(playerIndex) && _discards[playerIndex].Count > 0 && ReferenceEquals(_discards[playerIndex].Last(), _riichis[playerIndex].Item2) && IsUninterruptedHistory(playerIndex)
            ));
        }

        // tests if the specified player is riichi.
        private bool IsRiichi(int playerIndex)
        {
            return _riichis[playerIndex].Item1 >= 0;
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

        #endregion Private methods
    }
}
