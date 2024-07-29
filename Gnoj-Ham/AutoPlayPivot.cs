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

        #region Events

        /// <summary>
        /// Event to notify <see cref="HumanCallNotifierEventArgs"/>.
        /// </summary>
        public event Action<HumanCallNotifierEventArgs> HumanCallNotifier;

        /// <summary>
        /// Event to notify <see cref="DiscardTileNotifierEventArgs"/>.
        /// </summary>
        public event Action<DiscardTileNotifierEventArgs> DiscardTileNotifier;

        /// <summary>
        /// Event to notify <see cref="CallNotifierEventArgs"/>.
        /// </summary>
        public event Action<CallNotifierEventArgs> CallNotifier;

        /// <summary>
        /// Event to notify <see cref="ReadyToCallNotifierEventArgs"/>.
        /// </summary>
        public event Action<ReadyToCallNotifierEventArgs> ReadyToCallNotifier;

        /// <summary>
        /// Event to notify <see cref="TurnChangeNotifierEventArgs"/>.
        /// </summary>
        public event Action<TurnChangeNotifierEventArgs> TurnChangeNotifier;

        /// <summary>
        /// Event to notify <see cref="PickNotifierEventArgs"/>.
        /// </summary>
        public event Action<PickNotifierEventArgs> PickNotifier;

        /// <summary>
        /// Event to notify <see cref="RiichiChoicesNotifierEventArgs"/>.
        /// </summary>
        public event Action<RiichiChoicesNotifierEventArgs> RiichiChoicesNotifier;

        #endregion Events

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="skipCurrentAction"></param>
        /// <param name="humanRonPending"></param>
        /// <param name="autoCallMahjong"></param>
        /// <param name="sleepTime"></param>
        /// <returns></returns>
        public (bool endOfRound, int? ronPlayerId, CallTypePivot? humanAction) AutoPlayHuman(
            CancellationToken cancellationToken,
            bool skipCurrentAction,
            bool humanRonPending,
            bool autoCallMahjong,
            int sleepTime)
        {
            Tuple<int, TilePivot, int?> kanInProgress = null;
            (bool endOfRound, int? ronPlayerId, CallTypePivot? humanAction) result = default;
            var isFirstTurn = true;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!isFirstTurn)
                    skipCurrentAction = false;
                isFirstTurn = false;

                if (!skipCurrentAction && !humanRonPending && _game.Round.CanCallRon(GamePivot.HUMAN_INDEX))
                {
                    HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypePivot.Ron });
                    if (autoCallMahjong)
                    {
                        result = (result.endOfRound, result.ronPlayerId, CallTypePivot.Ron);
                    }
                    else
                    {
                        DiscardTileNotifier?.Invoke(new DiscardTileNotifierEventArgs());
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
                    ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.Kan, PotentialPreviousPlayerIndex = kanInProgress.Item3 });
                }

                if (!skipCurrentAction && _game.Round.CanCallPonOrKan(GamePivot.HUMAN_INDEX, out var isSelfKan))
                {
                    if (!isSelfKan)
                    {
                        DiscardTileNotifier?.Invoke(new DiscardTileNotifierEventArgs());
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
                    DiscardTileNotifier?.Invoke(new DiscardTileNotifierEventArgs());
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
                CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypePivot.Ron, PlayerIndex = opponentPlayerIndex });
            }

            return humanRonPending || opponentsCallRon.Count > 0;
        }

        private TilePivot OpponentBeginCallKan(int playerId, TilePivot kanTilePick, bool concealedKan)
        {
            TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

            var compensationTile = _game.Round.CallKan(playerId, concealedKan ? kanTilePick : null);
            if (compensationTile != null)
            {
                CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = playerId, Action = CallTypePivot.Kan });
            }
            return compensationTile;
        }

        private void Pick()
        {
            TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

            _ = _game.Round.Pick();

            PickNotifier?.Invoke(new PickNotifierEventArgs());
        }

        private void ChiiCall(Tuple<TilePivot, bool> chiiTilePick, int sleepTime)
        {
            TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

            if (_game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number))
            {
                CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypePivot.Chii, PlayerIndex = _game.Round.CurrentPlayerIndex });

                ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.Chii });

                if (!_game.Round.IsHumanPlayer)
                {
                    Discard(_game.Round.IaManager.DiscardDecision(new List<TilePivot>()), sleepTime);
                }
            }
        }

        private void PonCall(int playerIndex, int sleepTime)
        {
            TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

            // Note : this value is stored here because the call to "CallPon" makes it change.
            var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
            var isCpu = playerIndex != GamePivot.HUMAN_INDEX;

            if (_game.Round.CallPon(playerIndex))
            {
                CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = playerIndex, Action = CallTypePivot.Pon });

                ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.Pon, PreviousPlayerIndex = previousPlayerIndex, PlayerIndex = playerIndex });

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
                ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.NoCall });
            }
        }

        private bool OpponentAfterPick(ref Tuple<int, TilePivot, int?> kanInProgress, int sleepTime)
        {
            if (_game.Round.IaManager.TsumoDecision(kanInProgress != null))
            {
                CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypePivot.Tsumo, PlayerIndex = _game.Round.CurrentPlayerIndex });
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
                CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = _game.Round.CurrentPlayerIndex, Action = CallTypePivot.Riichi });
                Thread.Sleep(sleepTime);
            }

            if (_game.Round.CallRiichi(tile))
            {
                ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.Riichi });
            }
        }

        private CallTypePivot? HumanAutoPlay(bool autoCallMahjong, int sleepTime)
        {
            Pick();

            if (_game.Round.CanCallTsumo(false))
            {
                HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypePivot.Tsumo });
                return autoCallMahjong ? CallTypePivot.Tsumo : default(CallTypePivot?);
            }

            var riichiTiles = _game.Round.CanCallRiichi();
            RiichiChoicesNotifier?.Invoke(new RiichiChoicesNotifierEventArgs { Tiles = riichiTiles });
            if (riichiTiles.Count > 0)
            {
                var adviseRiichi = _game.Ruleset.DiscardTip && _game.Round.IaManager.RiichiDecision().choice != null;
                HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypePivot.Riichi, RiichiAdvised = adviseRiichi });
                return null;
            }
            else if (_game.Round.HumanCanAutoDiscard())
            {
                // Not a real CPU sleep: the auto-discard by human player is considered as such
                Thread.Sleep(sleepTime);
                return CallTypePivot.NoCall;
            }
            else
            {
                HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypePivot.NoCall });
            }

            return null;
        }
    }
}
