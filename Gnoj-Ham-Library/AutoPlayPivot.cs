using Gnoj_Ham_Library.Events;

namespace Gnoj_Ham_Library;

/// <summary>
/// Autu play management.
/// </summary>
public class AutoPlayPivot
{
    private readonly GamePivot _game;

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

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="game">The game instance.</param>
    public AutoPlayPivot(GamePivot game)
    {
        _game = game;
    }

    #endregion Constructors

    /// <summary>
    /// Starts and runs the auto player.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="declinedHumanCall">Indicates that a potential call has been suggested to the human player and has been declined..</param>
    /// <param name="humanRonPending">Indicates that the human player has called 'Ron', but the same call by opponents has to be checked too.</param>
    /// <param name="autoCallMahjong">When enabled, if the human player can call 'Tsumo' or 'Ron', the call is automatically made.</param>
    /// <param name="sleepTime">The time to wait after any action (call or discard).</param>
    /// <returns>A tuple that includes:
    /// <list type="bullet">
    /// <item>endOfRound: indicates if the round is over; otherwise, the control is given back to the human player.</item>
    /// <item>ronPlayerId: indicates, if one or several calls 'Ron' has been made, the player index who lost in that situation.</item>
    /// <item>humanAction: indicates a decision to automatically apply when the control is given back to human player.</item>
    /// </list>
    /// </returns>
    public (bool endOfRound, int? ronPlayerId, CallTypePivot? humanAction) RunAutoPlay(
        CancellationToken cancellationToken,
        bool declinedHumanCall = false,
        bool humanRonPending = false,
        bool autoCallMahjong = false,
        int sleepTime = 0)
    {
        Tuple<int, TilePivot, int?> kanInProgress = null;
        (bool endOfRound, int? ronPlayerId, CallTypePivot? humanAction) result = default;
        var isFirstTurn = true;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!isFirstTurn)
                declinedHumanCall = false;
            isFirstTurn = false;

            if (!_game.CpuVs && !declinedHumanCall && !humanRonPending && _game.Round.CanCallRon(GamePivot.HUMAN_INDEX))
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

            if (!_game.CpuVs && !declinedHumanCall && _game.Round.CanCallPonOrKan(GamePivot.HUMAN_INDEX, out var isSelfKan))
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

            if (!declinedHumanCall && _game.Round.IsHumanPlayer && _game.Round.CanCallChii().Count > 0)
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

            Pick();
            if (_game.Round.IsHumanPlayer)
            {
                result = (result.endOfRound, result.ronPlayerId, HumanAutoPlay(autoCallMahjong, sleepTime));
                break;
            }
            else if (OpponentAfterPick(ref kanInProgress, sleepTime))
            {
                result = (true, result.ronPlayerId, result.humanAction);
                break;
            }
        }

        return result;
    }

    #region Private methods

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

        var callChii = _game.Round.CallChii(chiiTilePick.Item2 ? chiiTilePick.Item1.Number - 1 : chiiTilePick.Item1.Number);
        if (callChii)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { Action = CallTypePivot.Chii, PlayerIndex = _game.Round.CurrentPlayerIndex });

            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.Chii });

            if (!_game.Round.IsHumanPlayer)
            {
                var discardDecision = _game.Round.IaManager.DiscardDecision(new List<TilePivot>());
                Discard(discardDecision, sleepTime);
            }
        }
    }

    private void PonCall(int playerIndex, int sleepTime)
    {
        TurnChangeNotifier?.Invoke(new TurnChangeNotifierEventArgs());

        // Note : this value is stored here because the call to "CallPon" makes it change.
        var previousPlayerIndex = _game.Round.PreviousPlayerIndex;
        var isCpu = playerIndex != GamePivot.HUMAN_INDEX || _game.CpuVs;

        var callPon = _game.Round.CallPon(playerIndex);
        if (callPon)
        {
            CallNotifier?.Invoke(new CallNotifierEventArgs { PlayerIndex = playerIndex, Action = CallTypePivot.Pon });

            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.Pon, PreviousPlayerIndex = previousPlayerIndex, PlayerIndex = playerIndex });

            if (isCpu)
            {
                var discardDecision = _game.Round.IaManager.DiscardDecision(new List<TilePivot>());
                Discard(discardDecision, sleepTime);
            }
        }
    }

    private void Discard(TilePivot tile, int sleepTime)
    {
        if (!_game.Round.IsHumanPlayer)
        {
            Thread.Sleep(sleepTime);
        }

        var hasDiscard = _game.Round.Discard(tile);
        if (hasDiscard)
        {
            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.NoCall });
        }
    }

    private bool OpponentAfterPick(ref Tuple<int, TilePivot, int?> kanInProgress, int sleepTime)
    {
        var tsumoDecision = _game.Round.IaManager.TsumoDecision(kanInProgress != null);
        if (tsumoDecision)
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

        var callRiichi = _game.Round.CallRiichi(tile);
        if (callRiichi)
        {
            ReadyToCallNotifier?.Invoke(new ReadyToCallNotifierEventArgs { Call = CallTypePivot.Riichi });
        }
    }

    private CallTypePivot? HumanAutoPlay(bool autoCallMahjong, int sleepTime)
    {
        if (_game.Round.CanCallTsumo(false))
        {
            HumanCallNotifier?.Invoke(new HumanCallNotifierEventArgs { Call = CallTypePivot.Tsumo });
            return autoCallMahjong ? CallTypePivot.Tsumo : default(CallTypePivot?);
        }

        var riichiTiles = _game.Round.CanCallRiichi();
        RiichiChoicesNotifier?.Invoke(new RiichiChoicesNotifierEventArgs(riichiTiles));
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

    #endregion Private methods
}
