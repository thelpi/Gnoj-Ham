using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a player.
    /// </summary>
    public class PlayerPivot
    {
        #region Constants

        private const string CPU_NAME_PREFIX = "CPU_";

        #endregion Constants

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
        /// <summary>
        /// Indicates if the player is managed by the CPU.
        /// </summary>
        public bool IsCpu { get; private set; }

        #endregion Embedded properties

        #region Constructors

        // Constructor.
        private PlayerPivot(string name, WindPivot initialWind, InitialPointsRulePivot initialPointsRulePivot, bool isCpu)
        {
            Name = name;
            InitialWind = initialWind;
            Points = initialPointsRulePivot.GetInitialPointsFromRule();
            IsCpu = isCpu;
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Adds a specified amount of points to the current player.
        /// </summary>
        /// <param name="points">The points count to add; might be negative.</param>
        internal void AddPoints(int points)
        {
            Points += points;
        }

        #endregion Public methods

        #region Static methods

        /// <summary>
        /// Generates a list of four <see cref="PlayerPivot"/> to start a game.
        /// </summary>
        /// <param name="humanPlayerName">The name of the human player; other players will be <see cref="IsCpu"/>.</param>
        /// <param name="initialPointsRulePivot">Rule for initial points count.</param>
        /// <returns>List of four <see cref="PlayerPivot"/>, not sorted.</returns>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidPlayerName"/></exception>
        public static List<PlayerPivot> GetFourPlayers(string humanPlayerName, InitialPointsRulePivot initialPointsRulePivot)
        {
            humanPlayerName = CheckName(humanPlayerName);

            int eastIndex = GlobalTools.Randomizer.Next(0, 4);

            var players = new List<PlayerPivot>();
            for (int i = 0; i < 4; i++)
            {
                players.Add(new PlayerPivot(
                    i == GamePivot.HUMAN_INDEX ? humanPlayerName : $"{CPU_NAME_PREFIX}{i}",
                    i == eastIndex ? WindPivot.East : (i > eastIndex ? (WindPivot)(i - eastIndex) : (WindPivot)(4 - eastIndex + i)),
                    initialPointsRulePivot,
                    i != GamePivot.HUMAN_INDEX
                ));
            }

            return players;
        }

        private static string CheckName(string humanPlayerName)
        {
            humanPlayerName = (humanPlayerName ?? string.Empty).Trim();

            if (humanPlayerName == string.Empty || humanPlayerName.ToUpperInvariant().StartsWith(CPU_NAME_PREFIX.ToUpperInvariant()))
            {
                throw new ArgumentException(Messages.InvalidPlayerName, nameof(humanPlayerName));
            }

            return humanPlayerName;
        }

        /// <summary>
        /// Updates the human player's name.
        /// </summary>
        /// <param name="game">The current game.</param>
        /// <param name="humanPlayerName">The new <see cref="Name"/> value for human player.</param>
        internal static void UpdateHumanPlayerName(GamePivot game, string humanPlayerName)
        {
            if (game == null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            humanPlayerName = CheckName(humanPlayerName);

            game.Players.ElementAt(GamePivot.HUMAN_INDEX).Name = humanPlayerName;
        }

        #endregion Static methods
    }
}
