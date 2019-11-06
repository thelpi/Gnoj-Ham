using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a game.
    /// </summary>
    public class GamePivot
    {
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
        /// <param name="initialPointsRule">The rule for initial points count.</param>
        /// <param name="withRedDoras">Optionnal; indicates if the set used for the game should contain red doras; default value is <c>False</c>.</param>
        public GamePivot(InitialPointsRulePivot initialPointsRule, bool withRedDoras = false)
        {
            _players = PlayerPivot.GetFourPlayers(initialPointsRule);
            DominantWind = WindPivot.East;
            EastIndexTurnCount = 1;
            EastIndex = _players.FindIndex(p => p.InitialWind == WindPivot.East);
            Round = new RoundPivot(EastIndex, withRedDoras);
        }

        #endregion Constructors
    }
}
