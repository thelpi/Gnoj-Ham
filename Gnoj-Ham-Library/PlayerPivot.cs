using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a player.
/// </summary>
public class PlayerPivot
{
    private const string CPU_NAME_PREFIX = "CPU_";
    private const string DEFAULT_HUMAN_NAME = "Empty";

    private readonly List<PlayerScorePivot> _scores = new(10);

    /// <summary>
    /// Name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// <see cref="Winds"/> at the first round of the current game.
    /// </summary>
    public Winds CurrentGameInitialWind { get; private set; }

    /// <summary>
    /// Number of points in the current game.
    /// </summary>
    public int CurrentGamePoints { get; private set; }

    #region Overall statistics

    /// <summary>
    /// Number of games played.
    /// </summary>
    internal int GamesCount => _scores.Count;

    /// <summary>
    /// Cumulated score.
    /// </summary>
    public int TotalScore => _scores.Sum(s => s.Score);

    /// <summary>
    /// Number of first places.
    /// </summary>
    public int FirstPlaceCount => _scores.Count(s => s.Rank == 1);

    /// <summary>
    /// Number of first or second places.
    /// </summary>
    public int FirstOrSecondPlaceCount => _scores.Count(s => s.Rank == 1 || s.Rank == 2);

    /// <summary>
    /// Number of last places.
    /// </summary>
    public int LastPlaceCount => _scores.Count(s => s.Rank == GamePivot.PlayersCount);

    /// <summary>
    /// Average score.
    /// </summary>
    public double AverageScore => _scores.Average(s => s.Score);

    /// <summary>
    /// Average rank.
    /// </summary>
    public double AverageRank => _scores.Average(s => s.Rank);

    #endregion Overall statistics

    private PlayerPivot(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Adds a specified amount of points to the current player.
    /// </summary>
    /// <param name="points">The points count to add; might be negative.</param>
    internal void AddPoints(int points)
    {
        CurrentGamePoints += points;
    }

    /// <summary>
    /// Builds a collection of four players.
    /// </summary>
    /// <param name="humanPlayer">Human player information; <c>Null</c> if all players are CPU.</param>
    /// <param name="cpuNameSuffixes">
    /// Optional; for any CPU seat present in the dictionary, its display name gets suffixed with this
    /// (e.g. "CPU_1 (no defense)") - meant to make a benchmark's scoreboard readable at a glance when
    /// different seats play through different <see cref="CpuManagerBasePivot"/> implementations (see
    /// <see cref="CpuManagerCatalog"/>). <c>Null</c> (default), or a seat missing from the dictionary,
    /// leaves that seat's plain name untouched.
    /// </param>
    /// <returns>Four players.</returns>
    public static IReadOnlyList<PlayerPivot> BuildPlayers(
        (PlayerIndices index, string name)? humanPlayer,
        IReadOnlyDictionary<PlayerIndices, string>? cpuNameSuffixes = null)
    {
        return Enum.GetValues<PlayerIndices>()
            .Select(i => new PlayerPivot(humanPlayer.HasValue && i == humanPlayer.Value.index
                ? (string.IsNullOrWhiteSpace(humanPlayer.Value.name)
                    ? DEFAULT_HUMAN_NAME
                    : humanPlayer.Value.name.Trim())
                : $"{CPU_NAME_PREFIX}{(int)i}{(cpuNameSuffixes != null && cpuNameSuffixes.TryGetValue(i, out var suffix) ? $" ({suffix})" : string.Empty)}"))
            .ToList();
    }

    /// <summary>
    /// Prepares players for the next game.
    /// </summary>
    /// <param name="players">Players to prepare.</param>
    /// <param name="initialPointsRulePivot">Initial points rule.</param>
    /// <param name="random"><see cref="Random"/> instance; to determine which player is <see cref="Winds.East"/>>..</param>
    internal static void SetPlayersForNewGame(IReadOnlyList<PlayerPivot> players,
        InitialPointsRules initialPointsRulePivot,
        Random random)
    {
        var eastIndex = (PlayerIndices)random.Next(GamePivot.PlayersCount);

        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            players[(int)i].CurrentGameInitialWind = i.WindFrom(eastIndex);
            players[(int)i].CurrentGamePoints = initialPointsRulePivot.GetInitialPointsFromRule();
        }
    }

    /// <summary>
    /// Add a score sheet for on game.
    /// </summary>
    /// <param name="score"></param>
    internal void AddGameScore(PlayerScorePivot score)
    {
        _scores.Add(score);
    }

    /// <summary>
    /// Resets player's score.
    /// </summary>
    internal void ResetScores()
    {
        _scores.Clear();
    }
}
