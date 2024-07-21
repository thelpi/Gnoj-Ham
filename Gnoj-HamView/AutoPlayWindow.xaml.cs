using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Interaction logic for AutoPlayWindow.xaml
    /// </summary>
    public partial class AutoPlayWindow : Window
    {
        private readonly GamePivot _game;
        private BackgroundWorker _autoPlay;
        private bool _hardStopAutoplay = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ruleset">Instance of <see cref="RulePivot"/>.</param>
        /// <exception cref="ArgumentException">Ruleset is not for four CPUs.</exception>
        public AutoPlayWindow(RulePivot ruleset)
        {
            InitializeComponent();

            if (!ruleset.FourCpus)
            {
                throw new ArgumentException($"{nameof(ruleset.FourCpus)} should be enabled.");
            }

            _game = new GamePivot(null, ruleset, null);

            InitializeAutoPlayWorker();

            ContentRendered += delegate (object sender, EventArgs evt)
            {
                RunAutoPlay();
            };
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            _hardStopAutoplay = true;
        }

        // Starts the background worker.
        private void RunAutoPlay()
        {
            if (!_autoPlay.IsBusy)
            {
                _autoPlay.RunWorkerAsync();
            }
        }

        // Proceeds to call a kan for an opponent.
        private TilePivot OpponentBeginCallKan(int playerId, TilePivot kanTilePick, bool concealedKan)
        {
            return _game.Round.CallKan(playerId, concealedKan ? kanTilePick : null);
        }

        // Manages every possible moves for the current opponent after his pick.
        private bool OpponentAfterPick(ref Tuple<int, TilePivot, int?> kanInProgress)
        {
            if (_game.Round.IaManager.TsumoDecision(kanInProgress != null))
            {
                return true;
            }

            var opponentWithKanTilePick = _game.Round.IaManager.KanDecision(true);
            if (opponentWithKanTilePick != null)
            {
                var compensationTile = OpponentBeginCallKan(_game.Round.CurrentPlayerIndex, opponentWithKanTilePick.Item2, true);
                kanInProgress = new Tuple<int, TilePivot, int?>(_game.Round.CurrentPlayerIndex, compensationTile, null);
                return false;
            }

            kanInProgress = null;

            var riichiTile = _game.Round.IaManager.RiichiDecision();
            if (riichiTile != null)
            {
                _game.Round.CallRiichi(riichiTile);
                return false;
            }

            _game.Round.Discard(_game.Round.IaManager.DiscardDecision());
            return false;
        }

        // Initializes a background worker which orchestrates the CPU actions.
        private void InitializeAutoPlayWorker()
        {
            _autoPlay = new BackgroundWorker
            {
                WorkerReportsProgress = false,
                WorkerSupportsCancellation = false
            };
            _autoPlay.DoWork += delegate (object sender, DoWorkEventArgs evt)
            {
                var argumentsList = evt.Argument as object[];
                Tuple<int, TilePivot, int?> kanInProgress = null;
                int? ronPlayerId = null;
                while (!_hardStopAutoplay)
                {
                    if (_game.Round.IaManager.RonDecision(false).Count > 0)
                    {
                        ronPlayerId = kanInProgress != null ? kanInProgress.Item1 : _game.Round.PreviousPlayerIndex;
                        if (kanInProgress != null)
                        {
                            _game.Round.UndoPickCompensationTile();
                        }
                        break;
                    }

                    var opponentWithKanTilePick = _game.Round.IaManager.KanDecision(false);
                    if (opponentWithKanTilePick != null)
                    {
                        var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
                        var compensationTile = OpponentBeginCallKan(opponentWithKanTilePick.Item1, opponentWithKanTilePick.Item2, false);
                        kanInProgress = new Tuple<int, TilePivot, int?>(opponentWithKanTilePick.Item1, compensationTile, previousPlayerIndex);
                        continue;
                    }

                    var opponentPlayerId = _game.Round.IaManager.PonDecision();
                    if (opponentPlayerId > -1)
                    {
                        if (_game.Round.CallPon(opponentPlayerId))
                        {
                            _game.Round.Discard(_game.Round.IaManager.DiscardDecision());
                        }
                        continue;
                    }

                    var chiiTilePick = _game.Round.IaManager.ChiiDecision();
                    if (chiiTilePick != null)
                    {
                        if (_game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number))
                        {
                            _game.Round.Discard(_game.Round.IaManager.DiscardDecision());
                        }
                        continue;
                    }

                    if (kanInProgress != null)
                    {
                        if (OpponentAfterPick(ref kanInProgress))
                        {
                            break;
                        }
                        continue;
                    }

                    if (_game.Round.IsWallExhaustion)
                    {
                        break;
                    }

                    _game.Round.Pick();
                    if (OpponentAfterPick(ref kanInProgress))
                    {
                        break;
                    }
                }

                evt.Result = ronPlayerId;
            };
            _autoPlay.RunWorkerCompleted += delegate (object sender, RunWorkerCompletedEventArgs evt)
            {
                if (!_hardStopAutoplay)
                {
                    var (endOfRoundInfo, _) = _game.NextRound((int?)evt.Result);

                    new ScoreWindow(_game.Players.ToList(), endOfRoundInfo).ShowDialog();

                    if (endOfRoundInfo.EndOfGame)
                    {
                        new EndOfGameWindow(_game).ShowDialog();
                        Close();
                    }
                    else
                    {
                        RunAutoPlay();
                    }
                }
            };
        }
    }
}
