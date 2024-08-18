using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_Library.Events;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a round in a game.
/// </summary>
public class RoundPivot
{
    #region Embedded properties

    private bool _stealingInProgress;
    private TilePivot? _closedKanInProgress;
    private TilePivot? _openedKanInProgress;
    private bool _waitForDiscard;
    private readonly List<PlayerIndices> _playerIndexHistory;
    private readonly List<TilePivot> _wallTiles;
    private readonly List<HandPivot> _hands;
    private readonly List<TilePivot> _compensationTiles;
    private readonly List<TilePivot> _doraIndicatorTiles;
    private readonly List<TilePivot> _uraDoraIndicatorTiles;
    private readonly List<TilePivot> _deadTreasureTiles;
    private readonly List<List<TilePivot>> _discards;
    private readonly List<List<TilePivot>> _virtualDiscards;
    private readonly List<RiichiPivot?> _riichis;
    private readonly List<TilePivot> _fullTilesList;

    /// <summary>
    /// All tiles.
    /// </summary>
    public IReadOnlyList<TilePivot> FullTilesList => _fullTilesList;

    /// <summary>
    /// History of the latest players to play.
    /// First on the list is the latest to play.
    /// The list is cleared when a jump (ie a call) is made.
    /// </summary>
    internal IReadOnlyList<PlayerIndices> PlayerIndexHistory => _playerIndexHistory;

    /// <summary>
    /// Wall tiles.
    /// </summary>
    public IReadOnlyList<TilePivot> WallTiles => _wallTiles;

    /// <summary>
    /// List of compensation tiles. 4 at the beginning, between 0 and 4 at the end.
    /// </summary>
    internal IReadOnlyList<TilePivot> CompensationTiles => _compensationTiles;

    /// <summary>
    /// List of dora indicator tiles. Always 5 (doesn't mean they're all visible).
    /// </summary>
    public IReadOnlyList<TilePivot> DoraIndicatorTiles => _doraIndicatorTiles;

    /// <summary>
    /// List of ura-dora indicator tiles. Always 5 (doesn't mean they're all visible).
    /// </summary>
    internal IReadOnlyList<TilePivot> UraDoraIndicatorTiles => _uraDoraIndicatorTiles;

    /// <summary>
    /// Other tiles of the treasure Always 4 minus the number of tiles of <see cref="_compensationTiles"/>.
    /// </summary>
    internal IReadOnlyList<TilePivot> DeadTreasureTiles => _deadTreasureTiles;

    /// <summary>
    /// Riichi informations of four players.
    /// </summary>
    /// <remarks>The list if filled by default with <c>Null</c> for every players.</remarks>
    internal IReadOnlyList<RiichiPivot?> Riichis => _riichis;

    /// <summary>
    /// The current player index.
    /// </summary>
    public PlayerIndices CurrentPlayerIndex { get; private set; }

    /// <summary>
    /// IA manager.
    /// </summary>
    public CpuManagerBasePivot IaManager { get; }

    /// <summary>
    /// Advisor.
    /// </summary>
    public CpuManagerBasePivot Advisor { get; }

    /// <summary>
    /// The game in which this instance happens.
    /// </summary>
    internal GamePivot Game { get; }

    /// <summary>
    /// The player index where the wall is opened.
    /// </summary>
    public PlayerIndices WallOpeningIndex { get; }

    #endregion Embedded properties

    #region Inferred properties

    /// <summary>
    /// Inferred; indicates if the current player is the human player.
    /// </summary>
    public bool IsHumanPlayer => Game.IsHuman(CurrentPlayerIndex);

    /// <summary>
    /// Inferred; indicates if the previous player is the human player.
    /// </summary>
    public bool PreviousIsHumanPlayer => Game.IsHuman(PreviousPlayerIndex);

    /// <summary>
    /// Inferred; indicates the index of the player before <see cref="CurrentPlayerIndex"/>.
    /// </summary>
    public PlayerIndices PreviousPlayerIndex => CurrentPlayerIndex.RelativePlayerIndex(-1);

    /// <summary>
    /// Inferred; indicates if the current round is over by wall exhaustion.
    /// </summary>
    internal bool IsWallExhaustion => WallTiles.Count == 0;

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
    public event Action? NotifyWallCount;

    /// <summary>
    /// Event triggered when a tile is picked.
    /// </summary>
    public event Action<PickTileEventArgs>? NotifyPick;

    /// <summary>
    /// Event to notify <see cref="HumanCallNotifierEventArgs"/>.
    /// </summary>
    public event Action<HumanCallNotifierEventArgs>? HumanCallNotifier;

    /// <summary>
    /// Event to notify <see cref="DiscardTileNotifierEventArgs"/>.
    /// </summary>
    public event Action<DiscardTileNotifierEventArgs>? DiscardTileNotifier;

    /// <summary>
    /// Event to notify <see cref="CallNotifierEventArgs"/>.
    /// </summary>
    public event Action<CallNotifierEventArgs>? CallNotifier;

    /// <summary>
    /// Event to notify <see cref="ReadyToCallNotifierEventArgs"/>.
    /// </summary>
    public event Action<ReadyToCallNotifierEventArgs>? ReadyToCallNotifier;

    /// <summary>
    /// Event to notify <see cref="TurnChangeNotifierEventArgs"/>.
    /// </summary>
    public event Action<TurnChangeNotifierEventArgs>? TurnChangeNotifier;

    /// <summary>
    /// Event to notify <see cref="PickNotifierEventArgs"/>.
    /// </summary>
    public event Action<PickNotifierEventArgs>? PickNotifier;

    /// <summary>
    /// Event to notify <see cref="RiichiChoicesNotifierEventArgs"/>.
    /// </summary>
    public event Action<RiichiChoicesNotifierEventArgs>? RiichiChoicesNotifier;

    #endregion Events

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> value.</param>
    /// <param name="firstPlayerIndex">The initial <see cref="CurrentPlayerIndex"/> value.</param>
    /// <param name="random">Randomizer instance.</param>
    internal RoundPivot(GamePivot game, PlayerIndices firstPlayerIndex, Random random)
    {
        Game = game;

        WallOpeningIndex = (PlayerIndices)random.Next(0, 4);

        _fullTilesList = TilePivot
            .GetCompleteSet(Game.Ruleset.UseRedDoras)
            .OrderBy(t => random.NextDouble())
            .ToList();

        // Add below specific calls to sort the draw
        // DrivenDrawPivot.HumanTenpai(_fullTilesList);

        _hands = Enumerable.Range(0, 4).Select(i => new HandPivot(_fullTilesList.GetRange(i * 13, 13))).ToList();
        _discards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>(20)).ToList();
        _virtualDiscards = Enumerable.Range(0, 4).Select(i => new List<TilePivot>(20)).ToList();
        _riichis = Enumerable.Range(0, 4).Select(i => (RiichiPivot?)null).ToList();
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
        _playerIndexHistory = new List<PlayerIndices>(10);
        IaManager = new BasicCpuManagerPivot(this);
        Advisor = new BasicCpuManagerPivot(this);
    }

    #endregion Constructors

    #region Public methods

    /// <summary>
    /// Starts and runs the auto player.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="AutoPlayResultPivot"/>.</returns>>
    public AutoPlayResultPivot RunAutoPlay(CancellationToken cancellationToken)
        => RunAutoPlay(cancellationToken, false, false, false, 0);

    /// <summary>
    /// Starts and runs the auto player.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="declinedHumanCall">Indicates that a potential call has been suggested to the human player and has been declined..</param>
    /// <param name="humanRonPending">Indicates that the human player has called 'Ron', but the same call by opponents has to be checked too.</param>
    /// <param name="autoCallMahjong">When enabled, if the human player can call 'Tsumo' or 'Ron', the call is automatically made.</param>
    /// <param name="sleepTime">The time to wait after any action (call or discard).</param>
    /// <returns>Instance of <see cref="AutoPlayResultPivot"/>.</returns>>
    public AutoPlayResultPivot RunAutoPlay(
        CancellationToken cancellationToken,
        bool declinedHumanCall,
        bool humanRonPending,
        bool autoCallMahjong,
        int sleepTime)
    {
        (PlayerIndices, TilePivot?, PlayerIndices?)? kanInProgress = null;
        var result = new AutoPlayResultPivot();
        var isFirstTurn = true;
        while (!cancellationToken.IsCancellationRequested)
        {
            // 0 - after one loop, there is no human decline remaining
            if (!isFirstTurn)
            {
                declinedHumanCall = false;
            }
            isFirstTurn = false;

            // 1 - checks if human (we have not checked yet) can call "ron"; the loop ends if it's the case
            if (Game.HumanPlayerIndex.HasValue && !declinedHumanCall && !humanRonPending && CanCallRon(Game.HumanPlayerIndex.Value))
            {
                HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypes.Ron });
                if (autoCallMahjong)
                {
                    result.HumanCall = (Game.HumanPlayerIndex.Value, CallTypes.Ron);
                }
                else
                {
                    DiscardTileNotifier?.Invoke(new DiscardTileNotifierEventArgs());
                }
                return result;
            }

            // 2 - this code runs after every human ron check has been made
            // TODO: should the backend store human ron pendings, to free the UI from this responsability?
            // the loop ends, with the "EndOfRound" marker, if any "ron" call is made
            if (CheckOpponensRonCall(humanRonPending))
            {
                result.EndOfRound = true;
                result.RonPlayerId = kanInProgress.HasValue ? kanInProgress.Value.Item1 : PreviousPlayerIndex;
                if (kanInProgress.HasValue)
                {
                    UndoPickCompensationTile();
                }
                return result;
            }

            // 3 - notify the UI of the kan
            // it's done here (and not right after the kan) to not display new dora too soon
            // TODO: it's probably not perfect
            if (kanInProgress.HasValue)
            {
                ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypes.Kan, PotentialPreviousPlayerIndex = kanInProgress.Value.Item3 });
            }

            // 4 - checks "pon" and "kan" calls for human player, except if declined
            if (Game.HumanPlayerIndex.HasValue && !declinedHumanCall && CanCallPonOrKan(Game.HumanPlayerIndex.Value, out var isSelfKan))
            {
                if (!isSelfKan)
                {
                    DiscardTileNotifier?.Invoke(new DiscardTileNotifierEventArgs());
                }
                return result;
            }

            // 5 - "kan" call from non-human players
            // the loop starts over
            var opponentWithKanTilePick = IaManager.KanDecision(false);
            if (opponentWithKanTilePick.HasValue)
            {
                var previousPlayerIndex = PreviousPlayerIndex;
                var compensationTile = OpponentBeginCallKan(opponentWithKanTilePick.Value.pIndex, opponentWithKanTilePick.Value.tile, false);
                kanInProgress = (opponentWithKanTilePick.Value.pIndex, compensationTile, previousPlayerIndex);
                continue;
            }

            // 6 - "pon" call from non-human players
            // the loop starts over
            foreach (var pi in Enum.GetValues<PlayerIndices>().Where(Game.IsCpu))
            {
                if (IaManager.PonDecision(pi))
                {
                    PonCall(pi, sleepTime);
                    continue;
                }
            }

            // 7 - checks "chii" call for current player (human)
            // exits the loop to let the UI suggests the call
            if (IsHumanPlayer && !declinedHumanCall && CanCallChii().Count > 0)
            {
                DiscardTileNotifier?.Invoke(new DiscardTileNotifierEventArgs());
                return result;
            }

            // 8 - checks "chii" call for current player (non-human)
            // the loop starts over
            var (_, chiiTilePick) = IaManager.ChiiDecision();
            if (chiiTilePick != null)
            {
                ChiiCall(chiiTilePick, sleepTime);
                continue;
            }

            // 9 - there is a "kan" call in progress by non-human player
            // several things can happen:
            // - tsumo from the caller (ends the loop)
            // - another "kan" call
            // - nothing special : checks "riichi" call and discard
            // note: in any case the loop starts over
            if (kanInProgress != null)
            {
                if (OpponentAfterPick(ref kanInProgress, sleepTime))
                {
                    result.EndOfRound = true;
                    return result;
                }
                continue;
            }

            // 10 - no more tiles to work with
            // ends the loop
            if (IsWallExhaustion)
            {
                result.EndOfRound = true;
                return result;
            }

            // 11 - the current player picks a tile
            AutoPick();

            // 12 - consequence of a pick:
            // - for human player:
            //      - checks for tsumo (auto or manual)
            //      - checks for riichi
            //      - checks for auto discard
            // - for non human player, checks for tsumo, kan and riichi
            if (IsHumanPlayer)
            {
                var call = HumanAutoPlay(autoCallMahjong, sleepTime);
                if (call.HasValue)
                {
                    result.HumanCall = (CurrentPlayerIndex, call.Value);
                }
                return result;
            }
            else if (OpponentAfterPick(ref kanInProgress, sleepTime))
            {
                result.EndOfRound = true;
                return result;
            }
        }

        return result;
    }

    /// <summary>
    /// Checks if calling chii is allowed for the specified player.
    /// </summary>
    /// <returns>
    /// the first tile (ie the lowest number) of <see cref="HandPivot.ConcealedTiles"/> to use in the sequence.
    /// The list is empty if calling chii is impossible.
    /// </returns>
    public IReadOnlyList<TilePivot> CanCallChii()
    {
        if (_wallTiles.Count == 0 || _discards[(int)PreviousPlayerIndex].Count == 0 || _waitForDiscard || IsRiichi(CurrentPlayerIndex))
        {
            return new List<TilePivot>();
        }

        var tile = _discards[(int)PreviousPlayerIndex][^1];
        if (tile.IsHonor)
        {
            return new List<TilePivot>();
        }

        var potentialTiles =
            _hands[(int)CurrentPlayerIndex]
                .ConcealedTiles
                .Where(t => t.Family == tile.Family && t.Number != tile.Number && (t.Number >= tile.Number - 2 || t.Number <= tile.Number + 2))
                .Distinct()
                .ToList();

        var tileRelativePositionMinus2 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number - 2);
        var tileRelativePositionMinus1 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number - 1);
        var tileRelativePositionBonus1 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number + 1);
        var tileRelativePositionBonus2 = potentialTiles.FirstOrDefault(t => t.Number == tile.Number + 2);

        var tilesFromConcealedHandWithRelativePosition = new List<TilePivot>(3);
        if (tileRelativePositionMinus2 != null && tileRelativePositionMinus1 != null)
        {
            tilesFromConcealedHandWithRelativePosition.Add(tileRelativePositionMinus2);
        }
        if (tileRelativePositionMinus1 != null && tileRelativePositionBonus1 != null)
        {
            tilesFromConcealedHandWithRelativePosition.Add(tileRelativePositionMinus1);
        }
        if (tileRelativePositionBonus1 != null && tileRelativePositionBonus2 != null)
        {
            tilesFromConcealedHandWithRelativePosition.Add(tileRelativePositionBonus1);
        }

        return tilesFromConcealedHandWithRelativePosition;
    }

    /// <summary>
    /// Checks if calling pon is allowed for the specified player in this context.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns><c>True</c> if calling pon is allowed in this context; <c>False otherwise.</c></returns>
    public bool CanCallPon(PlayerIndices playerIndex)
    {
        return _wallTiles.Count != 0
            && PreviousPlayerIndex != playerIndex
            && _discards[(int)PreviousPlayerIndex].Count != 0
            && !_waitForDiscard && !IsRiichi(playerIndex)
            && _hands[(int)playerIndex].ConcealedTiles.Where(t => t == _discards[(int)PreviousPlayerIndex][^1]).Count() >= 2;
    }

    /// <summary>
    /// Checks if calling kan is allowed for the specified player in this context.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns>A tile from every possible kans.</returns>
    public IReadOnlyList<TilePivot> CanCallKan(PlayerIndices playerIndex)
    {
        if (_compensationTiles.Count == 0 || _wallTiles.Count == 0)
        {
            return new List<TilePivot>();
        }

        if (CurrentPlayerIndex == playerIndex && _waitForDiscard)
        {
            var kansFromConcealed =
                _hands[(int)playerIndex].ConcealedTiles
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
                if (disposableForRiichi.Any(t => t != _hands[(int)playerIndex].LatestPick))
                {
                    return new List<TilePivot>();
                }
                kansFromConcealed = kansFromConcealed.Where(t => t == _hands[(int)playerIndex].LatestPick);
            }

            var kansFromPons =
                _hands[(int)playerIndex].DeclaredCombinations
                                        .Where(c => c.IsBrelan && _hands[(int)playerIndex].ConcealedTiles.Any(t => t == c.OpenTile))
                                        .Select(c => c.OpenTile!)
                                        .Distinct();

            var everyKans = new List<TilePivot>(kansFromConcealed);
            everyKans.AddRange(kansFromPons);

            return everyKans;
        }
        else
        {
            if (_waitForDiscard || PreviousPlayerIndex == playerIndex || _discards[(int)PreviousPlayerIndex].Count == 0 || IsRiichi(playerIndex))
            {
                return new List<TilePivot>();
            }

            var referenceTileFromDiscard = _discards[(int)PreviousPlayerIndex][^1];
            return _hands[(int)playerIndex].ConcealedTiles.Where(t => t == referenceTileFromDiscard).Count() >= 3
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
    public bool CallChii(TilePivot pickInSequence)
    {
        if (CanCallChii().Count == 0)
        {
            return false;
        }

        var stolenTile = _discards[(int)PreviousPlayerIndex][^1];

        _hands[(int)CurrentPlayerIndex].DeclareChii(
            stolenTile,
            Game.GetPlayerCurrentWind(PreviousPlayerIndex),
            Math.Min(pickInSequence.Number, stolenTile.Number)
        );
        _discards[(int)PreviousPlayerIndex].RemoveAt(_discards[(int)PreviousPlayerIndex].Count - 1);
        _stealingInProgress = true;
        _waitForDiscard = true;
        return true;
    }

    /// <summary>
    /// Tries to call pon for the specified player.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns><c>True</c> if success; <c>False</c> if failure.</returns>
    public bool CallPon(PlayerIndices playerIndex)
    {
        if (!CanCallPon(playerIndex))
        {
            return false;
        }

        _hands[(int)playerIndex].DeclarePon(
            _discards[(int)PreviousPlayerIndex][^1],
            Game.GetPlayerCurrentWind(PreviousPlayerIndex)
        );
        _discards[(int)PreviousPlayerIndex].RemoveAt(_discards[(int)PreviousPlayerIndex].Count - 1);
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
    public TilePivot? CallKan(PlayerIndices playerIndex, TilePivot? tileChoice = null)
    {
        if (CanCallKan(playerIndex).Count == 0)
        {
            return null;
        }

        var fromPreviousPon = tileChoice == null ? null :
            _hands[(int)playerIndex].DeclaredCombinations.FirstOrDefault(c => c.IsBrelan && c.OpenTile == tileChoice);

        var isClosedKan = false;
        if (CurrentPlayerIndex == playerIndex && _waitForDiscard)
        {
            // Forces a decision, even if there're several possibilities.
            if (tileChoice == null)
            {
                tileChoice = _hands[(int)playerIndex].ConcealedTiles.GroupBy(t => t).FirstOrDefault(t => t.Count() == 4)?.Key;
                if (tileChoice == null)
                {
                    tileChoice = _hands[(int)playerIndex].ConcealedTiles.First(t => _hands[(int)playerIndex].DeclaredCombinations.Any(c => c.IsBrelan && c.OpenTile == t));
                    fromPreviousPon = _hands[(int)playerIndex].DeclaredCombinations.First(c => c.OpenTile == tileChoice);
                }
            }

            _hands[(int)playerIndex].DeclareKan(tileChoice, null, fromPreviousPon);
            if (fromPreviousPon != null)
            {
                _virtualDiscards[(int)playerIndex].Add(tileChoice);
            }
            isClosedKan = true;
        }
        else
        {
            _hands[(int)playerIndex].DeclareKan(
                _discards[(int)PreviousPlayerIndex][^1],
                Game.GetPlayerCurrentWind(PreviousPlayerIndex),
                null
            );
            _discards[(int)PreviousPlayerIndex].RemoveAt(_discards[(int)PreviousPlayerIndex].Count - 1);
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
        var riichiTurnsCount = _discards[(int)CurrentPlayerIndex].Count;
        var isUninterruptedFirstTurn = _discards[(int)CurrentPlayerIndex].Count == 0 && IsUninterruptedHistory(CurrentPlayerIndex);

        if (!Discard(tile))
        {
            throw new InvalidOperationException("The discard post-riichi has failed for an unknow reason.");
        }

        _riichis[(int)PreviousPlayerIndex] = new RiichiPivot(riichiTurnsCount, isUninterruptedFirstTurn, tile,
            Enum.GetValues<PlayerIndices>().Where(i => i != PreviousPlayerIndex).Select(i => new KeyValuePair<PlayerIndices, int>(i, _virtualDiscards[(int)i].Count)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
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

        _hands[(int)CurrentPlayerIndex].Discard(tile);

        if (_stealingInProgress || _closedKanInProgress != null)
        {
            _playerIndexHistory.Clear();
        }

        _discards[(int)CurrentPlayerIndex].Add(tile);
        _virtualDiscards[(int)CurrentPlayerIndex].Add(tile);
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
            || !_hands[(int)CurrentPlayerIndex].IsConcealed
            || _wallTiles.Count < 4
            || Game.Players[(int)CurrentPlayerIndex].Points < ScoreTools.RIICHI_COST)
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
            _hands[(int)CurrentPlayerIndex].LatestPick,
            isKanCompensation ? DrawTypes.Compensation : DrawTypes.Wall);

        return _hands[(int)CurrentPlayerIndex].IsComplete;
    }

    /// <summary>
    /// Checks if the specified player is riichi.
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <returns><c>True</c> if riichi; <c>False</c> otherwise.</returns>
    public bool IsRiichi(PlayerIndices playerIndex)
    {
        return _riichis[(int)playerIndex] != null;
    }

    /// <summary>
    /// Checks, for a specified player, if the specified rank is the one when the riichi call has been made.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <param name="rank">The rank.</param>
    /// <returns><c>True</c> if the specified rank is the riichi one.</returns>
    public bool IsRiichiRank(PlayerIndices playerIndex, int rank)
    {
        return _riichis[(int)playerIndex] != null && _riichis[(int)playerIndex]!.DiscardRank == rank;
    }

    /// <summary>
    /// Checks if the current human player can auto-discard.
    /// </summary>
    /// <returns><c>True</c> if he can; <c>False</c> otherwise.</returns>
    public bool HumanCanAutoDiscard()
    {
        return Game.IsHuman(CurrentPlayerIndex)
            && IsRiichi(CurrentPlayerIndex)
            && CanCallKan(CurrentPlayerIndex).Count == 0
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

        _wallTiles.Add(_deadTreasureTiles[^1]);
        _deadTreasureTiles.RemoveAt(_deadTreasureTiles.Count - 1);

        // We could remove the compensation tile from the CurrentPlayerIndex hand, but it's not very useful in this context.
    }

    /// <summary>
    /// Checks if the specified tile is allowed for discard for the current player.
    /// </summary>
    /// <param name="tile">The tile to check.</param>
    /// <returns><c>True</c> if the tile is discardable; <c>False</c> otherwise.</returns>
    public bool CanDiscard(TilePivot tile)
    {
        return _waitForDiscard
            && (!IsRiichi(CurrentPlayerIndex) || ReferenceEquals(tile, _hands[(int)CurrentPlayerIndex].LatestPick))
            && _hands[(int)CurrentPlayerIndex].CanDiscardTile(tile, _stealingInProgress);
    }

    /// <summary>
    /// Gets the discard of a specified player.
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <returns>Collection of discarded <see cref="TilePivot"/> instances.</returns>
    public IReadOnlyList<TilePivot> GetDiscard(PlayerIndices playerIndex)
    {
        return _discards[(int)playerIndex];
    }

    /// <summary>
    /// Gets the hand of a specified player.
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <returns>Instance of <see cref="HandPivot"/>.</returns>
    public HandPivot GetHand(PlayerIndices playerIndex)
    {
        return _hands[(int)playerIndex];
    }

    public bool CheckOpponensRonCall(bool humanRonPending)
    {
        var atLeastOneRon = humanRonPending;
        // TODO: very marginally, the order of players can impact decision
        foreach (var pi in Enum.GetValues<PlayerIndices>().Where(Game.IsCpu))
        {
            var ronCalled = IaManager.RonDecision(pi, atLeastOneRon);
            if (ronCalled)
            {
                atLeastOneRon = true;
                CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypes.Ron, PlayerIndex = pi });
            }
        }

        return atLeastOneRon;
    }

    #endregion Public methods

    #region Private methods

    #region Autoplay methods

    private TilePivot? OpponentBeginCallKan(PlayerIndices playerId, TilePivot kanTilePick, bool concealedKan)
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        var compensationTile = CallKan(playerId, concealedKan ? kanTilePick : null);
        if (compensationTile != null)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = playerId, Action = CallTypes.Kan });
        }
        return compensationTile;
    }

    private void AutoPick()
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        Pick();

        PickNotifier?.Invoke(new PickNotifierEventArgs());
    }

    private void ChiiCall(TilePivot chiiTilePick, int sleepTime)
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        var callChii = CallChii(chiiTilePick);
        if (callChii)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypes.Chii, PlayerIndex = CurrentPlayerIndex });

            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypes.Chii });

            if (!IsHumanPlayer)
            {
                var discardDecision = IaManager.DiscardDecision();
                Discard(discardDecision, sleepTime);
            }
        }
    }

    private void PonCall(PlayerIndices playerIndex, int sleepTime)
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        // Note : this value is stored here because the call to "CallPon" makes it change.
        var previousPlayerIndex = PreviousPlayerIndex;
        var isCpu = Game.IsCpu(playerIndex);

        var callPon = CallPon(playerIndex);
        if (callPon)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = playerIndex, Action = CallTypes.Pon });

            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypes.Pon, PreviousPlayerIndex = previousPlayerIndex, PlayerIndex = playerIndex });

            if (isCpu)
            {
                var discardDecision = IaManager.DiscardDecision();
                Discard(discardDecision, sleepTime);
            }
        }
    }

    private void Discard(TilePivot tile, int sleepTime)
    {
        if (!IsHumanPlayer)
        {
            Thread.Sleep(sleepTime);
        }

        var hasDiscard = Discard(tile);
        if (hasDiscard)
        {
            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypes.NoCall });
        }
    }

    private bool OpponentAfterPick(ref (PlayerIndices, TilePivot?, PlayerIndices?)? kanInProgress, int sleepTime)
    {
        var tsumoDecision = IaManager.TsumoDecision(kanInProgress != null);
        if (tsumoDecision)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypes.Tsumo, PlayerIndex = CurrentPlayerIndex });
            return true;
        }

        var opponentWithKanTilePick = IaManager.KanDecision(true);
        if (opponentWithKanTilePick.HasValue)
        {
            var compensationTile = OpponentBeginCallKan(CurrentPlayerIndex, opponentWithKanTilePick.Value.tile, true);
            kanInProgress = (CurrentPlayerIndex, compensationTile, null);
            return false;
        }

        kanInProgress = null;

        var riichiTile = IaManager.RiichiDecision();
        if (riichiTile != null)
        {
            CallRiichi(riichiTile, sleepTime);
            return false;
        }

        Discard(IaManager.DiscardDecision(), sleepTime);
        return false;
    }

    private void CallRiichi(TilePivot tile, int sleepTime)
    {
        if (!IsHumanPlayer)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = CurrentPlayerIndex, Action = CallTypes.Riichi });
            Thread.Sleep(sleepTime);
        }

        var callRiichi = CallRiichi(tile);
        if (callRiichi)
        {
            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypes.Riichi });
        }
    }

    private CallTypes? HumanAutoPlay(bool autoCallMahjong, int sleepTime)
    {
        if (CanCallTsumo(false))
        {
            HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypes.Tsumo });
            return autoCallMahjong ? CallTypes.Tsumo : default(CallTypes?);
        }

        var riichiTiles = CanCallRiichi();
        RiichiChoicesNotifier?.Invoke(new RiichiChoicesNotifierEventArgs(riichiTiles));
        if (riichiTiles.Count > 0)
        {
            var adviseRiichi = Game.Ruleset.DiscardTip && IaManager.RiichiDecision() != null;
            HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypes.Riichi, RiichiAdvised = adviseRiichi });
            return null;
        }
        else if (HumanCanAutoDiscard())
        {
            // Not a real CPU sleep: the auto-discard by human player is considered as such
            Thread.Sleep(sleepTime);
            return CallTypes.NoCall;
        }
        else
        {
            HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypes.NoCall });
        }

        return null;
    }

    #endregion Autoplay methods

    // Gets every tiles from every opponents virtual discards after the riichi call of the specified player.
    private List<TilePivot> GetTilesFromVirtualDiscardsAtRank(PlayerIndices riichiPlayerIndex, TilePivot exceptTile)
    {
        var fullList = new List<TilePivot>(20);

        if (_riichis[(int)riichiPlayerIndex] == null)
        {
            return fullList;
        }

        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            if (i != riichiPlayerIndex)
            {
                var opponentRank = _riichis[(int)riichiPlayerIndex]!.OpponentsVirtualDiscardRank[i];
                fullList.AddRange(_virtualDiscards[(int)i].Skip(opponentRank));
            }
        }

        return fullList.Where(t => !ReferenceEquals(t, exceptTile)).ToList();
    }

    // Picks a compensation tile (after a kan call) for the current player.
    private TilePivot PickCompensationTile(bool isClosedKan)
    {
        var compensationTile = _compensationTiles[0];
        _compensationTiles.RemoveAt(0);

        _deadTreasureTiles.Add(_wallTiles[^1]);

        _wallTiles.RemoveAt(_wallTiles.Count - 1);
        NotifyWallCount?.Invoke();

        _hands[(int)CurrentPlayerIndex].Pick(compensationTile);
        NotifyPick?.Invoke(new PickTileEventArgs(CurrentPlayerIndex, compensationTile));

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
    private bool IsUninterruptedHistory(PlayerIndices playerIndex)
    {
        var historySinceLastTime = _playerIndexHistory.TakeWhile(i => i != playerIndex).ToList();

        var rank = 1;
        for (var i = historySinceLastTime.Count - 1; i >= 0; i--)
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
    private void SetYakus(PlayerIndices playerIndex, TilePivot tile, DrawTypes drawType)
    {
        _hands[(int)playerIndex].SetYakus(new WinContextPivot(
            latestTile: tile,
            drawType: drawType,
            dominantWind: Game.DominantWind,
            playerWind: Game.GetPlayerCurrentWind(playerIndex),
            isFirstOrLast: IsWallExhaustion ? (bool?)null : (_discards[(int)playerIndex].Count == 0 && IsUninterruptedHistory(playerIndex)),
            isRiichi: IsRiichi(playerIndex) ? (_riichis[(int)playerIndex]!.IsDaburu ? (bool?)null : true) : false,
            isIppatsu: IsIppatsu(playerIndex)
        ));
    }

    // Checks if the specified player is ippatsu.
    private bool IsIppatsu(PlayerIndices playerIndex)
    {
        return IsRiichi(playerIndex)
            && _discards[(int)playerIndex].Count > 0
            && ReferenceEquals(_discards[(int)playerIndex][^1], _riichis[(int)playerIndex]!.Tile)
            && IsUninterruptedHistory(playerIndex);
    }

    // Gets the concealed tile of the round from the point of view of a specified player.
    private List<TilePivot> GetConcealedTilesFromPlayerPointOfView(PlayerIndices playerIndex)
    {
        // Wall tiles.
        var tiles = new List<TilePivot>(_wallTiles);

        // Concealed tiles from opponents.
        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            if (i != playerIndex)
            {
                tiles.AddRange(_hands[(int)i].ConcealedTiles);
            }
        }

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
    private List<PlayerIndices> CheckForNagashiMangan()
    {
        var playerIndexList = new List<PlayerIndices>(4);

        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            var fullTerminalsOrHonors = _discards[(int)i].All(t => t.IsHonorOrTerminal);
            var noPlayerStealing = _hands[(int)i].IsConcealed;
            var noOpponentStealing = !_hands.Where(h => _hands.IndexOf(h) != (int)i).Any(h => h.DeclaredCombinations.Any(c => c.StolenFrom == Game.GetPlayerCurrentWind(i)));
            if (fullTerminalsOrHonors && noPlayerStealing && noOpponentStealing)
            {
                _hands[(int)i].SetYakus(new WinContextPivot());
                playerIndexList.Add(i);
            }
        }

        return playerIndexList;
    }

    // Gets the count of dora for specified tile
    private int GetDoraCountInternal(TilePivot t, IReadOnlyList<TilePivot> doraIndicators)
    {
        return doraIndicators.Take(VisibleDorasCount).Count(t.IsDoraNext);
    }

    #endregion Private methods

    #region Internal methods

    /// <summary>
    /// Tries to pick the next tile from the wall.
    /// </summary>
    internal void Pick()
    {
        if (_wallTiles.Count == 0 || _waitForDiscard)
        {
            return;
        }

        var tile = _wallTiles[0];
        _wallTiles.Remove(tile);
        NotifyWallCount?.Invoke();
        _hands[(int)CurrentPlayerIndex].Pick(tile);
        NotifyPick?.Invoke(new PickTileEventArgs(CurrentPlayerIndex, tile));
        _waitForDiscard = true;
    }

    /// <summary>
    /// Checks if the hand of the specified player is ready for calling ron.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns><c>True</c> if calling ron is possible; <c>False</c> otherwise.</returns>
    internal bool CanCallRon(PlayerIndices playerIndex)
    {
        var tile = _waitForDiscard ? null : _discards[(int)PreviousPlayerIndex].LastOrDefault();
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

        SetYakus(playerIndex, tile, forKokushiOnly ? DrawTypes.OpponentKanCallConcealed : (isChanka ? DrawTypes.OpponentKanCallOpen : DrawTypes.OpponentDiscard));

        return _hands[(int)playerIndex].IsComplete
            && !_hands[(int)playerIndex].CancelYakusIfFuriten(_discards[(int)playerIndex], GetTilesFromVirtualDiscardsAtRank(playerIndex, tile))
            && !_hands[(int)playerIndex].CancelYakusIfTemporaryFuriten(this, playerIndex);
    }

    /// <summary>
    /// Checks if the hand of the specified player is tenpai.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <param name="tileToRemoveFromConcealed">A tile to remove from the hand first; only if <see cref="HandPivot.IsFullHand"/> is <c>True</c> for this hand.</param>
    /// <returns><c>True</c> if tenpai; <c>False</c> otherwise.</returns>
    internal bool IsTenpai(PlayerIndices playerIndex, TilePivot? tileToRemoveFromConcealed)
    {
        var hand = _hands[(int)playerIndex];

        // TODO : there're (maybe) specific rules about it:
        // for instance, what if I have a single wait on tile "4 circle" but every tiles "4 circle" are already in my hand ?
        return hand.IsTenpai(_fullTilesList, tileToRemoveFromConcealed);
    }

    /// <summary>
    /// Checks if a priority call can be made by the specified player.
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <param name="isSelfKan">If the method returns <c>True</c>, this indicates a self kan if <c>True</c>.</param>
    /// <returns><c>True</c> if call available; <c>False otherwise</c>.</returns>
    internal bool CanCallPonOrKan(PlayerIndices playerIndex, out bool isSelfKan)
    {
        isSelfKan = _waitForDiscard;
        return CanCallKan(playerIndex).Count > 0 || CanCallPon(playerIndex);
    }

    /// <summary>
    /// Similar to <see cref="CanCallKan(int)"/> but with the list of possible tiles depending on <paramref name="concealed"/>.
    /// </summary>
    /// <param name="playerId">The player index.</param>
    /// <param name="concealed"><c>True</c> to check only concealed kan (or from a previous pon); <c>False</c> to check the opposite; <c>Null</c> for both.</param>
    /// <returns>List of possible tiles.</returns>
    internal IReadOnlyList<TilePivot> CanCallKanWithChoices(PlayerIndices playerId, bool? concealed)
    {
        var tiles = CanCallKan(playerId);
        if (concealed == true)
        {
            tiles = tiles.Where(t => _hands[(int)playerId].ConcealedTiles.Count(ct => t == ct) == 4
                || _hands[(int)playerId].DeclaredCombinations.Any(ct => ct.IsBrelan && t == ct.OpenTile)).ToList();
        }
        else if (concealed == false)
        {
            tiles = tiles.Where(t => _hands[(int)playerId].ConcealedTiles.Count(ct => t == ct) == 3).ToList();
        }

        return tiles;
    }

    /// <summary>
    /// Checks if the hand of the specified player is tenpai and list tiles which can be discarded.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns>The list of tiles which can be discarded.</returns>
    internal IReadOnlyList<TilePivot> ExtractDiscardChoicesFromTenpai(PlayerIndices playerIndex)
    {
        var distinctTilesFromOverallConcealed = GetConcealedTilesFromPlayerPointOfView(playerIndex).Distinct().ToList();

        var hand = _hands[(int)playerIndex];

        var tilesToSub = hand.ConcealedTiles
            .Where(tt => hand.CanDiscardTile(tt, _stealingInProgress))
            .Distinct()
            .ToList();

        var subPossibilities = new List<TilePivot>(tilesToSub.Count);
        foreach (var tileToSub in tilesToSub)
        {
            var tempListConcealed = new List<TilePivot>(hand.ConcealedTiles);
            tempListConcealed.Remove(tileToSub);
            if (HandPivot.IsTenpai(tempListConcealed, hand.DeclaredCombinations, distinctTilesFromOverallConcealed))
            {
                subPossibilities.Add(tileToSub);
            }
        }

        // Avoids red doras in the list returned (if possible).
        var realSubPossibilities = new List<TilePivot>(subPossibilities.Count);
        foreach (var tile in subPossibilities)
        {
            TilePivot? subTile = null;
            if (tile.IsRedDora)
            {
                subTile = hand.ConcealedTiles.FirstOrDefault(t => t == tile && !t.IsRedDora);
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
    internal EndOfRoundInformationsPivot EndOfRound(PlayerIndices? ronPlayerIndex)
    {
        var turnWind = false;
        var ryuukyoku = true;
        var displayUraDoraTiles = false;

        var winners = _hands.Where(h => h.IsComplete).Select(w => (PlayerIndices)_hands.IndexOf(w)).ToList();

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
            var tenpaiPlayersIndex = Enum.GetValues<PlayerIndices>().Where(i => IsTenpai(i, null)).ToList();
            var notTenpaiPlayersIndex = Enum.GetValues<PlayerIndices>().Except(tenpaiPlayersIndex).ToList();

            // Wind turns if East is not tenpai.
            turnWind = notTenpaiPlayersIndex.Any(tpi => Game.GetPlayerCurrentWind(tpi) == Winds.East);

            var (tenpai, nonTenpai) = ScoreTools.GetRyuukyokuPoints(tenpaiPlayersIndex.Count);

            tenpaiPlayersIndex.ForEach(i =>
                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(i, Game.IsHuman(i), 0, 0, _hands[(int)i], tenpai, 0, 0, 0, tenpai)));
            notTenpaiPlayersIndex.ForEach(i =>
                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(i, Game.IsHuman(i), nonTenpai)));
        }
        else
        {
            turnWind = !winners.Any(w => Game.GetPlayerCurrentWind(w) == Winds.East);

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
            var honbaPoints = ScoreTools.GetHonbaPoints(Game.HonbaCountBeforeScoring);

            foreach (var pIndex in winners)
            {
                var phand = _hands[(int)pIndex];

                // in case of multiple rons; the winning player closest to east win the prize
                var winnerHonba = honbaPoints;
                if (ronPlayerIndex.HasValue && winners.Count > 1
                    && Game.GetPlayerCurrentWind(pIndex) != winners.Min(Game.GetPlayerCurrentWind))
                {
                    winnerHonba = 0;
                }

                // In case of ron, fix the "LatestPick" property of the winning hand
                if (ronPlayerIndex.HasValue)
                {
                    phand.SetFromRon(_discards[(int)ronPlayerIndex.Value][^1]);
                }

                PlayerIndices? liablePlayerId = null;
                if (phand.Yakus!.Contains(YakuPivot.Daisangen)
                    && phand.DeclaredCombinations.Count(c => c.Family == Families.Dragon) == 3
                    && phand.DeclaredCombinations.Last(c => c.Family == Families.Dragon).StolenFrom.HasValue)
                {
                    liablePlayerId = Game.GetPlayerIndexByCurrentWind(phand.DeclaredCombinations.Last(c => c.Family == Families.Dragon).StolenFrom!.Value);
                }
                else if (phand.Yakus!.Contains(YakuPivot.Daisuushii)
                    && phand.DeclaredCombinations.Count(c => c.Family == Families.Wind) == 4
                    && phand.DeclaredCombinations.Last(c => c.Family == Families.Wind).StolenFrom.HasValue)
                {
                    liablePlayerId = Game.GetPlayerIndexByCurrentWind(phand.DeclaredCombinations.Last(c => c.Family == Families.Wind).StolenFrom!.Value);
                }

                var isRiichi = phand.Yakus!.Contains(YakuPivot.Riichi) || phand.Yakus!.Contains(YakuPivot.DaburuRiichi);

                var dorasCount = phand.AllTiles.Sum(GetDoraCount);
                var uraDorasCount = isRiichi ? phand.AllTiles.Sum(GetUraDoraCount) : 0;
                var redDorasCount = phand.AllTiles.Count(t => t.IsRedDora);

                if (isRiichi)
                {
                    displayUraDoraTiles = true;
                }

                var fanCount = ScoreTools.GetFanCount(phand.Yakus!, phand.IsConcealed, dorasCount, uraDorasCount, redDorasCount);
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

                var (east, notEast) = ScoreTools.GetPoints(fanCount, fuCount, !ronPlayerIndex.HasValue, Game.GetPlayerCurrentWind(pIndex));

                var basePoints = east + (notEast * 2);

                var riichiPart = Game.PendingRiichiCount * ScoreTools.RIICHI_COST;

                // In case of ron with multiple winners, only the one who comes right next to "ronPlayerIndex" takes the stack of riichi.
                if (winners.Count > 1)
                {
                    for (var i = 1; i <= 3; i++)
                    {
                        var nextPlayerId = ronPlayerIndex!.Value.RelativePlayerIndex(i);
                        if (winners.Contains(nextPlayerId) && pIndex != nextPlayerId)
                        {
                            riichiPart = 0;
                        }
                    }
                }

                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                    pIndex, Game.IsHuman(pIndex), fanCount, fuCount, phand, basePoints + riichiPart + winnerHonba,
                    dorasCount, uraDorasCount, redDorasCount, basePoints));

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

            // Note : "liablePlayersLost" is empty in case of tsumo transformed into ron.
            if (liablePlayersLost.Count > 0)
            {
                var pointsNotOnRonPlayer = 0;
                foreach (var liablePlayerId in liablePlayersLost.Keys)
                {
                    pointsNotOnRonPlayer += liablePlayerId != ronPlayerIndex!.Value ? liablePlayersLost[liablePlayerId] : 0;
                    if (playerInfos.Any(pi => pi.Index == liablePlayerId))
                    {
                        playerInfos.First(pi => pi.Index == liablePlayerId).AddPoints(liablePlayersLost[liablePlayerId]);
                    }
                    else
                    {
                        playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                            liablePlayerId, Game.IsHuman(liablePlayerId), liablePlayersLost[liablePlayerId] - honbaPoints));
                    }
                }
                if (playerInfos.Any(pi => pi.Index == ronPlayerIndex!.Value))
                {
                    playerInfos.First(pi => pi.Index == ronPlayerIndex!.Value).AddPoints(-pointsNotOnRonPlayer);
                }
                else
                {
                    playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                        ronPlayerIndex!.Value, Game.IsHuman(ronPlayerIndex.Value), eastOrLoserLostCumul - pointsNotOnRonPlayer));
                }
            }
            else if (ronPlayerIndex.HasValue)
            {
                playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                    ronPlayerIndex.Value, Game.IsHuman(ronPlayerIndex.Value), eastOrLoserLostCumul - honbaPoints));
            }
            else
            {
                foreach (var pIndex in Enum.GetValues<PlayerIndices>())
                {
                    if (!winners.Contains(pIndex))
                    {
                        playerInfos.Add(new EndOfRoundInformationsPivot.PlayerInformationsPivot(
                            pIndex, Game.IsHuman(pIndex), (Game.GetPlayerCurrentWind(pIndex) == Winds.East ? eastOrLoserLostCumul : notEastLostCumul) - (honbaPoints / 3)));
                    }
                }
            }

            ryuukyoku = false;
        }

        foreach (var p in playerInfos)
        {
            Game.Players[(int)p.Index].AddPoints(p.PointsGain);
        }

        return new EndOfRoundInformationsPivot(ryuukyoku, turnWind, displayUraDoraTiles, playerInfos, Game.HonbaCountBeforeScoring,
            Game.PendingRiichiCount, DoraIndicatorTiles, UraDoraIndicatorTiles, VisibleDorasCount);
    }

    /// <summary>
    /// Computes the list of every tiles whose fate is sealed from the point of view of a specific player.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns>Tiles enumeration.</returns>
    internal IReadOnlyList<TilePivot> DeadTilesFromIndexPointOfView(PlayerIndices playerIndex)
    {
        return _fullTilesList.Except(GetConcealedTilesFromPlayerPointOfView(playerIndex)).ToList();
    }

    // Gets dora count if the specvified tile is a dora
    internal int GetDoraCount(TilePivot t) => GetDoraCountInternal(t, DoraIndicatorTiles);

    // Gets dora count if the specvified tile is an uradora
    internal int GetUraDoraCount(TilePivot t) => GetDoraCountInternal(t, UraDoraIndicatorTiles);

    #endregion Internal methods
}