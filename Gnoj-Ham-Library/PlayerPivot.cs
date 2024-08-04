using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a player.
/// </summary>
public class PlayerPivot
{
    private const string CPU_NAME_PREFIX = "CPU_";
    private const string DEFAULT_HUMAN_NAME = "Empty";

    /// <summary>
    /// Name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// <see cref="Winds"/> at the first round of the game.
    /// </summary>
    public Winds InitialWind { get; }

    /// <summary>
    /// Number of points.
    /// </summary>
    public int Points { get; private set; }

    /// <summary>
    /// Indicates if the player is managed by the CPU.
    /// </summary>
    public bool IsCpu { get; }

    /// <summary>
    /// Permanent player the current instance is based upon.
    /// </summary>
    public PermanentPlayerPivot? PermanentPlayer { get; }

    private PlayerPivot(string name, Winds initialWind, InitialPointsRules initialPointsRulePivot, bool isCpu, PermanentPlayerPivot? permanentPlayer)
    {
        Name = name;
        InitialWind = initialWind;
        Points = initialPointsRulePivot.GetInitialPointsFromRule();
        IsCpu = isCpu;
        PermanentPlayer = permanentPlayer;
    }

    /// <summary>
    /// Adds a specified amount of points to the current player.
    /// </summary>
    /// <param name="points">The points count to add; might be negative.</param>
    internal void AddPoints(int points)
    {
        Points += points;
    }

    /// <summary>
    /// Generates a list of four <see cref="PlayerPivot"/> to start a game.
    /// </summary>
    /// <param name="humanPlayerName">The name of the human player; other players will be <see cref="IsCpu"/>.</param>
    /// <param name="initialPointsRulePivot">Rule for initial points count.</param>
    /// <param name="random">Randomizer instance.</param>
    /// <returns>List of four <see cref="PlayerPivot"/>, not sorted.</returns>
    internal static IReadOnlyList<PlayerPivot> GetFourPlayers(string? humanPlayerName, InitialPointsRules initialPointsRulePivot, Random random)
    {
        humanPlayerName = CheckName(humanPlayerName);

        var eastIndex = (PlayerIndices)random.Next(0, 4);

        var players = new List<PlayerPivot>(4);
        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            players.Add(new PlayerPivot(
                i == GamePivot.HUMAN_INDEX ? humanPlayerName : $"{CPU_NAME_PREFIX}{i}",
                GetWindFromIndex(eastIndex, i),
                initialPointsRulePivot,
                i != GamePivot.HUMAN_INDEX,
                null
            ));
        }

        return players;
    }

    /// <summary>
    /// Generates a list of four <see cref="PlayerPivot"/> from permanent players.
    /// </summary>
    /// <param name="permanentPlayers">Four permanent players.</param>
    /// <param name="initialPointsRulePivot">Rule for initial points count.</param>
    /// <param name="random">Randomizer instance.</param>
    /// <returns>Four players generated.</returns>
    internal static IReadOnlyList<PlayerPivot> GetFourPlayersFromPermanent(IReadOnlyList<PermanentPlayerPivot> permanentPlayers, InitialPointsRules initialPointsRulePivot, Random random)
    {
        var eastIndex = (PlayerIndices)random.Next(0, 4);

        return permanentPlayers
            .Select((p, i) => new PlayerPivot($"{CPU_NAME_PREFIX}{i}", GetWindFromIndex(eastIndex, (PlayerIndices)i), initialPointsRulePivot, true, p))
            .ToList();
    }

    private static Winds GetWindFromIndex(PlayerIndices eastIndex, PlayerIndices i)
        => i == eastIndex ? Winds.East : (i > eastIndex ? (Winds)(i - eastIndex) : (Winds)(4 - (int)eastIndex + i));

    private static string CheckName(string? humanPlayerName)
    {
        humanPlayerName = (humanPlayerName ?? string.Empty).Trim();

        return humanPlayerName == string.Empty || humanPlayerName.StartsWith(CPU_NAME_PREFIX, StringComparison.InvariantCultureIgnoreCase)
            ? DEFAULT_HUMAN_NAME
            : humanPlayerName;
    }
}
