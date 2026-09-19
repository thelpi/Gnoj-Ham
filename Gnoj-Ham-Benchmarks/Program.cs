using System.Globalization;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_Benchmarks;

/// <summary>
/// Measures how much a CPU is worth against another: one of them takes each seat in turn against three of the
/// other, over a batch of whole games.
/// </summary>
/// <remarks>
/// <c>dotnet run -c Release --project Gnoj-Ham-Benchmarks -- efficiency basic 1500</c> plays 1500 games of the
/// "efficiency" CPU against three "basic" ones (the names are the ones of <see cref="CpuManagerCatalog"/>, listed by
/// <c>--list</c>). The same CPU on both sides is the way to see how much is only luck: about 2.5 for the mean rank.
/// A game takes around 0.3 s on each core; the mean rank is known to about 0.03 with 1500 games.
/// </remarks>
internal static class Program
{
    private const int DefaultGamesCount = 1000;
    private const int DefaultFirstSeed = 1;

    private static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--list")
        {
            ListCpus();
            return 0;
        }

        if (args.Length is < 2 or > 4)
        {
            Console.WriteLine("Usage: <challenger> <opponent> [games = 1000] [first seed = 1]   (or --list)");
            ListCpus();
            return 1;
        }

        var challenger = FindCpu(args[0]);
        var opponent = FindCpu(args[1]);
        if (challenger == null || opponent == null
            || !TryParse(args, 2, DefaultGamesCount, out var gamesCount) || gamesCount < 1
            || !TryParse(args, 3, DefaultFirstSeed, out var firstSeed))
        {
            Console.WriteLine("Unknown CPU, or invalid number of games or seed.");
            ListCpus();
            return 1;
        }

        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellation.Cancel();
        };

        Console.WriteLine($"{challenger.DisplayName} against three {opponent.DisplayName}: {gamesCount} games...");

        BenchmarkResult result;
        try
        {
            var progressStep = Math.Max(1, gamesCount / 20);
            result = CpuBenchmark.Run(
                challenger.Type,
                opponent.Type,
                gamesCount,
                firstSeed,
                over =>
                {
                    if (over % progressStep == 0)
                    {
                        Console.WriteLine($"  {over}/{gamesCount}");
                    }
                },
                cancellation.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Cancelled.");
            return 2;
        }

        var culture = CultureInfo.InvariantCulture;
        Console.WriteLine(string.Create(culture, $"Mean rank:   {result.RankMean:F3} (+/- {result.RankStandardError:F3})"));
        Console.WriteLine(string.Create(culture, $"Mean points: {result.PointsMean:F0} (+/- {result.PointsStandardError:F0})"));
        Console.WriteLine(string.Create(culture, $"First:       {result.FirstRate:P1}"));
        Console.WriteLine(string.Create(culture, $"Last:        {result.LastRate:P1}"));
        Console.WriteLine(string.Create(culture, $"Time:        {result.Elapsed:hh\\:mm\\:ss}"));
        return 0;
    }

    private static CpuManagerOption? FindCpu(string name)
        => CpuManagerCatalog.Implementations.FirstOrDefault(o => string.Equals(o.DisplayName, name, StringComparison.OrdinalIgnoreCase));

    private static bool TryParse(string[] args, int index, int defaultValue, out int value)
    {
        value = defaultValue;
        return index >= args.Length || int.TryParse(args[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static void ListCpus()
    {
        Console.WriteLine("CPUs: " + string.Join(", ", CpuManagerCatalog.Implementations.Select(o => $"\"{o.DisplayName}\"")));
    }
}
