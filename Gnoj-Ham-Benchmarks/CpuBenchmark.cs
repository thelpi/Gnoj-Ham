using System.Diagnostics;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Benchmarks;

/// <summary>
/// Plays whole games of one CPU against three others, all on the machine's cores at once, to measure
/// the challenger's results.
/// </summary>
internal static class CpuBenchmark
{
    /// <summary>
    /// Plays the games. The challenger takes each seat in turn, so that where it sits is worth nothing to it;
    /// every game is played from its own seed (<paramref name="firstSeed"/> then the following ones), so the
    /// same batch always gives the same results.
    /// </summary>
    /// <param name="challenger">The CPU measured (a type of <see cref="CpuManagerCatalog"/>).</param>
    /// <param name="opponent">The CPU of the three other seats (a type of <see cref="CpuManagerCatalog"/>).</param>
    /// <param name="gamesCount">The games to play.</param>
    /// <param name="firstSeed">The seed of the first game.</param>
    /// <param name="onGameOver">Called each time a game is over, with how many are.</param>
    /// <param name="cancellationToken">Stops the batch.</param>
    /// <returns>What the challenger achieved.</returns>
    internal static BenchmarkResult Run(
        Type challenger,
        Type opponent,
        int gamesCount,
        int firstSeed,
        Action<int> onGameOver,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var ranks = new double[gamesCount];
        var points = new double[gamesCount];
        var overCount = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        Parallel.For(0, gamesCount, options, gameIndex =>
        {
            var seat = (PlayerIndices)(gameIndex % GamePivot.PlayersCount);
            var game = PlayGame(seat, challenger, opponent, firstSeed + gameIndex, cancellationToken);

            var challengerPoints = game.Players[(int)seat].CurrentGamePoints;
            ranks[gameIndex] = 1 + game.Players.Count(p => p.CurrentGamePoints > challengerPoints);
            points[gameIndex] = challengerPoints;

            onGameOver(Interlocked.Increment(ref overCount));
        });

        return new BenchmarkResult(
            gamesCount,
            ranks.Average(),
            StandardError(ranks),
            points.Average(),
            StandardError(points),
            ranks.Count(r => r == 1) / (double)gamesCount,
            ranks.Count(r => r == GamePivot.PlayersCount) / (double)gamesCount,
            stopwatch.Elapsed);
    }

    private static GamePivot PlayGame(PlayerIndices challengerSeat, Type challenger, Type opponent, int seed, CancellationToken cancellationToken)
    {
        var factories = GamePivot.PerPlayer(seat => seat == challengerSeat ? challenger : opponent)
            .Select((type, index) => (index, factory: (Func<RoundPivot, CpuManagerBasePivot>)(round => (CpuManagerBasePivot)Activator.CreateInstance(type, round)!)))
            .ToDictionary(e => (PlayerIndices)e.index, e => e.factory);

        var game = new GamePivot(RulePivot.Default, PlayerPivot.BuildPlayers(null), new Random(seed), factories);

        EndOfRoundInformationsPivot endOfRound;
        do
        {
            var result = game.Round.RunAutoPlay(cancellationToken);

            // The engine just stops playing when cancelled: what follows would be a truncated round.
            cancellationToken.ThrowIfCancellationRequested();

            endOfRound = game.NextRound(result.RonPlayerId);
        } while (!endOfRound.EndOfGame);

        return game;
    }

    private static double StandardError(double[] values)
    {
        var mean = values.Average();
        var variance = values.Sum(v => (v - mean) * (v - mean)) / Math.Max(1, values.Length - 1);
        return Math.Sqrt(variance / values.Length);
    }
}
