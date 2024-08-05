using System.ComponentModel;
using System.Text;
using System.Windows;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for AutoPlayWindow.xaml
/// </summary>
public partial class AutoPlayWindow : Window
{
    private GamePivot? _game;
    private readonly BackgroundWorker _autoPlay;
    private int _currentGameIndex;
    private int _totalGamesCount;
    private IReadOnlyList<PermanentPlayerPivot>? _permanentPlayers;

    private readonly Dictionary<string, (int count, double sum)> _times = new(50);
    private readonly RulePivot _ruleset;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly CancellationToken _cancellationToken;

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="ruleset">Instance of <see cref="RulePivot"/>.</param>
    /// <param name="enableBenchmark">Enable benchmark.</param>
    public AutoPlayWindow(RulePivot ruleset)
    {
        InitializeComponent();

        _autoPlay = new BackgroundWorker
        {
            WorkerReportsProgress = true,
            WorkerSupportsCancellation = false
        };
        InitializeAutoPlayWorker();

        _ruleset = ruleset;
        _cancellationToken = _cancellationTokenSource.Token;
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _cancellationTokenSource.Cancel();
    }

    // Starts the background worker.
    private void RunAutoPlay(bool newGame)
    {
        if (newGame)
        {
            _game = new GamePivot(_ruleset, _permanentPlayers!, new Random());
        }
        _autoPlay.RunWorkerAsync();
    }

    // Initializes a background worker which orchestrates the CPU actions.
    private void InitializeAutoPlayWorker()
    {
        _autoPlay.DoWork += delegate (object? sender, DoWorkEventArgs evt)
        {
            var (_, ronPlayerId, _) = _game!.Round.RunAutoPlay(_cancellationToken);
            evt.Result = ronPlayerId;
        };
        _autoPlay.RunWorkerCompleted += delegate (object? sender, RunWorkerCompletedEventArgs evt)
        {
            if (!_cancellationToken.IsCancellationRequested)
            {
                var (endOfRoundInfo, _) = _game!.NextRound((PlayerIndices?)evt.Result);

                if (endOfRoundInfo.EndOfGame)
                {
                    _game.ComputeCurrentRanking();

                    _currentGameIndex++;
                    PgbGames.Value = _currentGameIndex / (double)_totalGamesCount;
                    if (_currentGameIndex < _totalGamesCount)
                    {
                        RunAutoPlay(true);
                    }
                    else
                    {
                        ScoresList.ItemsSource = _permanentPlayers;

                        WaitingPanel.Visibility = Visibility.Collapsed;
                        ActionPanel.Visibility = Visibility.Visible;
                        ScoresList.Visibility = Visibility.Visible;
                        WindowState = WindowState.Maximized;
                    }
                }
                else
                {
                    // if we are in south (or post-south) : +50%
                    var currentGameProgression = _game.DominantWind == Winds.East ? 0 : 0.5;

                    // +12.5% for each "East" turn (it's not really accurate as a player can keep "East" several turns)
                    currentGameProgression += (_game.EastRank - 1) * 0.125;

                    // adds to th current value (based on number of games)
                    PgbGames.Value = (_currentGameIndex / (double)_totalGamesCount) + (currentGameProgression * (1 / (double)_totalGamesCount));

                    RunAutoPlay(false);
                }
            }
        };
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtGamesCount.Text, out _totalGamesCount))
        {
            MessageBox.Show("Invalid number of games!", "Gnoj-Ham - Error");
            return;
        }

        _currentGameIndex = 0;
        _times.Clear();
        _permanentPlayers = new List<PermanentPlayerPivot>
        {
            new PermanentPlayerPivot(),
            new PermanentPlayerPivot(),
            new PermanentPlayerPivot(),
            new PermanentPlayerPivot()
        };

        WaitingPanel.Visibility = Visibility.Visible;
        ActionPanel.Visibility = Visibility.Collapsed;
        ScoresList.Visibility = Visibility.Collapsed;
        RunAutoPlay(true);
    }
}
