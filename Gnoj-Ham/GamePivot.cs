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

        #endregion Constants

        #region Embedded properties
        
        private readonly List<PlayerPivot> _players;

        /// <summary>
        /// List of players.
        /// </summary>
        public IReadOnlyCollection<PlayerPivot> Players
        {
            get
            {
                return _players;
            }
        }
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
        public int HonbaCountBeforeScoring { get { return HonbaCount > 0 ? HonbaCount - 1 : 0; } }
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
        /// <c>True</c> if akadora are used; <c>False</c> otherwise.
        /// </summary>
        public bool WithRedDoras { get; private set; }
        /// <summary>
        /// Indicates if the yaku <see cref="YakuPivot.NagashiMangan"/> is used or not.
        /// </summary>
        public bool UseNagashiMangan { get; private set; }
        /// <summary>
        /// Indicates if the yakuman <see cref="YakuPivot.Renhou"/> is used or not.
        /// </summary>
        public bool UseRenhou { get; private set; }
        /// <summary>
        /// The rule to check end of the game.
        /// </summary>
        public EndOfGameRulePivot EndOfGameRule { get; private set; }
        /// <summary>
        /// The rule for players initial points.
        /// </summary>
        public InitialPointsRulePivot InitialPointsRule { get; private set; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; current east player.
        /// </summary>
        public PlayerPivot CurrentEastPlayer
        {
            get
            {
                return _players[EastIndex];
            }
        }
        /// <summary>
        /// Inferred; get players sorted by their ranking.
        /// </summary>
        public IReadOnlyCollection<PlayerPivot> PlayersRanked
        {
            get
            {
                return _players.OrderByDescending(p => p.Points).ThenBy(p => (int)p.InitialWind).ToList();
            }
        }

        /// <summary>
        /// Inferred; gets the player index which was the first <see cref="WindPivot.East"/>.
        /// </summary>
        public int FirstEastIndex
        {
            get
            {
                return _players.FindIndex(p => p.InitialWind == WindPivot.East);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="humanPlayerName">The name of the human player; other players will be <see cref="PlayerPivot.IsCpu"/>.</param>
        /// <param name="initialPointsRule">The <see cref="InitialPointsRule"/> value.</param>
        /// <param name="endOfGameRule">The <see cref="EndOfGameRule"/> value.</param>
        /// <param name="withRedDoras">Optionnal; the <see cref="WithRedDoras"/> value; default value is <c>False</c>.</param>
        /// <param name="useNagashiMangan">Optionnal; the <see cref="UseNagashiMangan"/> value; default value is <c>False</c>.</param>
        /// <param name="useRenhou">Optionnal; the <see cref="UseRenhou"/> value; default value is <c>False</c>.</param>
        public GamePivot(string humanPlayerName, InitialPointsRulePivot initialPointsRule, EndOfGameRulePivot endOfGameRule,
            bool withRedDoras = false, bool useNagashiMangan = false, bool useRenhou = false)
        {
            InitialPointsRule = initialPointsRule;
            EndOfGameRule = endOfGameRule;
            _players = PlayerPivot.GetFourPlayers(humanPlayerName, InitialPointsRule);
            DominantWind = WindPivot.East;
            EastIndexTurnCount = 1;
            EastIndex = FirstEastIndex;
            EastRank = 1;
            WithRedDoras = withRedDoras;
            UseNagashiMangan = useNagashiMangan;
            UseRenhou = useRenhou;

            Round = new RoundPivot(this, EastIndex);
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Updates the current game configuration.
        /// </summary>
        /// <param name="humanPlayerName"></param>
        /// <param name="withRedDoras">The new <see cref="WithRedDoras"/> value.</param>
        /// <param name="useNagashiMangan">The new <see cref="UseNagashiMangan"/> value.</param>
        /// <param name="useRenhou">The new <see cref="UseRenhou"/> value.</param>
        public void UpdateConfiguration(string humanPlayerName, bool withRedDoras, bool useNagashiMangan, bool useRenhou)
        {
            PlayerPivot.UpdateHumanPlayerName(this, humanPlayerName);
            WithRedDoras = withRedDoras;
            UseNagashiMangan = useNagashiMangan;
            UseRenhou = useRenhou;
        }

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
        /// <returns>An instance of <see cref="EndOfRoundInformationsPivot"/>.</returns>
        public EndOfRoundInformationsPivot NextRound(int? ronPlayerIndex)
        {
            EndOfRoundInformationsPivot endOfRoundInformations = Round.EndOfRound(ronPlayerIndex);

            if (!endOfRoundInformations.Ryuukyoku)
            {
                PendingRiichiCount = 0;
            }

            if (!endOfRoundInformations.ToNextEast || endOfRoundInformations.Ryuukyoku)
            {
                HonbaCount++;
            }

            if (EndOfGameRule.TobiRuleApply() && _players.Any(p => p.Points < 0))
            {
                endOfRoundInformations.EndOfGame = true;
                ClearPendingRiichi();
                return endOfRoundInformations;
            }

            if (DominantWind == WindPivot.West || DominantWind == WindPivot.North)
            {
                if (!endOfRoundInformations.Ryuukyoku && _players.Any(p => p.Points >= 30000))
                {
                    endOfRoundInformations.EndOfGame = true;
                    ClearPendingRiichi();
                    return endOfRoundInformations;
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
                        if (EndOfGameRule.EnchousenRuleApply()
                            && InitialPointsRule == InitialPointsRulePivot.K25
                            && _players.All(p => p.Points < 30000))
                        {
                            DominantWind = WindPivot.West;
                        }
                        else
                        {
                            endOfRoundInformations.EndOfGame = true;
                            ClearPendingRiichi();
                            return endOfRoundInformations;
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
                        return endOfRoundInformations;
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

            return endOfRoundInformations;
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
                PlayerPivot winner = _players.OrderByDescending(p => p.Points).First();
                List<PlayerPivot> everyWinner = _players.Where(p => p.Points == winner.Points).ToList();
                if ((PendingRiichiCount * ScoreTools.RIICHI_COST) % everyWinner.Count != 0)
                {
                    // This is ugly...
                    everyWinner.Remove(everyWinner.Last());
                }
                foreach (PlayerPivot w in everyWinner)
                {
                    w.AddPoints((PendingRiichiCount * ScoreTools.RIICHI_COST) / everyWinner.Count);
                }
            }
        }

        #endregion Private methods
    }
}
