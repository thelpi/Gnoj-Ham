using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Interaction logic for AutoPlayWindow.xaml
    /// </summary>
    public partial class AutoPlayWindow : Window
    {
        private GamePivot _game;
        private BackgroundWorker _autoPlay;
        private DateTime _timestamp;
        private int _currentGameIndex;
        private int _totalGamesCount;
        private List<PermanentPlayerPivot> _permanentPlayers;

        private readonly Dictionary<string, (int count, double sum)> _times = new Dictionary<string, (int, double)>(50);
        private readonly RulePivot _ruleset;
        private readonly bool _enableBenchmark;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ruleset">Instance of <see cref="RulePivot"/>.</param>
        /// <param name="enableBenchmark">Enable benchmark.</param>
        public AutoPlayWindow(RulePivot ruleset, bool enableBenchmark)
        {
            InitializeComponent();

            InitializeAutoPlayWorker();

            _ruleset = ruleset;
            _enableBenchmark = enableBenchmark;
            _cancellationToken = _cancellationTokenSource.Token;
        }

        private void AddTimeEntry(string name)
        {
            if (_enableBenchmark)
            {
                var elapsed = (DateTime.Now - _timestamp).TotalMilliseconds;
                var currentEntry = _times.ContainsKey(name)
                    ? _times[name]
                    : (0, 0);
                _times[name] = (currentEntry.count + 1, currentEntry.sum + elapsed);
                _timestamp = DateTime.Now;
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _cancellationTokenSource.Cancel();
        }

        // Starts the background worker.
        private void RunAutoPlay(bool newGame)
        {
            _timestamp = DateTime.Now;
            if (newGame)
            {
                _game = new GamePivot(_ruleset, _permanentPlayers);
            }
            _autoPlay.RunWorkerAsync();
        }

        // Initializes a background worker which orchestrates the CPU actions.
        private void InitializeAutoPlayWorker()
        {
            _autoPlay = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = false
            };
            _autoPlay.DoWork += delegate (object sender, DoWorkEventArgs evt)
            {
                evt.Result = new AutoPlayPivot(_game, AddTimeEntry).AutoPlay(_cancellationToken);
            };
            _autoPlay.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs evt)
            {
                if (!_cancellationToken.IsCancellationRequested)
                {
                    var (endOfRoundInfo, _) = _game.NextRound((int?)evt.Result);

                    if (endOfRoundInfo.EndOfGame)
                    {
                        ScoreTools.ComputeCurrentRanking(_game);

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
                            if (_enableBenchmark)
                            {
                                TxtResultsRaw.Visibility = Visibility.Visible;

                                var sb = new StringBuilder();
                                sb.AppendLine("Action\tCount\tSum (s)\tAverage (ms)");
                                foreach (var r in _times.OrderByDescending(t => t.Value.sum).Select(t => t.Key))
                                {
                                    sb.AppendLine($"{r}\t{_times[r].count}\t{Math.Floor(_times[r].sum / 1000)}\t{Math.Floor(_times[r].sum / _times[r].count)}");
                                }
                                TxtResultsRaw.AppendText(sb.ToString());
                            }
                            WindowState = WindowState.Maximized;
                        }
                    }
                    else
                    {
                        // if we are in south (or post-south) : +50%
                        var currentGameProgression = _game.DominantWind == WindPivot.East ? 0 : 0.5;

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
}
