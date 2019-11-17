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
        
        private readonly bool _withRedDoras;
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
        /// <param name="initialPointsRule">The rule for initial points count.</param>
        /// <param name="withRedDoras">Optionnal; indicates if the set used for the game should contain red doras; default value is <c>False</c>.</param>
        public GamePivot(string humanPlayerName, InitialPointsRulePivot initialPointsRule, bool withRedDoras = false)
        {
            _players = PlayerPivot.GetFourPlayers(humanPlayerName, initialPointsRule);
            DominantWind = WindPivot.East;
            EastIndexTurnCount = 1;
            EastIndex = FirstEastIndex;
            EastRank = 1;
            _withRedDoras = withRedDoras;

            Round = new RoundPivot(this, EastIndex, _withRedDoras);
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
        /// <param name="isRon"><c>True</c> if the round ends on a ron call; <c>False</c> otherwise.</param>
        /// <returns>An instance of <see cref="EndOfRoundInformationsPivot"/>.</returns>
        public EndOfRoundInformationsPivot NextRound(bool isRon)
        {
            EndOfRoundInformationsPivot endOfRoundInformations = Round.EndOfRound(isRon);

            if (endOfRoundInformations.ResetRiichiPendingCount)
            {
                PendingRiichiCount = 0;
            }

            if (endOfRoundInformations.ToNextEast)
            {
                EastIndex = EastIndex.RelativePlayerIndex(1);
                EastIndexTurnCount = 1;
                EastRank++;

                if (EastIndex == FirstEastIndex)
                {
                    EastRank = 1;
                    // TODO : west turn if everyone is between 20k and 30k
                    // TODO : Riichi pending ?
                    if (DominantWind == WindPivot.South)
                    {
                        endOfRoundInformations.EndOfGame = true;
                        return endOfRoundInformations;
                    }
                    DominantWind = WindPivot.South;
                }
            }
            else
            {
                EastIndexTurnCount++;
            }

            Round = new RoundPivot(this, EastIndex, _withRedDoras);

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

        #endregion Public methods
    }
}
