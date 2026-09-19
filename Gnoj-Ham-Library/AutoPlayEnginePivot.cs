using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_Library.Events;

namespace Gnoj_Ham_Library;

/// <summary>
/// Drives the auto-play loop (CPU decisions and human-facing pauses) for a <see cref="RoundPivot"/>.
/// </summary>
internal class AutoPlayEnginePivot
{
    private readonly RoundPivot _round;

    internal AutoPlayEnginePivot(RoundPivot round)
    {
        _round = round;
    }

    internal AutoPlayResultPivot Run(
        CancellationToken cancellationToken,
        bool declinedHumanCall,
        bool humanRonPending,
        bool autoCallMahjong,
        bool discardTip,
        (TilePivot compensationTile, PlayerIndices? previousPlayerIndex)? humanKanCompensation,
        int sleepTime)
    {
        (PlayerIndices, TilePivot?, PlayerIndices?)? kanInProgress = null;
        if (humanKanCompensation.HasValue)
        {
            if (!_round.Game.HumanPlayerIndex.HasValue)
            {
                throw new InvalidOperationException("A human kan compensation was supplied, but this game has no human player.");
            }

            kanInProgress = (_round.Game.HumanPlayerIndex.Value, humanKanCompensation.Value.compensationTile, humanKanCompensation.Value.previousPlayerIndex);
        }

        var result = new AutoPlayResultPivot();
        var isFirstTurn = true;
        while (!cancellationToken.IsCancellationRequested)
        {
            // 0 - after one loop, there is no human decline remaining
            if (!isFirstTurn)
            {
                declinedHumanCall = false;
            }
            isFirstTurn = false;

            // 1 - checks if human (we have not checked yet) can call "ron"; the loop ends if it's the case
            // does not check Ron if we come in with a human kan in progress
            if (_round.Game.HumanPlayerIndex.HasValue && !humanKanCompensation.HasValue && !declinedHumanCall && !humanRonPending && _round.CanCallRon(_round.Game.HumanPlayerIndex.Value))
            {
                _round.RaiseHumanCallNotifier(new HumanCallNotifierEventArgs { Call = CallTypes.Ron });
                if (autoCallMahjong)
                {
                    result.HumanCall = (_round.Game.HumanPlayerIndex.Value, CallTypes.Ron);
                }
                else
                {
                    _round.RaiseDiscardTileNotifier(new DiscardTileNotifierEventArgs());
                }
                return result;
            }

            // 2 - this code runs after every human ron check has been made
            // the loop ends, with the "EndOfRound" marker, if any "ron" call is made
            if (CheckOpponensRonCall(humanRonPending))
            {
                result.EndOfRound = true;
                result.RonPlayerId = kanInProgress.HasValue ? kanInProgress.Value.Item1 : _round.PreviousPlayerIndex;
                if (kanInProgress.HasValue)
                {
                    _round.UndoPickCompensationTile();
                }
                return result;
            }

            // 2bis - "suucha riichi": all four players are riichi, and nobody called ron on the
            // discard which completed the fourth riichi; the round ends as an abortive draw.
            // "suukaikan": four kans declared by at least two different players, and nobody called
            // ron on the discard following the fourth kan; same abortive draw outcome.
            // "suufon renda" (optional rule): the fourth player's first-turn discard matches the
            // other three, and nobody called ron on it; same abortive draw outcome.
            if (_round.IsSuuchaRiichi || _round.IsSuukaikan || _round.IsSuufonRenda)
            {
                result.EndOfRound = true;
                return result;
            }

            // 3 - notify the UI of the kan
            // it's done here (and not right after the kan) because this is the first point where we
            // know the kan wasn't chankan'd. ResolveKanDoraReveal reveals the new dora right away for
            // an ankan, but only queues it (until the discard that follows) for an open kan - real
            // rule: an ankan's indicator flips immediately, a daiminkan/shouminkan's only after the
            // resulting discard (and never at all if the round ends on rinshan kaihou first).
            if (kanInProgress.HasValue)
            {
                _round.ResolveKanDoraReveal();
                _round.RaiseReadyToCallNotifier(new ReadyToCallNotifierEventArgs { Call = CallTypes.Kan, PotentialPreviousPlayerIndex = kanInProgress.Value.Item3 });
            }

            // 3bis - if we start the loop with a human kan in progress, go to 12
            if (!humanKanCompensation.HasValue)
            {
                // 4 - checks "pon" and "kan" calls for human player, except if declined
                if (_round.Game.HumanPlayerIndex.HasValue && !declinedHumanCall && _round.CanCallPonOrKan(_round.Game.HumanPlayerIndex.Value, out var isSelfKan))
                {
                    if (!isSelfKan)
                    {
                        _round.RaiseDiscardTileNotifier(new DiscardTileNotifierEventArgs());
                    }
                    return result;
                }

                // 5 - "kan" call from non-human players
                // the loop starts over
                var kanExit = false;
                foreach (var pi in Enum.GetValues<PlayerIndices>().Where(_round.Game.IsCpu))
                {
                    var (_, kanDecision) = _round.CpuManager(pi).KanDecision(pi, false);
                    if (kanDecision != null)
                    {
                        var previousPlayerIndex = _round.PreviousPlayerIndex;
                        var compensationTile = _round.OpponentBeginCallKan(pi, kanDecision, false);
                        kanInProgress = (pi, compensationTile, previousPlayerIndex);
                        kanExit = true;
                        break;
                    }
                }
                if (kanExit)
                {
                    continue;
                }

                // 6 - "pon" call from non-human players
                // the loop starts over
                var ponExit = false;
                foreach (var pi in Enum.GetValues<PlayerIndices>().Where(_round.Game.IsCpu))
                {
                    if (_round.CpuManager(pi).PonDecision(pi))
                    {
                        _round.PonCall(pi, sleepTime);
                        ponExit = true;
                        break;
                    }
                }
                if (ponExit)
                {
                    continue;
                }

                // 7 - checks "chii" call for current player (human)
                // exits the loop to let the UI suggests the call
                if (_round.IsHumanPlayer && !declinedHumanCall && _round.CanCallChii().Count > 0)
                {
                    _round.RaiseDiscardTileNotifier(new DiscardTileNotifierEventArgs());
                    return result;
                }

                // 8 - checks "chii" call for current player (non-human)
                // the loop starts over
                // A human player is never called for: the chii they have just declined (step 7 lets the
                // UI suggest it, once) must not be made anyway on the advice of their own CPU manager.
                if (!_round.IsHumanPlayer)
                {
                    var (_, chiiTilePick) = _round.CpuManager(_round.CurrentPlayerIndex).ChiiDecision();
                    if (chiiTilePick != null)
                    {
                        _round.ChiiCall(chiiTilePick, sleepTime);
                        continue;
                    }
                }

                // 9 - there is a "kan" call in progress by non-human player
                // several things can happen:
                // - tsumo from the caller (ends the loop)
                // - another "kan" call
                // - nothing special : checks "riichi" call and discard
                // note: in any case the loop starts over
                if (kanInProgress != null)
                {
                    if (OpponentAfterPick(ref kanInProgress, sleepTime))
                    {
                        result.EndOfRound = true;
                        return result;
                    }
                    continue;
                }

                // 10 - no more tiles to work with
                // ends the loop
                if (_round.IsWallExhaustion)
                {
                    result.EndOfRound = true;
                    return result;
                }

                // 11 - the current player picks a tile
                _round.AutoPick();
            }

            // 12 - consequence of a pick:
            // - for human player:
            //      - checks for tsumo (auto or manual)
            //      - checks for riichi
            //      - checks for auto discard
            // - for non human player, checks for tsumo, kan and riichi
            if (_round.IsHumanPlayer)
            {
                var call = HumanAutoPlay(autoCallMahjong, discardTip, sleepTime);
                if (call.HasValue)
                {
                    result.HumanCall = (_round.CurrentPlayerIndex, call.Value);
                }
                return result;
            }
            else if (OpponentAfterPick(ref kanInProgress, sleepTime))
            {
                result.EndOfRound = true;
                return result;
            }
        }

        return result;
    }

    private bool CheckOpponensRonCall(bool humanRonPending)
    {
        var atLeastOneRon = humanRonPending;
        foreach (var pi in Enum.GetValues<PlayerIndices>().Where(_round.Game.IsCpu))
        {
            var ronCalled = _round.CpuManager(pi).RonDecision(pi, atLeastOneRon);
            if (ronCalled)
            {
                atLeastOneRon = true;
                _round.RaiseCallNotifier(new CallNotifierEventArgs { Action = CallTypes.Ron, PlayerIndex = pi });
            }
        }

        return atLeastOneRon;
    }

    private bool OpponentAfterPick(ref (PlayerIndices, TilePivot?, PlayerIndices?)? kanInProgress, int sleepTime)
    {
        var currentPlayerIndex = _round.CurrentPlayerIndex;

        var tsumoDecision = _round.CpuManager(currentPlayerIndex).TsumoDecision(kanInProgress != null);
        if (tsumoDecision)
        {
            _round.RaiseCallNotifier(new CallNotifierEventArgs { Action = CallTypes.Tsumo, PlayerIndex = currentPlayerIndex });
            return true;
        }

        // Computed at most once per pick: the riichi eligibility check and, if riichi isn't called,
        // the discard-to-stay-tenpai check right below ask the exact same question ("what can I
        // discard and remain tenpai?") on the exact same, still-unchanged hand. Null means the
        // question was never actually asked (open hand, already riichi, not enough wall/points...),
        // as opposed to "asked and the answer is empty" - only the latter is safe to reuse as-is.
        var tenpaiDiscardChoices = _round.CanConsiderRiichi() ? _round.ExtractDiscardChoicesFromTenpai(currentPlayerIndex) : null;

        // A first-turn kokushi-tenpai hand also satisfies CanCallKyuushuKyuuhai, but a real tenpai
        // shape (daburu riichi, ippatsu, pressure on opponents' discards) always takes priority over
        // aborting the round.
        if ((tenpaiDiscardChoices == null || tenpaiDiscardChoices.Count == 0) && _round.CpuManager(currentPlayerIndex).KyuushuKyuuhaiDecision())
        {
            _round.CallKyuushuKyuuhai();
            _round.RaiseCallNotifier(new CallNotifierEventArgs { Action = CallTypes.KyuushuKyuuhai, PlayerIndex = currentPlayerIndex });
            return true;
        }

        var (_, kanTile) = _round.CpuManager(currentPlayerIndex).KanDecision(currentPlayerIndex, true);
        if (kanTile != null)
        {
            var compensationTile = _round.OpponentBeginCallKan(currentPlayerIndex, kanTile, true);
            kanInProgress = (currentPlayerIndex, compensationTile, null);
            return false;
        }

        kanInProgress = null;

        var riichiTile = _round.CpuManager(currentPlayerIndex).RiichiDecision(tenpaiDiscardChoices);
        if (riichiTile != null)
        {
            _round.CallRiichi(riichiTile, sleepTime);
            return false;
        }

        _round.Discard(_round.CpuManager(currentPlayerIndex).DiscardDecision(tenpaiDiscardChoices), sleepTime);
        return false;
    }

    private CallTypes? HumanAutoPlay(bool autoCallMahjong, bool discardTip, int sleepTime)
    {
        if (_round.CanCallTsumo(false))
        {
            _round.RaiseHumanCallNotifier(new HumanCallNotifierEventArgs { Call = CallTypes.Tsumo });
            return autoCallMahjong ? CallTypes.Tsumo : default(CallTypes?);
        }

        var riichiTiles = _round.CanCallRiichi();
        _round.RaiseRiichiChoicesNotifier(new RiichiChoicesNotifierEventArgs(riichiTiles));
        if (riichiTiles.Count > 0)
        {
            // A first-turn tenpai hand (even a kokushi musou one, which also satisfies
            // CanCallKyuushuKyuuhai) is a real offensive opportunity - daburu riichi, ippatsu,
            // pressure on opponents' discards - not a reason to abort the round. Riichi always
            // takes priority when both are legally available.
            var adviseRiichi = discardTip && _round.CpuManager(_round.CurrentPlayerIndex).RiichiDecision(riichiTiles) != null;
            _round.RaiseHumanCallNotifier(new HumanCallNotifierEventArgs { Call = CallTypes.Riichi, RiichiAdvised = adviseRiichi });
            return null;
        }
        else if (_round.CanCallKyuushuKyuuhai())
        {
            _round.RaiseHumanCallNotifier(new HumanCallNotifierEventArgs { Call = CallTypes.KyuushuKyuuhai });
            return null;
        }
        else if (_round.HumanCanAutoDiscard())
        {
            // Not a real CPU sleep: the auto-discard by human player is considered as such
            Thread.Sleep(sleepTime);
            return CallTypes.NoCall;
        }
        else
        {
            _round.RaiseHumanCallNotifier(new HumanCallNotifierEventArgs { Call = CallTypes.NoCall });
        }

        return null;
    }
}
