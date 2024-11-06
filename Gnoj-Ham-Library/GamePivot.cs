using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a game.
/// </summary>
public class GamePivot
{
    #region Properties

    private readonly PlayerSavePivot? _save;
    private readonly Random _random;

    /// <summary>
    /// Human player index (if any).
    /// </summary>
    public PlayerIndices? HumanPlayerIndex { get; }
    /// <summary>
    /// List of players.
    /// </summary>
    public IReadOnlyList<PlayerPivot> Players { get; }
    /// <summary>
    /// Current dominant wind.
    /// </summary>
    public Winds DominantWind { get; private set; }
    /// <summary>
    /// Index of the player in <see cref="Players"/> currently east.
    /// </summary>
    internal PlayerIndices EastIndex { get; private set; }
    /// <summary>
    /// Number of rounds with the current <see cref="EastIndex"/>.
    /// </summary>
    internal int EastIndexTurnCount { get; private set; }
    /// <summary>
    /// Honba count.
    /// </summary>
    /// <remarks>For scoring, <see cref="HonbaCountBeforeScoring"/> should be used.</remarks>
    public int HonbaCount { get; private set; }
    /// <summary>
    /// Pending riichi count.
    /// </summary>
    public int PendingRiichiCount { get; private set; }
    /// <summary>
    /// East rank (1, 2, 3, 4).
    /// </summary>
    public int EastRank { get; private set; }
    /// <summary>
    /// Current <see cref="RoundPivot"/>.
    /// </summary>
    public RoundPivot Round { get; private set; }
    /// <summary>
    /// The ruleset for the game.
    /// </summary>
    public RulePivot Ruleset { get; }
    /// <summary>
    /// Honba count before scoring.
    /// </summary>
    internal int HonbaCountBeforeScoring => HonbaCount > 0 ? HonbaCount - 1 : 0;
    /// <summary>
    /// Inferred; current east player.
    /// </summary>
    internal PlayerPivot CurrentEastPlayer => Players[(int)EastIndex];
    /// <summary>
    /// Inferred; get players sorted by their ranking.
    /// </summary>
    internal IReadOnlyList<PlayerPivot> PlayersRanked => Players.OrderByDescending(p => p.CurrentGamePoints).ThenBy(p => (int)p.CurrentGameInitialWind).ToList();
    /// <summary>
    /// Inferred; gets the player index which was the first <see cref="Winds.East"/>.
    /// </summary>
    internal PlayerIndices FirstEastIndex => (PlayerIndices)Players.Select((p, i) => (p, i)).First(pi => pi.p.CurrentGameInitialWind == Winds.East).i; // TODO: gross

    #endregion Properties

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="humanPlayerName">Human player name.</param>
    /// <param name="ruleset">Ruleset for the game.</param>
    /// <param name="save">Player save stats.</param>
    /// <param name="random">Randomizer instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="save"/> is <c>Null</c> while ruleset is default.</exception>
    public GamePivot(string humanPlayerName, RulePivot ruleset, PlayerSavePivot? save, Random random)
    {
        if (ruleset.AreDefaultRules() && save == null)
        {
            throw new ArgumentNullException(nameof(save));
        }

        _save = save;

        var players = PlayerPivot.BuildPlayers((PlayerIndices.Zero, humanPlayerName));
        PlayerPivot.SetPlayersForNewGame(players, ruleset.InitialPointsRule, random);

        HumanPlayerIndex = PlayerIndices.Zero;
        Ruleset = ruleset;
        Players = players;
        DominantWind = Winds.East;
        EastIndexTurnCount = 1;
        EastIndex = FirstEastIndex;
        EastRank = 1;
        _random = random;

        Round = new RoundPivot(this, EastIndex, random);
    }

    /// <summary>
    /// Constructor for CPU game.
    /// </summary>
    /// <param name="ruleset">Ruleset for the game.</param>
    /// <param name="players">Four players.</param>
    /// <param name="random">Randomizer instance.</param>
    /// <exception cref="ArgumentException">Four players are required.</exception>
    public GamePivot(RulePivot ruleset, IReadOnlyList<PlayerPivot> players, Random random)
    {
        if (players.Count != 4)
        {
            throw new ArgumentException("Four players are required.", nameof(players));
        }

        PlayerPivot.SetPlayersForNewGame(players, ruleset.InitialPointsRule, random);

        Ruleset = ruleset;
        Players = players;
        DominantWind = Winds.East;
        EastIndexTurnCount = 1;
        EastIndex = FirstEastIndex;
        EastRank = 1;
        _random = random;

        Round = new RoundPivot(this, EastIndex, random);
    }

    #region Public methods

    /// <summary>
    /// Computes the rank and score of every players at the current state of the game.
    /// </summary>
    /// <returns>A list of player with score, order by ascending rank.</returns>
    public IReadOnlyList<PlayerScorePivot> ComputeCurrentRanking()
    {
        var playersOrdered = new List<PlayerScorePivot>(4);

        var i = 1;
        foreach (var player in Players.OrderByDescending(p => p.CurrentGamePoints))
        {
            playersOrdered.Add(new PlayerScorePivot(player, i, ScoreTools.ComputeUma(i), Ruleset.InitialPointsRule.GetInitialPointsFromRule()));
            i++;
        }

        return playersOrdered;
    }

    /// <summary>
    /// Manages the end of the current round and generates a new one.
    /// <see cref="Round"/> stays <c>Null</c> at the end of the game.
    /// </summary>
    /// <param name="ronPlayerIndex">The player index on who the call has been made; <c>Null</c> if tsumo or ryuukyoku.</param>
    /// <returns>An instance of <see cref="EndOfRoundInformationsPivot"/> and potentiel error on save of statistics.</returns>
    public (EndOfRoundInformationsPivot endOfRoundInformation, string? error) NextRound(PlayerIndices? ronPlayerIndex)
    {
        var endOfRoundInformations = Round.EndOfRound(ronPlayerIndex);

        // used for stats ONLY, when one player ONLY
        var humanIsRiichi = HumanPlayerIndex.HasValue && Round.IsRiichi(HumanPlayerIndex.Value);
        var humanIsConcealed = HumanPlayerIndex.HasValue && Round.GetHand(HumanPlayerIndex.Value).IsConcealed;

        if (!endOfRoundInformations.Ryuukyoku)
        {
            PendingRiichiCount = 0;
        }

        if (!endOfRoundInformations.ToNextEast || endOfRoundInformations.Ryuukyoku)
        {
            HonbaCount++;
        }
        else
        {
            HonbaCount = 0;
        }

        if (Ruleset.EndOfGameRule.TobiRuleApply() && Players.Any(p => p.CurrentGamePoints < 0))
        {
            endOfRoundInformations.EndOfGame = true;
            ClearPendingRiichi();
            goto Exit;
        }

        if (DominantWind == Winds.West || DominantWind == Winds.North)
        {
            if (!endOfRoundInformations.Ryuukyoku && Players.Any(p => p.CurrentGamePoints >= 30000))
            {
                endOfRoundInformations.EndOfGame = true;
                ClearPendingRiichi();
                goto Exit;
            }
        }

        if (endOfRoundInformations.ToNextEast)
        {
            EastIndex = EastIndex.RelativePlayerIndex(1);
            EastIndexTurnCount = 1;
            EastRank++;

            if (EastIndex == FirstEastIndex)
            {
                EastRank = 1;
                if (DominantWind == Winds.South)
                {
                    if (Ruleset.EndOfGameRule.EnchousenRuleApply()
                        && Ruleset.InitialPointsRule == InitialPointsRules.K25
                        && Players.All(p => p.CurrentGamePoints < 30000))
                    {
                        DominantWind = Winds.West;
                    }
                    else
                    {
                        endOfRoundInformations.EndOfGame = true;
                        ClearPendingRiichi();
                        goto Exit;
                    }
                }
                else if (DominantWind == Winds.West)
                {
                    DominantWind = Winds.North;
                }
                else if (DominantWind == Winds.North)
                {
                    endOfRoundInformations.EndOfGame = true;
                    ClearPendingRiichi();
                    goto Exit;
                }
                else
                {
                    DominantWind = Winds.South;
                }
            }
        }
        else
        {
            EastIndexTurnCount++;
        }

        Round = new RoundPivot(this, EastIndex, _random);

    Exit:
        string? error = null;
        if (Ruleset.AreDefaultRules() && HumanPlayerIndex.HasValue)
        {
            var humanPlayer = Players[(int)HumanPlayerIndex.Value];
            error = _save!.UpdateAndSave(endOfRoundInformations,
                ronPlayerIndex.HasValue,
                humanIsRiichi,
                humanIsConcealed,
                Players.OrderByDescending(_ => _.CurrentGamePoints).ToList().IndexOf(humanPlayer),
                humanPlayer.CurrentGamePoints);
        }

        return (endOfRoundInformations, error);
    }

    /// <summary>
    /// Gets the current <see cref="Winds"/> of the specified player.
    /// </summary>
    /// <param name="playerIndex">The player index in <see cref="Players"/>.</param>
    /// <returns>The <see cref="Winds"/>.</returns>
    public Winds GetPlayerCurrentWind(PlayerIndices playerIndex)
    {
        if (playerIndex == EastIndex + 1 || playerIndex == EastIndex - 3)
        {
            return Winds.South;
        }
        else if (playerIndex == EastIndex + 2 || playerIndex == EastIndex - 2)
        {
            return Winds.West;
        }
        else if (playerIndex == EastIndex + 3 || playerIndex == EastIndex - 1)
        {
            return Winds.North;
        }

        return Winds.East;
    }

    /// <summary>
    /// Indicates if the current index is an human player.
    /// </summary>
    /// <param name="i">The player index.</param>
    /// <returns><c>True</c> if human; <c>False</c> otherwise.</returns>
    public bool IsHuman(PlayerIndices i)
        => i == HumanPlayerIndex;

    /// <summary>
    /// Indicates if the current index is not an human player.
    /// </summary>
    /// <param name="i">The player index.</param>
    /// <returns><c>False</c> if human; <c>True</c> otherwise.</returns>
    public bool IsCpu(PlayerIndices i)
        => !IsHuman(i);

    #endregion Public methods

    /// <summary>
    /// Adds a pending riichi.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    internal void AddPendingRiichi(PlayerIndices playerIndex)
    {
        PendingRiichiCount++;
        Players[(int)playerIndex].AddPoints(-ScoreTools.RIICHI_COST);
    }

    /// <summary>
    /// Gets the player index for the specified wind.
    /// </summary>
    /// <param name="wind">The wind.</param>
    /// <returns>The player index.</returns>
    internal PlayerIndices GetPlayerIndexByCurrentWind(Winds wind)
    {
        return Enum.GetValues<PlayerIndices>().First(i => GetPlayerCurrentWind(i) == wind);
    }

    // At the end of the game, manage the remaining pending riichi.
    private void ClearPendingRiichi()
    {
        if (PendingRiichiCount > 0)
        {
            var winner = Players.OrderByDescending(p => p.CurrentGamePoints).First();
            var everyWinner = Players.Where(p => p.CurrentGamePoints == winner.CurrentGamePoints).ToList();
            if (PendingRiichiCount * ScoreTools.RIICHI_COST % everyWinner.Count != 0)
            {
                // This is ugly...
                everyWinner.Remove(everyWinner[^1]);
            }
            foreach (var w in everyWinner)
            {
                w.AddPoints(PendingRiichiCount * ScoreTools.RIICHI_COST / everyWinner.Count);
            }
        }
    }
}
