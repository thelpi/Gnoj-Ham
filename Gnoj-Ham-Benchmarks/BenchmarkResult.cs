namespace Gnoj_Ham_Benchmarks;

/// <summary>
/// What a CPU achieved in a batch of games against three others; the rank goes from 1 to 4, and the points
/// are the ones left at the end of the game.
/// </summary>
/// <param name="GamesCount">The games played.</param>
/// <param name="RankMean">The mean rank; 2.5 for a CPU no better than the others.</param>
/// <param name="RankStandardError">How far from the real mean rank the measured one may be.</param>
/// <param name="PointsMean">The mean points at the end of the game.</param>
/// <param name="PointsStandardError">How far from the real mean points the measured one may be.</param>
/// <param name="FirstRate">The share of games finished first; 0.25 for a CPU no better than the others.</param>
/// <param name="LastRate">The share of games finished last; 0.25 for a CPU no better than the others.</param>
/// <param name="Elapsed">The time it took.</param>
internal sealed record BenchmarkResult(
    int GamesCount,
    double RankMean,
    double RankStandardError,
    double PointsMean,
    double PointsStandardError,
    double FirstRate,
    double LastRate,
    TimeSpan Elapsed);
