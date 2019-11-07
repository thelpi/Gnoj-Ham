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
                // TODO: what to do in case of equality ?
                return _players.OrderByDescending(p => p.Points).ThenBy(p => (int)p.InitialWind).ToList();
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
            EastIndex = _players.FindIndex(p => p.InitialWind == WindPivot.East);
            Round = new RoundPivot(this, EastIndex, withRedDoras);
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Gets the current <see cref="WindPivot"/> of the specified player.
        /// </summary>
        /// <param name="playerIndex">The player index in <see cref="Players"/>.</param>
        /// <returns>The <see cref="WindPivot"/>.</returns>
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
