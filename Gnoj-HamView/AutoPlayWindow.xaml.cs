﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private bool _hardStopAutoplay = false;
        private DateTime _timestamp;
        private readonly Dictionary<string, (int count, double sum)> _times = new Dictionary<string, (int, double)>(50);

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

        private void AddTimeEntry(string name)
        {
            var elapsed = (DateTime.Now - _timestamp).TotalMilliseconds;
            var currentEntry = _times.ContainsKey(name)
                ? _times[name]
                : (0, 0);
            _times[name] = (currentEntry.count + 1, currentEntry.sum + elapsed);
            _timestamp = DateTime.Now;
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
                _timestamp = DateTime.Now;
                _autoPlay.RunWorkerAsync();
            }
        }

        // Proceeds to call a kan for an opponent.
        private TilePivot OpponentBeginCallKan(int playerId, TilePivot kanTilePick, bool concealedKan)
        {
            var kanResult = _game.Round.CallKan(playerId, concealedKan ? kanTilePick : null);
            AddTimeEntry(nameof(RoundPivot.CallKan));
            return kanResult;
        }

        // Manages every possible moves for the current opponent after his pick.
        private bool OpponentAfterPick(ref Tuple<int, TilePivot, int?> kanInProgress)
        {
            var tsumoDecision = _game.Round.IaManager.TsumoDecision(kanInProgress != null);
            AddTimeEntry(nameof(IaManagerPivot.TsumoDecision));
            if (tsumoDecision)
            {
                return true;
            }

            var opponentWithKanTilePick = _game.Round.IaManager.KanDecision(true);
            AddTimeEntry(nameof(IaManagerPivot.KanDecision));
            if (opponentWithKanTilePick != null)
            {
                var compensationTile = OpponentBeginCallKan(_game.Round.CurrentPlayerIndex, opponentWithKanTilePick.Item2, true);
                kanInProgress = new Tuple<int, TilePivot, int?>(_game.Round.CurrentPlayerIndex, compensationTile, null);
                return false;
            }

            kanInProgress = null;

            var riichiTile = _game.Round.IaManager.RiichiDecision();
            AddTimeEntry(nameof(IaManagerPivot.RiichiDecision));
            if (riichiTile != null)
            {
                _game.Round.CallRiichi(riichiTile);
                AddTimeEntry(nameof(RoundPivot.CallRiichi));
                return false;
            }

            var discardDecision = _game.Round.IaManager.DiscardDecision();
            AddTimeEntry(nameof(IaManagerPivot.DiscardDecision));
            _game.Round.Discard(discardDecision);
            AddTimeEntry(nameof(RoundPivot.Discard));
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
                    var ronDecision = _game.Round.IaManager.RonDecision(false);
                    AddTimeEntry(nameof(IaManagerPivot.RonDecision));
                    if (ronDecision.Count > 0)
                    {
                        ronPlayerId = kanInProgress != null ? kanInProgress.Item1 : _game.Round.PreviousPlayerIndex;
                        if (kanInProgress != null)
                        {
                            _game.Round.UndoPickCompensationTile();
                            AddTimeEntry(nameof(RoundPivot.UndoPickCompensationTile));
                        }
                        break;
                    }

                    var opponentWithKanTilePick = _game.Round.IaManager.KanDecision(false);
                    AddTimeEntry(nameof(IaManagerPivot.KanDecision));
                    if (opponentWithKanTilePick != null)
                    {
                        var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
                        var compensationTile = OpponentBeginCallKan(opponentWithKanTilePick.Item1, opponentWithKanTilePick.Item2, false);
                        kanInProgress = new Tuple<int, TilePivot, int?>(opponentWithKanTilePick.Item1, compensationTile, previousPlayerIndex);
                        continue;
                    }

                    var opponentPlayerId = _game.Round.IaManager.PonDecision();
                    AddTimeEntry(nameof(IaManagerPivot.PonDecision));
                    if (opponentPlayerId > -1)
                    {
                        var canCallPon = _game.Round.CallPon(opponentPlayerId);
                        AddTimeEntry(nameof(RoundPivot.CallPon));
                        if (canCallPon)
                        {
                            var discardDecision = _game.Round.IaManager.DiscardDecision();
                            AddTimeEntry(nameof(IaManagerPivot.DiscardDecision));
                            _game.Round.Discard(discardDecision);
                            AddTimeEntry(nameof(RoundPivot.Discard));
                        }
                        continue;
                    }

                    var chiiTilePick = _game.Round.IaManager.ChiiDecision();
                    AddTimeEntry(nameof(IaManagerPivot.ChiiDecision));
                    if (chiiTilePick != null)
                    {
                        var callChii = _game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number);
                        AddTimeEntry(nameof(RoundPivot.CallChii));
                        if (callChii)
                        {
                            var discardDecision = _game.Round.IaManager.DiscardDecision();
                            AddTimeEntry(nameof(IaManagerPivot.DiscardDecision));
                            _game.Round.Discard(discardDecision);
                            AddTimeEntry(nameof(RoundPivot.Discard));
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
                    AddTimeEntry(nameof(RoundPivot.Pick));
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

                    //new ScoreWindow(_game.Players.ToList(), endOfRoundInfo).ShowDialog();

                    if (endOfRoundInfo.EndOfGame)
                    {
                        foreach (var k in _times.Keys)
                        {
                            var avg = Math.Round(_times[k].sum / _times[k].count);
                            var sum = Math.Ceiling(_times[k].sum / 1000);
                            System.Diagnostics.Debug.WriteLine($"{k} - avg {avg} ms ; total {sum} sec ; count {_times[k].count}");
                        }

                        _game = new GamePivot(null, _game.Ruleset, null);
                        //new EndOfGameWindow(_game).ShowDialog();
                        //Close();
                        RunAutoPlay();
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
