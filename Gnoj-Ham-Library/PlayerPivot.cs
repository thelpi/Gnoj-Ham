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
    /// Permanent player the current instance is based upon.
    /// </summary>
    public PermanentPlayerPivot? PermanentPlayer { get; }

    private PlayerPivot(string name, Winds initialWind, InitialPointsRules initialPointsRulePivot, PermanentPlayerPivot? permanentPlayer)
    {
        Name = name;
        InitialWind = initialWind;
        Points = initialPointsRulePivot.GetInitialPointsFromRule();
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
    /// <param name="humanPlayers">Collection of human players; other players will be <see cref="IsCpu"/>.</param>
    /// <param name="initialPointsRulePivot">Rule for initial points count.</param>
    /// <param name="random">Randomizer instance.</param>
    /// <returns>List of four <see cref="PlayerPivot"/>, not sorted.</returns>
    internal static IReadOnlyList<PlayerPivot> GetFourPlayers(
        IDictionary<PlayerIndices, string?> humanPlayers,
        InitialPointsRules initialPointsRulePivot,
        Random random)
    {
        CheckName(humanPlayers);

        var eastIndex = (PlayerIndices)random.Next(0, 4);

        var players = new List<PlayerPivot>(4);
        foreach (var i in Enum.GetValues<PlayerIndices>())
        {
            players.Add(new PlayerPivot(
                humanPlayers.TryGetValue(i, out var value) ? value! : $"{CPU_NAME_PREFIX}{i}",
                GetWindFromIndex(eastIndex, i),
                initialPointsRulePivot,
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
            .Select((p, i) => new PlayerPivot($"{CPU_NAME_PREFIX}{i}", GetWindFromIndex(eastIndex, (PlayerIndices)i), initialPointsRulePivot, p))
            .ToList();
    }

    private static Winds GetWindFromIndex(PlayerIndices eastIndex, PlayerIndices i)
        => i == eastIndex ? Winds.East : (i > eastIndex ? (Winds)(i - eastIndex) : (Winds)(4 - (int)eastIndex + i));

    private static void CheckName(IDictionary<PlayerIndices, string?> humanPlayers)
    {
        for (var i =  0; i < humanPlayers.Count; i++)
        {
            var key = humanPlayers.ElementAt(i).Key;
            var name = (humanPlayers[key] ?? string.Empty).Trim();
            humanPlayers[key] = name == string.Empty || name.StartsWith(CPU_NAME_PREFIX, StringComparison.InvariantCultureIgnoreCase)
                ? $"{DEFAULT_HUMAN_NAME}-{i}"
                : name;
        }
    }
}
