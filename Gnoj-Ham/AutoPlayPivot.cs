using System;
using System.Collections.Generic;
using System.Threading;

namespace Gnoj_Ham
{
    /// <summary>
    /// 
    /// </summary>
    public class AutoPlayPivot
    {
        private readonly GamePivot _game;
        private readonly Action<string> _addTimeEntry;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <param name="addTimeEntry"></param>
        public AutoPlayPivot(GamePivot game, Action<string> addTimeEntry)
        {
            _game = game;
            _addTimeEntry = addTimeEntry;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public int? AutoPlay(CancellationToken cancellationToken)
        {
            Tuple<int, TilePivot, int?> kanInProgress = null;
            int? ronPlayerId = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                var ronDecision = _game.Round.IaManager.RonDecision(false);
                _addTimeEntry(nameof(IaManagerPivot.RonDecision));
                if (ronDecision.Count > 0)
                {
                    ronPlayerId = kanInProgress != null ? kanInProgress.Item1 : _game.Round.PreviousPlayerIndex;
                    if (kanInProgress != null)
                    {
                        _game.Round.UndoPickCompensationTile();
                        _addTimeEntry(nameof(RoundPivot.UndoPickCompensationTile));
                    }
                    break;
                }

                var opponentWithKanTilePick = _game.Round.IaManager.KanDecision(false);
                _addTimeEntry(nameof(IaManagerPivot.KanDecision));
                if (opponentWithKanTilePick != null)
                {
                    var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
                    var compensationTile = OpponentBeginCallKan(opponentWithKanTilePick.Item1, opponentWithKanTilePick.Item2, false);
                    kanInProgress = new Tuple<int, TilePivot, int?>(opponentWithKanTilePick.Item1, compensationTile, previousPlayerIndex);
                    continue;
                }

                var opponentPlayerId = _game.Round.IaManager.PonDecision();
                _addTimeEntry(nameof(IaManagerPivot.PonDecision));
                if (opponentPlayerId > -1)
                {
                    var canCallPon = _game.Round.CallPon(opponentPlayerId);
                    _addTimeEntry(nameof(RoundPivot.CallPon));
                    if (canCallPon)
                    {
                        var discardDecision = _game.Round.IaManager.DiscardDecision(new List<TilePivot>());
                        _addTimeEntry(nameof(IaManagerPivot.DiscardDecision));
                        _game.Round.Discard(discardDecision);
                        _addTimeEntry(nameof(RoundPivot.Discard));
                    }
                    continue;
                }

                var chiiTilePick = _game.Round.IaManager.ChiiDecision();
                _addTimeEntry(nameof(IaManagerPivot.ChiiDecision));
                if (chiiTilePick != null)
                {
                    var callChii = _game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number);
                    _addTimeEntry(nameof(RoundPivot.CallChii));
                    if (callChii)
                    {
                        var discardDecision = _game.Round.IaManager.DiscardDecision(new List<TilePivot>());
                        _addTimeEntry(nameof(IaManagerPivot.DiscardDecision));
                        _game.Round.Discard(discardDecision);
                        _addTimeEntry(nameof(RoundPivot.Discard));
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
                _addTimeEntry(nameof(RoundPivot.Pick));
                if (OpponentAfterPick(ref kanInProgress))
                {
                    break;
                }
            }

            return ronPlayerId;
        }

        // Proceeds to call a kan for an opponent.
        private TilePivot OpponentBeginCallKan(int playerId, TilePivot kanTilePick, bool concealedKan)
        {
            var kanResult = _game.Round.CallKan(playerId, concealedKan ? kanTilePick : null);
            _addTimeEntry(nameof(RoundPivot.CallKan));
            return kanResult;
        }

        // Manages every possible moves for the current opponent after his pick.
        private bool OpponentAfterPick(ref Tuple<int, TilePivot, int?> kanInProgress)
        {
            var tsumoDecision = _game.Round.IaManager.TsumoDecision(kanInProgress != null);
            _addTimeEntry(nameof(IaManagerPivot.TsumoDecision));
            if (tsumoDecision)
            {
                return true;
            }

            var opponentWithKanTilePick = _game.Round.IaManager.KanDecision(true);
            _addTimeEntry(nameof(IaManagerPivot.KanDecision));
            if (opponentWithKanTilePick != null)
            {
                var compensationTile = OpponentBeginCallKan(_game.Round.CurrentPlayerIndex, opponentWithKanTilePick.Item2, true);
                kanInProgress = new Tuple<int, TilePivot, int?>(_game.Round.CurrentPlayerIndex, compensationTile, null);
                return false;
            }

            kanInProgress = null;

            var (riichiTile, riichiTiles) = _game.Round.IaManager.RiichiDecision();
            _addTimeEntry(nameof(IaManagerPivot.RiichiDecision));
            if (riichiTile != null)
            {
                _game.Round.CallRiichi(riichiTile);
                _addTimeEntry(nameof(RoundPivot.CallRiichi));
                return false;
            }

            var discardDecision = _game.Round.IaManager.DiscardDecision(riichiTiles);
            _addTimeEntry(nameof(IaManagerPivot.DiscardDecision));
            _game.Round.Discard(discardDecision);
            _addTimeEntry(nameof(RoundPivot.Discard));
            return false;
        }
    }
}
