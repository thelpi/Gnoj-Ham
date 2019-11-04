using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a player.
    /// </summary>
    public class PlayerPivot
    {
        #region Embedded properties

        /// <summary>
        /// Name.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// <see cref="WindPivot"/> at the first round of the game.
        /// </summary>
        public WindPivot InitialWind { get; private set; }
        /// <summary>
        /// Number of points.
        /// </summary>
        public int Points { get; private set; }

        #endregion Embedded properties

        #region Constructors

        // Constructor
        private PlayerPivot(string name, WindPivot initialWind, InitialPointsRulePivot initialPointsRulePivot)
        {
            Name = name;
            InitialWind = initialWind;
            Points = initialPointsRulePivot.GetInitialPointsFromRule();
        }

        #endregion Constructors

        #region Static methods

        /// <summary>
        /// Generates a list of four <see cref="PlayerPivot"/> to start a game.
        /// </summary>
        /// <param name="initialPointsRulePivot">Rule for initial points count.</param>
        /// <returns>List of four <see cref="PlayerPivot"/>, sorted by their wind (east first).</returns>
        public static List<PlayerPivot> GetFourPlayers(InitialPointsRulePivot initialPointsRulePivot)
        {
            return Enumerable.Range(0, 4).Select(i => new PlayerPivot($"Player_{i + 1}", (WindPivot)i, initialPointsRulePivot)).ToList();
        }

        #endregion Static methods
    }
}
