using System.ComponentModel;
using System.Windows;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for AutoPlayWindow.xaml
/// </summary>
public partial class AutoPlayWindow : Window
{
    // Used as the estimated rounds-per-game before any game in this batch has actually completed.
    private const double DefaultEstimatedRoundsPerGame = 10;

    private GamePivot? _game;
    private int _currentGameIndex;
    private int _totalGamesCount;
    private IReadOnlyList<PlayerPivot>? _permanentCpuPlayers;
    private int _roundsPlayedInCurrentGame;
    private int _roundsPlayedAcrossCompletedGames;

    private readonly RulePivot _ruleset;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="ruleset">Instance of <see cref="RulePivot"/>.</param>
    public AutoPlayWindow(RulePivot ruleset)
    {
        InitializeComponent();

        _ruleset = ruleset;
        _cancellationToken = _cancellationTokenSource.Token;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    // Runs the CPU auto-play off the UI thread, then applies its result back on the UI thread.
    private async void RunAutoPlay(bool newGame)
    {
        if (newGame)
        {
            _game = new GamePivot(_ruleset, _permanentCpuPlayers!, new Random());
            _roundsPlayedInCurrentGame = 0;
        }

        var result = await Task.Run(() => _game!.Round.RunAutoPlay(_cancellationToken));

        if (_cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var endOfRoundInfo = _game!.NextRound(result.RonPlayerId);
        _roundsPlayedInCurrentGame++;

        if (endOfRoundInfo.EndOfGame)
        {
            _game.ComputeCurrentRanking();
            _roundsPlayedAcrossCompletedGames += _roundsPlayedInCurrentGame;

            _currentGameIndex++;
            SetProgress(_currentGameIndex / (double)_totalGamesCount);
            if (_currentGameIndex < _totalGamesCount)
            {
                RunAutoPlay(true);
            }
            else
            {
                ScoresList.ItemsSource = _permanentCpuPlayers;

                WaitingPanel.Visibility = Visibility.Collapsed;
                ActionPanel.Visibility = Visibility.Visible;
                ScoresList.Visibility = Visibility.Visible;
                WindowState = WindowState.Maximized;
            }
        }
        else
        {
            // Estimates progress through the current (still in-progress) game as its round count
            // against the average rounds-per-game observed so far in this batch (a fixed fallback
            // before any game has completed) - much finer-grained than reasoning about wind phases,
            // and naturally handles Enchousen extensions without any special-casing. Capped short of
            // 100% since it's only an estimate: the game isn't actually over yet.
            var averageRoundsPerGame = _currentGameIndex > 0
                ? _roundsPlayedAcrossCompletedGames / (double)_currentGameIndex
                : DefaultEstimatedRoundsPerGame;
            var currentGameProgression = Math.Min(0.95, _roundsPlayedInCurrentGame / averageRoundsPerGame);

            SetProgress((_currentGameIndex / (double)_totalGamesCount) + (currentGameProgression / (double)_totalGamesCount));

            RunAutoPlay(false);
        }
    }

    // The rounds-per-game estimate isn't exact, so a game running longer than average could make it
    // dip relative to the previous update - this keeps the bar from ever visibly going backwards.
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

        _currentGameIndex = 0;
        _roundsPlayedAcrossCompletedGames = 0;
        _permanentCpuPlayers = PlayerPivot.BuildPlayers(null);
        PgbGames.Value = 0;

        WaitingPanel.Visibility = Visibility.Visible;
        ActionPanel.Visibility = Visibility.Collapsed;
        ScoresList.Visibility = Visibility.Collapsed;
        RunAutoPlay(true);
    }
}
