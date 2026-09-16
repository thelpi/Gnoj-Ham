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
    // Whether the most recently called kan was open (daiminkan or shouminkan) rather than a genuine
    // ankan - set by CallKan, consumed once by ResolveKanDoraReveal right after the chankan window.
    private bool _lastKanWasOpen;
    // Dora indicators actually revealed so far (starts at 1, the initial indicator).
    private int _visibleDorasCount = 1;
    // Open-kan dora reveals confirmed past the chankan window but still withheld until the discard
    // that follows them - real rule: ankan reveals its dora immediately, an open kan (daiminkan or
    // shouminkan) only after the resulting discard (and never at all if won by rinshan kaihou first).
    private int _pendingOpenKanDoraReveals;
    private readonly DiscardHistoryPivot _discardHistory;
    private readonly List<TilePivot> _wallTiles;
    private readonly List<HandPivot> _hands;
    private readonly List<TilePivot> _compensationTiles;
    private readonly List<TilePivot> _doraIndicatorTiles;
    private readonly List<TilePivot> _uraDoraIndicatorTiles;
    private readonly List<TilePivot> _deadTreasureTiles;
    private readonly List<RiichiPivot?> _riichis;
    private readonly List<TilePivot> _fullTilesList;
    private readonly IReadOnlyDictionary<PlayerIndices, CpuManagerBasePivot> _cpuManagers;

    /// <summary>
    /// All tiles.
    /// </summary>
    public IReadOnlyList<TilePivot> FullTilesList => _fullTilesList;

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
    /// Advisor.
    /// </summary>
    public CpuManagerBasePivot? Advisor => Game.HumanPlayerIndex.HasValue ? _cpuManagers[Game.HumanPlayerIndex.Value] : null;

    /// <summary>
    /// Gets the CPU decisions manager of the specified player.
    /// </summary>
    /// <param name="playerIndex">Player index.</param>
    /// <returns>Instance of <see cref="CpuManagerBasePivot"/>.</returns>
    internal CpuManagerBasePivot CpuManager(PlayerIndices playerIndex) => _cpuManagers[playerIndex];

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
    /// Inferred; indicates if all four players have declared riichi ("suucha riichi" abortive draw).
    /// </summary>
    internal bool IsSuuchaRiichi => _riichis.All(r => r != null);

    /// <summary>
    /// Indicates if "kyuushu kyuuhai" has been declared (see <see cref="CallKyuushuKyuuhai"/>).
    /// </summary>
    internal bool IsKyuushuKyuuhai { get; private set; }

    /// <summary>
    /// Inferred; indicates if four kans have been declared by at least two different players
    /// ("suukaikan" abortive draw). If all four kans come from a single player, the round continues
    /// instead, to give them a chance at the "suukantsu" yakuman.
    /// </summary>
    internal bool IsSuukaikan
    {
        get
        {
            var kanCountsByPlayer = _hands.Select(h => h.DeclaredCombinations.Count(c => c.IsSquare)).ToList();
            return kanCountsByPlayer.Sum() == 4 && kanCountsByPlayer.Count(c => c > 0) > 1;
        }
    }

    /// <summary>
    /// Inferred; indicates if all four players discarded the same wind tile on their first,
    /// uninterrupted turn ("suufon renda" abortive draw). Optional rule (not used in European
    /// competition rules), on by default.
    /// </summary>
    internal bool IsSuufonRenda
    {
        get
        {
            if (!Game.Ruleset.UseSuufonRenda)
            {
                return false;
            }

            // "PlayerIndexHistory.Count" only ever equals the total discard count when no call
            // (pon / chii / kan) has happened yet: any call clears it. Requiring both to be 4 pins
            // this down to exactly "four discards, first turn each, nothing called in between".
            if (_discardHistory.PlayerIndexHistory.Count != 4 || _discardHistory.Discards.Sum(d => d.Count) != 4)
            {
                return false;
            }

            var firstDiscardedWind = _discardHistory.Discards[0][0];
            return firstDiscardedWind.Family == Families.Wind
                && _discardHistory.Discards.All(d => d[0].Family == Families.Wind && d[0].Wind == firstDiscardedWind.Wind);
        }
    }

    /// <summary>
    /// Inferred; count of visible doras. An ankan reveals its indicator immediately; an open kan
    /// (daiminkan or shouminkan) only once the discard that follows it happens (see
    /// <see cref="ResolveKanDoraReveal"/> and the pending-reveal flush in <see cref="Discard(TilePivot)"/>).
    /// </summary>
    public int VisibleDorasCount => _visibleDorasCount;

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

    // Raise methods for AutoPlayEnginePivot: a field-like event can only be invoked from within the
    // declaring type, so the engine (a separate class) needs these trampolines to fire them itself.
    internal void RaiseHumanCallNotifier(HumanCallNotifierEventArgs args) => HumanCallNotifier?.Invoke(args);
    internal void RaiseDiscardTileNotifier(DiscardTileNotifierEventArgs args) => DiscardTileNotifier?.Invoke(args);
    internal void RaiseCallNotifier(CallNotifierEventArgs args) => CallNotifier?.Invoke(args);
    internal void RaiseReadyToCallNotifier(ReadyToCallNotifierEventArgs args) => ReadyToCallNotifier?.Invoke(args);
    internal void RaiseRiichiChoicesNotifier(RiichiChoicesNotifierEventArgs args) => RiichiChoicesNotifier?.Invoke(args);

    #endregion Events

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> value.</param>
    /// <param name="firstPlayerIndex">The initial <see cref="CurrentPlayerIndex"/> value.</param>
    /// <param name="random">Randomizer instance.</param>
    /// <param name="drivenDraw">
    /// Optional; a <see cref="DrivenDrawPivot"/> method to rig the wall for a specific manual-testing
    /// scenario. <c>Null</c> (default) for a normal, fully random draw.
    /// </param>
    /// <param name="cpuManagerFactories">
    /// Optional; per-seat override of which <see cref="CpuManagerBasePivot"/> implementation plays
    /// that seat, for any seat present in the dictionary. A seat missing from the dictionary (or
    /// <c>Null</c> altogether, the default) plays through the plain <see cref="BasicCpuManagerPivot"/>.
    /// </param>
    internal RoundPivot(GamePivot game, PlayerIndices firstPlayerIndex, Random random, Action<List<TilePivot>>? drivenDraw = null,
        IReadOnlyDictionary<PlayerIndices, Func<RoundPivot, CpuManagerBasePivot>>? cpuManagerFactories = null)
    {
        Game = game;

        WallOpeningIndex = (PlayerIndices)random.Next(0, 4);

        _fullTilesList = TilePivot
            .GetCompleteSet(Game.Ruleset.UseRedDoras)
            .OrderBy(t => random.NextDouble())
            .ToList();

        drivenDraw?.Invoke(_fullTilesList);

        _hands = Enumerable.Range(0, 4).Select(i => new HandPivot(_fullTilesList.GetRange(i * 13, 13))).ToList();
        _discardHistory = new DiscardHistoryPivot();
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
        var cpuManagers = new Dictionary<PlayerIndices, CpuManagerBasePivot>();
        foreach (var i in Enumerable.Range(0, 4).Select(i => (PlayerIndices)i))
        {
            cpuManagers[i] = cpuManagerFactories != null && cpuManagerFactories.TryGetValue(i, out var factory)
                ? factory(this)
                : new BasicCpuManagerPivot(this);
        }
        _cpuManagers = cpuManagers;
    }

    #endregion Constructors

    #region Public methods

    /// <summary>
    /// Starts and runs the auto player.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="AutoPlayResultPivot"/>.</returns>>
    public AutoPlayResultPivot RunAutoPlay(CancellationToken cancellationToken)
        => RunAutoPlay(cancellationToken, false, false, false, false, null, 0);

    /// <summary>
    /// Starts and runs the auto player.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="declinedHumanCall">Indicates that a potential call has been suggested to the human player and has been declined..</param>
    /// <param name="humanRonPending">Indicates that the human player has called 'Ron', but the same call by opponents has to be checked too.</param>
    /// <param name="autoCallMahjong">When enabled, if the human player can call 'Tsumo' or 'Ron', the call is automatically made.</param>
    /// <param name="discardTip">When enabled, a riichi recommendation is computed for the human player.</param>
    /// <param name="sleepTime">The time to wait after any action (call or discard).</param>
    /// <returns>Instance of <see cref="AutoPlayResultPivot"/>.</returns>>
    public AutoPlayResultPivot RunAutoPlay(
        CancellationToken cancellationToken,
        bool declinedHumanCall,
        bool humanRonPending,
        bool autoCallMahjong,
        bool discardTip,
        (TilePivot compensationTile, PlayerIndices? previousPlayerIndex)? humanKanCompensation,
        int sleepTime)
    {
        return new AutoPlayEnginePivot(this).Run(cancellationToken, declinedHumanCall, humanRonPending, autoCallMahjong, discardTip, humanKanCompensation, sleepTime);
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
        if (_wallTiles.Count == 0 || _discardHistory.Discards[(int)PreviousPlayerIndex].Count == 0 || _waitForDiscard || IsRiichi(CurrentPlayerIndex))
        {
            return new List<TilePivot>();
        }

        var tile = _discardHistory.Discards[(int)PreviousPlayerIndex][^1];
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
            && _discardHistory.Discards[(int)PreviousPlayerIndex].Count != 0
            && !_waitForDiscard && !IsRiichi(playerIndex)
            && _hands[(int)playerIndex].ConcealedTiles.Where(t => t == _discardHistory.Discards[(int)PreviousPlayerIndex][^1]).Count() >= 2;
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
            if (_waitForDiscard || PreviousPlayerIndex == playerIndex || _discardHistory.Discards[(int)PreviousPlayerIndex].Count == 0 || IsRiichi(playerIndex))
            {
                return new List<TilePivot>();
            }

            var referenceTileFromDiscard = _discardHistory.Discards[(int)PreviousPlayerIndex][^1];
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

        var stolenTile = _discardHistory.TakeLastDiscard(PreviousPlayerIndex);

        _hands[(int)CurrentPlayerIndex].DeclareChii(
            stolenTile,
            Game.GetPlayerCurrentWind(PreviousPlayerIndex),
            Math.Min(pickInSequence.Number, stolenTile.Number)
        );
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
            _discardHistory.TakeLastDiscard(PreviousPlayerIndex),
            Game.GetPlayerCurrentWind(PreviousPlayerIndex)
        );
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
                _discardHistory.RecordVirtualOnly(playerIndex, tileChoice);
            }
            isClosedKan = true;
        }
        else
        {
            _hands[(int)playerIndex].DeclareKan(
                _discardHistory.TakeLastDiscard(PreviousPlayerIndex),
                Game.GetPlayerCurrentWind(PreviousPlayerIndex),
                null
            );
            CurrentPlayerIndex = playerIndex;
            _stealingInProgress = true;
        }

        // A genuine ankan is the only "own turn" kan that isn't an upgrade of an existing pon
        // (fromPreviousPon == null); every other case (shouminkan, or the "else" branch above which
        // is always a daiminkan) is an open kan for dora-reveal-timing purposes.
        _lastKanWasOpen = !(isClosedKan && fromPreviousPon == null);

        _waitForDiscard = true;

        return PickCompensationTile(isClosedKan);
    }

    /// <summary>
    /// Confirms that the most recently called kan (see <see cref="CallKan"/>) survived the chankan
    /// window (nobody called ron on it): reveals its dora indicator immediately if it was a genuine
    /// ankan, or queues the reveal until the discard that follows it if it was an open kan (daiminkan
    /// or shouminkan) - see <see cref="VisibleDorasCount"/>.
    /// </summary>
    internal void ResolveKanDoraReveal()
    {
        if (_lastKanWasOpen)
        {
            _pendingOpenKanDoraReveals++;
        }
        else
        {
            _visibleDorasCount++;
        }
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
        var riichiTurnsCount = _discardHistory.Discards[(int)CurrentPlayerIndex].Count;
        var isUninterruptedFirstTurn = _discardHistory.Discards[(int)CurrentPlayerIndex].Count == 0 && IsUninterruptedHistory(CurrentPlayerIndex);

        if (!Discard(tile))
        {
            throw new InvalidOperationException("The discard post-riichi has failed for an unknow reason.");
        }

        _riichis[(int)PreviousPlayerIndex] = new RiichiPivot(riichiTurnsCount, isUninterruptedFirstTurn, tile,
            Enum.GetValues<PlayerIndices>().Where(i => i != PreviousPlayerIndex).Select(i => new KeyValuePair<PlayerIndices, int>(i, _discardHistory.VirtualDiscards[(int)i].Count)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
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

        // Freezing every opponent's current virtual-discard count, and extending (or, if a call just
        // interrupted the sequence, resetting) the clean turn-order history, are both handled inside
        // RecordDiscard - see DiscardHistoryPivot for why each survives (or doesn't) a call made by
        // someone else in the meantime.
        _discardHistory.RecordDiscard(CurrentPlayerIndex, tile, _stealingInProgress || _closedKanInProgress != null);

        _stealingInProgress = false;
        _closedKanInProgress = null;
        _openedKanInProgress = null;
        _waitForDiscard = false;
        CurrentPlayerIndex = CurrentPlayerIndex.RelativePlayerIndex(1);

        // An open kan's dora indicator stays hidden until precisely this moment (see
        // ResolveKanDoraReveal); the notifier is re-raised here purely so the UI, which only redraws
        // the dora panel from that event, catches up on a reveal that didn't happen at kan time.
        if (_pendingOpenKanDoraReveals > 0)
        {
            _visibleDorasCount += _pendingOpenKanDoraReveals;
            _pendingOpenKanDoraReveals = 0;
            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypes.Kan });
        }

        return true;
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
        return _discardHistory.Discards[(int)playerIndex];
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

    #endregion Public methods

    #region Internal methods

    /// <summary>
    /// Checks if the hand of the current player is ready for calling tsumo.
    /// </summary>
    /// <param name="isKanCompensation"><c>True</c> if the latest pick comes from a kan compensation.</param>
    /// <returns><c>True</c> if ready for tsumo; <c>False</c> otherwise.</returns>
    internal bool CanCallTsumo(bool isKanCompensation)
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
    /// Checks if the current player can call riichi.
    /// </summary>
    /// <returns>Tiles the player can discard; empty list if riichi is impossible.</returns>
    internal IReadOnlyList<TilePivot> CanCallRiichi()
    {
        if (!CanConsiderRiichi())
        {
            return new List<TilePivot>();
        }

        return ExtractDiscardChoicesFromTenpai(CurrentPlayerIndex);
    }

    /// <summary>
    /// Checks if the current player can declare "kyuushu kyuuhai" (nine different terminals/honours):
    /// an optional abortive draw, only available on the player's own first turn, and only if nothing
    /// (call or concealed kan) has interrupted the round since its very beginning.
    /// </summary>
    /// <returns><c>True</c> if the declaration is currently allowed; <c>False</c> otherwise.</returns>
    internal bool CanCallKyuushuKyuuhai()
    {
        return _waitForDiscard
            && _discardHistory.Discards[(int)CurrentPlayerIndex].Count == 0
            && IsUninterruptedHistory(CurrentPlayerIndex)
            && _hands[(int)CurrentPlayerIndex].ConcealedTiles.Where(t => t.IsHonorOrTerminal).Distinct().Count() >= 9;
    }

    /// <summary>
    /// Declares "kyuushu kyuuhai": ends the round immediately as an abortive draw.
    /// </summary>
    /// <returns><c>True</c> if the declaration succeeded; <c>False</c> otherwise (see <see cref="CanCallKyuushuKyuuhai"/>).</returns>
    public bool CallKyuushuKyuuhai()
    {
        if (!CanCallKyuushuKyuuhai())
        {
            return false;
        }

        IsKyuushuKyuuhai = true;
        return true;
    }

    // Guards of CanCallRiichi that don't require computing ExtractDiscardChoicesFromTenpai: whenever
    // this is false, the current player's tenpai status was never actually checked (open hand, already
    // riichi, not enough wall/points...), as opposed to "checked and found not tenpai".
    internal bool CanConsiderRiichi()
    {
        return _waitForDiscard
            && !IsRiichi(CurrentPlayerIndex)
            && _hands[(int)CurrentPlayerIndex].IsConcealed
            && _wallTiles.Count >= 4
            && Game.Players[(int)CurrentPlayerIndex].CurrentGamePoints >= ScoreTools.RIICHI_COST;
    }

    /// <summary>
    /// Checks if the hand of the specified player is ready for calling ron.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <returns><c>True</c> if calling ron is possible; <c>False</c> otherwise.</returns>
    internal bool CanCallRon(PlayerIndices playerIndex)
    {
        var tile = _waitForDiscard ? null : _discardHistory.Discards[(int)PreviousPlayerIndex].LastOrDefault();
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
            && !_hands[(int)playerIndex].CancelYakusIfFuriten(_discardHistory.Discards[(int)playerIndex], GetTilesFromVirtualDiscardsAtRank(playerIndex, tile))
            && !_hands[(int)playerIndex].CancelYakusIfTemporaryFuriten(GetTilesFromVirtualDiscardsSinceLastOwnDiscard(playerIndex, tile));
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

        return hand.IsTenpai(_fullTilesList, tileToRemoveFromConcealed);
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

        // note: this algorithm makes sense only with tiles ordered with honors in last
        TilePivot? refT = null;
        var countRefT = 0;
        var singleCount = 0;
        var j = 0;
        foreach (var t in hand.ConcealedTiles)
        {
            if (t.IsHonor)
            {
                if (refT == null)
                {
                    refT = t;
                    countRefT = 1;
                }
                else if (refT == t)
                {
                    countRefT++;
                }
                else
                {
                    if (countRefT < 2)
                    {
                        singleCount++;
                        if (singleCount > 1)
                            break;
                    }
                    refT = t;
                    countRefT = 1;
                }
            }
            j++;
            if (j == hand.ConcealedTiles.Count)
            {
                if (countRefT < 2)
                    singleCount++;
            }
        }
        var skipBasic = singleCount > 1;

        var subPossibilities = new List<TilePivot>(tilesToSub.Count);
        foreach (var tileToSub in tilesToSub)
        {
            var tempListConcealed = new List<TilePivot>(hand.ConcealedTiles);
            tempListConcealed.Remove(tileToSub);
            if (TileCombinatoricsPivot.IsTenpai(tempListConcealed, hand.DeclaredCombinations, distinctTilesFromOverallConcealed, skipBasic))
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
        return new EndOfRoundCalculatorPivot(this).Compute(ronPlayerIndex);
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

    /// <summary>
    /// Gets dora count if the specified tile is a dora.
    /// </summary>
    /// <param name="t">The dora tile.</param>
    /// <returns>Dora count.</returns>
    internal int GetDoraCount(TilePivot t) => GetDoraCountInternal(t, DoraIndicatorTiles);

    /// <summary>
    /// Gets dora count if the specified tile is an uradora.
    /// </summary>
    /// <param name="t">The dora tile.</param>
    /// <returns>Ura-dora count.</returns>
    internal int GetUraDoraCount(TilePivot t) => GetDoraCountInternal(t, UraDoraIndicatorTiles);

    #endregion Internal methods

    #region Private methods

    // Checks if a priority call can be made by the specified player.
    internal bool CanCallPonOrKan(PlayerIndices playerIndex, out bool isSelfKan)
    {
        isSelfKan = _waitForDiscard;
        return CanCallKan(playerIndex).Count > 0 || CanCallPon(playerIndex);
    }

    // Tries to pick the next tile from the wall.
    private void Pick()
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

    internal TilePivot? OpponentBeginCallKan(PlayerIndices playerId, TilePivot kanTilePick, bool concealedKan)
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        var compensationTile = CallKan(playerId, concealedKan ? kanTilePick : null);
        if (compensationTile != null)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = playerId, Action = CallTypes.Kan });
        }
        return compensationTile;
    }

    internal void AutoPick()
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        Pick();

        PickNotifier?.Invoke(new PickNotifierEventArgs());
    }

    internal void ChiiCall(TilePivot chiiTilePick, int sleepTime)
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        var callChii = CallChii(chiiTilePick);
        if (callChii)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypes.Chii, PlayerIndex = CurrentPlayerIndex });

            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypes.Chii });

            if (!IsHumanPlayer)
            {
                var discardDecision = _cpuManagers[CurrentPlayerIndex].DiscardDecision();
                Discard(discardDecision, sleepTime);
            }
        }
    }

    internal void PonCall(PlayerIndices playerIndex, int sleepTime)
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
                var discardDecision = _cpuManagers[CurrentPlayerIndex].DiscardDecision();
                Discard(discardDecision, sleepTime);
            }
        }
    }

    internal void Discard(TilePivot tile, int sleepTime)
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

    internal void CallRiichi(TilePivot tile, int sleepTime)
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

    // Undoes the pick of a compensation tile after a kan.
    internal void UndoPickCompensationTile()
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
                fullList.AddRange(_discardHistory.VirtualDiscards[(int)i].Skip(opponentRank));
            }
        }

        return fullList.Where(t => !ReferenceEquals(t, exceptTile)).ToList();
    }

    // Gets every tile discarded by opponents since the specified player's own last discard.
    // Unlike "PlayerIndexHistory", this is immune to call (pon / chii / kan) interruptions in between,
    // which is required by the temporary furiten rule: it lasts until the player's own next discard,
    // regardless of any call made by someone else in the meantime.
    internal List<TilePivot> GetTilesFromVirtualDiscardsSinceLastOwnDiscard(PlayerIndices playerIndex, TilePivot exceptTile)
    {
        var fullList = new List<TilePivot>(20);

        foreach (var opponent in Enum.GetValues<PlayerIndices>())
        {
            if (opponent != playerIndex)
            {
                var rank = _discardHistory.GetLastOwnDiscardOpponentRank(playerIndex, opponent);
                fullList.AddRange(_discardHistory.VirtualDiscards[(int)opponent].Skip(rank));
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
        var historySinceLastTime = _discardHistory.PlayerIndexHistory.TakeWhile(i => i != playerIndex).ToList();

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
            isFirstOrLast: IsWallExhaustion ? (bool?)null : (_discardHistory.Discards[(int)playerIndex].Count == 0 && IsUninterruptedHistory(playerIndex)),
            isRiichi: IsRiichi(playerIndex) ? (_riichis[(int)playerIndex]!.IsDaburu ? (bool?)null : true) : false,
            isIppatsu: IsIppatsu(playerIndex)
        ));
    }

    // Checks if the specified player is ippatsu.
    private bool IsIppatsu(PlayerIndices playerIndex)
    {
        return IsRiichi(playerIndex)
            && _discardHistory.Discards[(int)playerIndex].Count > 0
            && ReferenceEquals(_discardHistory.Discards[(int)playerIndex][^1], _riichis[(int)playerIndex]!.Tile)
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

    // Gets the count of dora for specified tile
    private int GetDoraCountInternal(TilePivot t, IReadOnlyList<TilePivot> doraIndicators)
    {
        return doraIndicators.Take(VisibleDorasCount).Count(t.IsDoraNext);
    }

    #endregion Private methods
}