using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for AutoPlayWindow.xaml
/// </summary>
public partial class AutoPlayWindow : Window
{
    private int _totalGamesCount;
    private IReadOnlyList<PlayerPivot>? _permanentCpuPlayers;
    private IReadOnlyDictionary<PlayerIndices, Func<RoundPivot, CpuManagerBasePivot>>? _cpuManagerFactories;

    private readonly RulePivot _ruleset;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;
    private readonly ComboBox[] _cpuPickers;
    private readonly object _statsLock = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="ruleset">Instance of <see cref="RulePivot"/>.</param>
    public AutoPlayWindow(RulePivot ruleset)
    {
        InitializeComponent();

        _ruleset = ruleset;
        _cancellationToken = _cancellationTokenSource.Token;

        _cpuPickers = new[] { CmbCpu0, CmbCpu1, CmbCpu2, CmbCpu3 };
        foreach (var picker in _cpuPickers)
        {
            picker.ItemsSource = CpuManagerCatalog.Implementations;
            picker.DisplayMemberPath = "DisplayName";
            picker.SelectedIndex = 0;
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    // Plays every game of the batch to completion, several at a time (bounded by the machine's core
    // count) off the UI thread. Each game runs on its own throwaway PlayerPivot set - sharing
    // _permanentCpuPlayers directly across concurrently-running games would mean several games writing
    // CurrentGamePoints at once - then its final ranking is committed onto _permanentCpuPlayers (the
    // ones actually displayed and accumulating stats across the whole batch) under a lock, since
    // several games can finish at nearly the same moment.
    private async void RunBatch()
    {
        var completedGamesCount = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = _cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(Enumerable.Range(0, _totalGamesCount), options, (_, cancellationToken) =>
            {
                var gamePlayers = PlayerPivot.BuildPlayers(null);
                var game = new GamePivot(_ruleset, gamePlayers, new Random(), _cpuManagerFactories);

                EndOfRoundInformationsPivot endOfRoundInfo;
                do
                {
                    var result = game.Round.RunAutoPlay(cancellationToken);
                    endOfRoundInfo = game.NextRound(result.RonPlayerId);
                } while (!endOfRoundInfo.EndOfGame);

                lock (_statsLock)
                {
                    game.ComputeCurrentRanking(_permanentCpuPlayers);
                }

                var done = Interlocked.Increment(ref completedGamesCount);
                Dispatcher.Invoke(() => SetProgress(done / (double)_totalGamesCount));

                return ValueTask.CompletedTask;
            });
        }
        catch (OperationCanceledException)
        {
            // The window is closing: no UI left to update.
            return;
        }

        ScoresList.ItemsSource = _permanentCpuPlayers;

        foreach (var picker in _cpuPickers)
        {
            picker.IsEnabled = true;
        }

        WaitingPanel.Visibility = Visibility.Collapsed;
        ActionPanel.Visibility = Visibility.Visible;
        ScoresList.Visibility = Visibility.Visible;
        WindowState = WindowState.Maximized;
    }

    // Games complete out of order once run in parallel, so this only ever moves forward in whole
    // "one game done" steps rather than interpolating progress within any single still-running game.
    private void SetProgress(double value)
    {
        PgbGames.Value = Math.Max(PgbGames.Value, value);
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtGamesCount.Text, out _totalGamesCount))
        {
            MessageBox.Show("Invalid number of games!", "Gnoj-Ham - Error");
            return;
        }

        var factories = new Dictionary<PlayerIndices, Func<RoundPivot, CpuManagerBasePivot>>();
        var nameSuffixes = new Dictionary<PlayerIndices, string>();
        for (var i = 0; i < _cpuPickers.Length; i++)
        {
            var playerIndex = (PlayerIndices)i;
            var option = (CpuManagerOption)_cpuPickers[i].SelectedItem;
            factories[playerIndex] = round => (CpuManagerBasePivot)Activator.CreateInstance(option.Type, round)!;
            nameSuffixes[playerIndex] = option.DisplayName;
        }
        _cpuManagerFactories = factories;
        _permanentCpuPlayers = PlayerPivot.BuildPlayers(null, nameSuffixes);

        PgbGames.Value = 0;

        foreach (var picker in _cpuPickers)
        {
            picker.IsEnabled = false;
        }

        WaitingPanel.Visibility = Visibility.Visible;
        ActionPanel.Visibility = Visibility.Collapsed;
        ScoresList.Visibility = Visibility.Collapsed;
        RunBatch();
    }
}
