using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a game.
    /// </summary>
    public class GamePivot
    {
        #region Constants

        /// <summary>
        /// Index of the human player in <see cref="Players"/>.
        /// </summary>
        public const int HUMAN_INDEX = 0;

        /// <summary>
        /// Number of tiles in a wall.
        /// </summary>
        public const int WALL_TILES_COUNT = 17;

        #endregion Constants

        #region Embedded properties

        private readonly PlayerSavePivot _save;
        private readonly List<PlayerPivot> _players;

        /// <summary>
        /// List of players.
        /// </summary>
        public IReadOnlyCollection<PlayerPivot> Players => _players;
        /// <summary>
        /// Current dominant wind.
        /// </summary>
        public WindPivot DominantWind { get; private set; }
        /// <summary>
        /// Index of the player in <see cref="_players"/> currently east.
        /// </summary>
        public int EastIndex { get; private set; }
        /// <summary>
        /// Number of rounds with the current <see cref="EastIndex"/>.
        /// </summary>
        public int EastIndexTurnCount { get; private set; }
        /// <summary>
        /// Honba count.
        /// </summary>
        /// <remarks>For scoring, <see cref="HonbaCountBeforeScoring"/> should be used.</remarks>
        public int HonbaCount { get; private set; }
        /// <summary>
        /// Honba count before scoring.
        /// </summary>
        public int HonbaCountBeforeScoring => HonbaCount > 0 ? HonbaCount - 1 : 0;
        /// <summary>
        /// Pending riichi count.
        /// </summary>
        public int PendingRiichiCount { get; private set; }
        /// <summary>
        /// East rank (1, 2, 3, 4).
        /// </summary>
        public int EastRank { get; private set; }
        /// <summary>
        /// Current <see cref="RoundPivot"/>.
        /// </summary>
        public RoundPivot Round { get; private set; }
        /// <summary>
        /// The ruleset for the game.
        /// </summary>
        public RulePivot Ruleset { get; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; current east player.
        /// </summary>
        public PlayerPivot CurrentEastPlayer => _players[EastIndex];
        /// <summary>
        /// Inferred; get players sorted by their ranking.
        /// </summary>
        public IReadOnlyCollection<PlayerPivot> PlayersRanked => _players.OrderByDescending(p => p.Points).ThenBy(p => (int)p.InitialWind).ToList();

        /// <summary>
        /// Inferred; gets the player index which was the first <see cref="WindPivot.East"/>.
        /// </summary>
        public int FirstEastIndex => _players.FindIndex(p => p.InitialWind == WindPivot.East);

        /// <summary>
        /// Inferred; indicates the game is between CPU only.
        /// </summary>
        public bool CpuVs => _players.All(_ => _.IsCpu);

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="humanPlayerName">The name of the human player; other players will be <see cref="PlayerPivot.IsCpu"/>.</param>
        /// <param name="ruleset">Ruleset for the game.</param>
        /// <param name="save">Player save stats.</param>
        public GamePivot(string humanPlayerName, RulePivot ruleset, PlayerSavePivot save)
        {
            _save = save;

            Ruleset = ruleset;
            _players = PlayerPivot.GetFourPlayers(humanPlayerName, Ruleset.InitialPointsRule, Ruleset.FourCpus);
            DominantWind = WindPivot.East;
            EastIndexTurnCount = 1;
            EastIndex = FirstEastIndex;
            EastRank = 1;

            Round = new RoundPivot(this, EastIndex);
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Adds a pending riichi.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        public void AddPendingRiichi(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            PendingRiichiCount++;
            _players[playerIndex].AddPoints(-ScoreTools.RIICHI_COST);
        }

        /// <summary>
        /// Manages the end of the current round and generates a new one.
        /// <see cref="Round"/> stays <c>Null</c> at the end of the game.
        /// </summary>
        /// <param name="ronPlayerIndex">The player index on who the call has been made; <c>Null</c> if tsumo or ryuukyoku.</param>
        /// <returns>An instance of <see cref="EndOfRoundInformationsPivot"/> and potentiel error on save of statistics.</returns>
        public (EndOfRoundInformationsPivot endOfRoundInformation, string error) NextRound(int? ronPlayerIndex)
        {
            var endOfRoundInformations = Round.EndOfRound(ronPlayerIndex);

            // used for stats
            var humanIsRiichi = Round.IsRiichi(HUMAN_INDEX);
            var humanIsConcealed = Round.GetHand(HUMAN_INDEX).IsConcealed;

            if (!endOfRoundInformations.Ryuukyoku)
            {
                PendingRiichiCount = 0;
            }

            if (!endOfRoundInformations.ToNextEast || endOfRoundInformations.Ryuukyoku)
            {
                HonbaCount++;
            }

            if (Ruleset.EndOfGameRule.TobiRuleApply() && _players.Any(p => p.Points < 0))
            {
                endOfRoundInformations.EndOfGame = true;
                ClearPendingRiichi();
                goto Exit;
            }

            if (DominantWind == WindPivot.West || DominantWind == WindPivot.North)
            {
                if (!endOfRoundInformations.Ryuukyoku && _players.Any(p => p.Points >= 30000))
                {
                    endOfRoundInformations.EndOfGame = true;
                    ClearPendingRiichi();
                    goto Exit;
                }
            }

            if (endOfRoundInformations.ToNextEast)
            {
                EastIndex = EastIndex.RelativePlayerIndex(1);
                EastIndexTurnCount = 1;
                EastRank++;

                if (EastIndex == FirstEastIndex)
                {
                    EastRank = 1;
                    if (DominantWind == WindPivot.South)
                    {
                        if (Ruleset.EndOfGameRule.EnchousenRuleApply()
                            && Ruleset.InitialPointsRule == InitialPointsRulePivot.K25
                            && _players.All(p => p.Points < 30000))
                        {
                            DominantWind = WindPivot.West;
                        }
                        else
                        {
                            endOfRoundInformations.EndOfGame = true;
                            ClearPendingRiichi();
                            goto Exit;
                        }
                    }
                    else if (DominantWind == WindPivot.West)
                    {
                        DominantWind = WindPivot.North;
                    }
                    else if (DominantWind == WindPivot.North)
                    {
                        endOfRoundInformations.EndOfGame = true;
                        ClearPendingRiichi();
                        goto Exit;
                    }
                    else
                    {
                        DominantWind = WindPivot.South;
                    }
                }
            }
            else
            {
                EastIndexTurnCount++;
            }

            Round = new RoundPivot(this, EastIndex);

        Exit:
            string error = null;
            if (Ruleset.AreDefaultRules())
            {
                var humanPlayer = Players.First(_ => !_.IsCpu);

                error = _save.UpdateAndSave(endOfRoundInformations,
                    ronPlayerIndex.HasValue,
                    humanIsRiichi,
                    humanIsConcealed,
                    Players.OrderByDescending(_ => _.Points).ToList().IndexOf(humanPlayer),
                    humanPlayer.Points);
            }

            return (endOfRoundInformations, error);
        }



        /// <summary>
        /// Gets the current <see cref="WindPivot"/> of the specified player.
        /// </summary>
        /// <param name="playerIndex">The player index in <see cref="Players"/>.</param>
        /// <returns>The <see cref="WindPivot"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerIndex"/> is out of range.</exception>
        public WindPivot GetPlayerCurrentWind(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            if (playerIndex == EastIndex + 1 || playerIndex == EastIndex - 3)
            {
                return WindPivot.South;
            }
            else if (playerIndex == EastIndex + 2 || playerIndex == EastIndex - 2)
            {
                return WindPivot.West;
            }
            else if (playerIndex == EastIndex + 3 || playerIndex == EastIndex - 1)
            {
                return WindPivot.North;
            }

            return WindPivot.East;
        }

        /// <summary>
        /// Gets the player index for the specified wind.
        /// </summary>
        /// <param name="wind">The wind.</param>
        /// <returns>The player index.</returns>
        public int GetPlayerIndexByCurrentWind(WindPivot wind)
        {
            return Enumerable.Range(0, 4).First(i => GetPlayerCurrentWind(i) == wind);
        }

        #endregion Public methods

        #region Private methods

        // At the end of the game, manage the remaining pending riichi.
        private void ClearPendingRiichi()
        {
            if (PendingRiichiCount > 0)
            {
                var winner = _players.OrderByDescending(p => p.Points).First();
                var everyWinner = _players.Where(p => p.Points == winner.Points).ToList();
                if ((PendingRiichiCount * ScoreTools.RIICHI_COST) % everyWinner.Count != 0)
                {
                    // This is ugly...
                    everyWinner.Remove(everyWinner.Last());
                }
                foreach (var w in everyWinner)
                {
                    w.AddPoints((PendingRiichiCount * ScoreTools.RIICHI_COST) / everyWinner.Count);
                }
            }
        }

        #endregion Private methods
    }
}
