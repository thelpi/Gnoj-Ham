using System;
using System.Collections.Generic;
using System.Threading;
using Gnoj_Ham.AutoPlayEvents;

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
                    var compensationTile = OpponentBeginCallKanCpuOnly(opponentWithKanTilePick.Item1, opponentWithKanTilePick.Item2, false);
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
                    if (OpponentAfterPickOnly(ref kanInProgress))
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
                if (OpponentAfterPickOnly(ref kanInProgress))
                {
                    break;
                }
            }

            return ronPlayerId;
        }

        // Proceeds to call a kan for an opponent.
        private TilePivot OpponentBeginCallKanCpuOnly(int playerId, TilePivot kanTilePick, bool concealedKan)
        {
            var kanResult = _game.Round.CallKan(playerId, concealedKan ? kanTilePick : null);
            _addTimeEntry(nameof(RoundPivot.CallKan));
            return kanResult;
        }

        // Manages every possible moves for the current opponent after his pick.
        private bool OpponentAfterPickOnly(ref Tuple<int, TilePivot, int?> kanInProgress)
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
                var compensationTile = OpponentBeginCallKanCpuOnly(_game.Round.CurrentPlayerIndex, opponentWithKanTilePick.Item2, true);
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

        #region Events

        public event HumanCanCallRonEventHandler HumanCanCallRon;
        public event HighlightPreviousPlayerDiscardEventHandler HighlightPreviousPlayerDiscard;
        public event InvokeOverlayEventHandler InvokeOverlay;
        public event CommonCallKanEventHandler CommonCallKan;
        public event RefreshPlayerTurnStyleEventHandler RefreshPlayerTurnStyle;
        public event AfterPickEventHandler AfterPick;
        public event AfterChiiEventHandler AfterChii;
        public event AfterPonEventHandler AfterPon;
        public event AfterDiscardEventHandler AfterDiscard;
        public event AfterRiichiEventHandler AfterRiichi;
        public event HumanCallTsumoEventHandler HumanCallTsumo;
        public event HumanCallRiichiEventHandler HumanCallRiichi;
        public event HumanDoesNotCallEventHandler HumanDoesNotCall;
        public event NotifyRiichiTilesEventHandler NotifyRiichiTiles;

        #endregion Events

        public (bool endOfRound, int? ronPlayerId, HumanActionPivot? humanAction) AutoPlayHuman(
            CancellationToken cancellationToken,
            bool skipCurrentAction,
            bool humanRonPending,
            bool autoCallMahjong,
            int sleepTime)
        {
            Tuple<int, TilePivot, int?> kanInProgress = null;
            (bool endOfRound, int? ronPlayerId, HumanActionPivot? humanAction) result = default;
            var isFirstTurn = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!isFirstTurn)
                    skipCurrentAction = false;
                isFirstTurn = false;

                if (!skipCurrentAction && !humanRonPending && _game.Round.CanCallRon(GamePivot.HUMAN_INDEX))
                {
                    HumanCanCallRon?.Invoke(new HumanCanCallRonEventArgs());
                    if (autoCallMahjong)
                    {
                        result = (result.endOfRound, result.ronPlayerId, HumanActionPivot.Ron);
                    }
                    else
                    {
                        HighlightPreviousPlayerDiscard?.Invoke(new HighlightPreviousPlayerDiscardEventArgs());
                    }
                    break;
                }

                if (CheckOpponensRonCall(humanRonPending))
                {
                    result = (true, kanInProgress != null ? kanInProgress.Item1 : _game.Round.PreviousPlayerIndex, result.humanAction);
                    if (kanInProgress != null)
                    {
                        _game.Round.UndoPickCompensationTile();
                    }
                    break;
                }

                if (kanInProgress != null)
                {
                    CommonCallKan?.Invoke(new CommonCallKanEventArgs(kanInProgress.Item3));
                }

                if (!skipCurrentAction && _game.Round.CanCallPonOrKan(GamePivot.HUMAN_INDEX, out var isSelfKan))
                {
                    if (!isSelfKan)
                    {
                        HighlightPreviousPlayerDiscard?.Invoke(new HighlightPreviousPlayerDiscardEventArgs());
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
                    PonCall(opponentPlayerId, sleepTime);
                    continue;
                }

                if (!skipCurrentAction && _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Count > 0)
                {
                    HighlightPreviousPlayerDiscard?.Invoke(new HighlightPreviousPlayerDiscardEventArgs());
                    break;
                }

                var chiiTilePick = _game.Round.IaManager.ChiiDecision();
                if (chiiTilePick != null)
                {
                    ChiiCall(chiiTilePick, sleepTime);
                    continue;
                }

                if (kanInProgress != null)
                {
                    if (OpponentAfterPick(ref kanInProgress, sleepTime))
                    {
                        result = (true, result.ronPlayerId, result.humanAction);
                        break;
                    }
                    continue;
                }

                if (_game.Round.IsWallExhaustion)
                {
                    result = (true, result.ronPlayerId, result.humanAction);
                    break;
                }

                if (_game.Round.IsHumanPlayer)
                {
                    result = (result.endOfRound, result.ronPlayerId, HumanAutoPlay(autoCallMahjong, sleepTime));
                    break;
                }
                else
                {
                    Pick();
                    if (OpponentAfterPick(ref kanInProgress, sleepTime))
                    {
                        result = (true, result.ronPlayerId, result.humanAction);
                        break;
                    }
                }
            }

            return result;
        }

        private bool CheckOpponensRonCall(bool humanRonPending)
        {
            var opponentsCallRon = _game.Round.IaManager.RonDecision(humanRonPending);
            foreach (var opponentPlayerIndex in opponentsCallRon)
            {
                InvokeOverlay?.Invoke(new InvokeOverlayEventArgs(opponentPlayerIndex, HumanActionPivot.Ron));
            }

            return humanRonPending || opponentsCallRon.Count > 0;
        }

        private TilePivot OpponentBeginCallKan(int playerId, TilePivot kanTilePick, bool concealedKan)
        {
            RefreshPlayerTurnStyle?.Invoke(new RefreshPlayerTurnStyleEventArgs());

            var compensationTile = _game.Round.CallKan(playerId, concealedKan ? kanTilePick : null);
            if (compensationTile != null)
            {
                InvokeOverlay?.Invoke(new InvokeOverlayEventArgs(playerId, HumanActionPivot.Kan));
            }
            return compensationTile;
        }

        private void Pick()
        {
            RefreshPlayerTurnStyle?.Invoke(new RefreshPlayerTurnStyleEventArgs());

            var pick = _game.Round.Pick();

            AfterPick?.Invoke(new AfterPickEventArgs());
        }

        private void ChiiCall(Tuple<TilePivot, bool> chiiTilePick, int sleepTime)
        {
            RefreshPlayerTurnStyle?.Invoke(new RefreshPlayerTurnStyleEventArgs());

            if (_game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number))
            {
                InvokeOverlay?.Invoke(new InvokeOverlayEventArgs(_game.Round.CurrentPlayerIndex, HumanActionPivot.Chii));

                AfterChii?.Invoke(new AfterChiiEventArgs());

                if (!_game.Round.IsHumanPlayer)
                {
                    Discard(_game.Round.IaManager.DiscardDecision(new List<TilePivot>()), sleepTime);
                }
            }
        }

        private void PonCall(int playerIndex, int sleepTime)
        {
            RefreshPlayerTurnStyle?.Invoke(new RefreshPlayerTurnStyleEventArgs());

            // Note : this value is stored here because the call to "CallPon" makes it change.
            var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
            var isCpu = playerIndex != GamePivot.HUMAN_INDEX;

            if (_game.Round.CallPon(playerIndex))
            {
                InvokeOverlay?.Invoke(new InvokeOverlayEventArgs(playerIndex, HumanActionPivot.Pon));

                AfterPon?.Invoke(new AfterPonEventArgs(playerIndex, previousPlayerIndex));

                if (isCpu)
                {
                    Discard(_game.Round.IaManager.DiscardDecision(new List<TilePivot>()), sleepTime);
                }
            }
        }

        private void Discard(TilePivot tile, int sleepTime)
        {
            if (!_game.Round.IsHumanPlayer)
            {
                Thread.Sleep(sleepTime);
            }

            if (_game.Round.Discard(tile))
            {
                AfterDiscard?.Invoke(new AfterDiscardEventArgs());
            }
        }

        private bool OpponentAfterPick(ref Tuple<int, TilePivot, int?> kanInProgress, int sleepTime)
        {
            if (_game.Round.IaManager.TsumoDecision(kanInProgress != null))
            {
                InvokeOverlay?.Invoke(new InvokeOverlayEventArgs(_game.Round.CurrentPlayerIndex, HumanActionPivot.Tsumo));
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

            var (riichiTile, riichiTiles) = _game.Round.IaManager.RiichiDecision();
            if (riichiTile != null)
            {
                CallRiichi(riichiTile, sleepTime);
                return false;
            }

            Discard(_game.Round.IaManager.DiscardDecision(riichiTiles), sleepTime);
            return false;
        }

        private void CallRiichi(TilePivot tile, int sleepTime)
        {
            if (!_game.Round.IsHumanPlayer)
            {
                InvokeOverlay?.Invoke(new InvokeOverlayEventArgs(_game.Round.CurrentPlayerIndex, HumanActionPivot.Riichi));
                Thread.Sleep(sleepTime);
            }

            if (_game.Round.CallRiichi(tile))
            {
                AfterRiichi?.Invoke(new AfterRiichiEventArgs());
            }
        }

        private HumanActionPivot? HumanAutoPlay(bool autoCallMahjong, int sleepTime)
        {
            Pick();

            if (_game.Round.CanCallTsumo(false))
            {
                HumanCallTsumo?.Invoke(new HumanCallTsumoEventArgs());
                return autoCallMahjong ? HumanActionPivot.Tsumo : default(HumanActionPivot?);
            }

            var riichiTiles = _game.Round.CanCallRiichi();
            NotifyRiichiTiles?.Invoke(new NotifyRiichiTilesEventArgs(riichiTiles));
            if (riichiTiles.Count > 0)
            {
                var riichiDecision = _game.Ruleset.DiscardTip && _game.Round.IaManager.RiichiDecision().choice != null;
                HumanCallRiichi?.Invoke(new HumanCallRiichiEventArgs(riichiDecision));
                return null;
            }
            else if (_game.Round.HumanCanAutoDiscard())
            {
                // Not a real CPU sleep: the auto-discard by human player is considered as such
                Thread.Sleep(sleepTime);
                return HumanActionPivot.Discard;
            }
            else
            {
                HumanDoesNotCall?.Invoke(new HumanDoesNotCallEventArgs());
            }

            return null;
        }
    }
}
