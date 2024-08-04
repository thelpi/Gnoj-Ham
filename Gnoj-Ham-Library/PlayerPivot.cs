using System;
using System.Collections.Generic;
using System.Linq;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library
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
        /// <see cref="Winds"/> at the first round of the game.
        /// </summary>
        internal Winds InitialWind { get; private set; }
        /// <summary>
        /// Number of points.
        /// </summary>
        public int Points { get; private set; }
        /// <summary>
        /// Indicates if the player is managed by the CPU.
        /// </summary>
        internal bool IsCpu { get; private set; }
        /// <summary>
        /// Permanent player the current instance is based upon.
        /// </summary>
        internal PermanentPlayerPivot PermanentPlayer { get; }

        #endregion Embedded properties

        #region Constructors

        private PlayerPivot(string name, Winds initialWind, InitialPointsRules initialPointsRulePivot, bool isCpu, PermanentPlayerPivot permanentPlayer)
        {
            Name = name;
            InitialWind = initialWind;
            Points = initialPointsRulePivot.GetInitialPointsFromRule();
            IsCpu = isCpu;
            PermanentPlayer = permanentPlayer;
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
        /// <param name="random">Randomizer instance.</param>
        /// <returns>List of four <see cref="PlayerPivot"/>, not sorted.</returns>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidPlayerName"/></exception>
        /// <remarks>Keey the 'List' type in return.</remarks>
        internal static List<PlayerPivot> GetFourPlayers(string humanPlayerName, InitialPointsRules initialPointsRulePivot, Random random)
        {
            humanPlayerName = CheckName(humanPlayerName);

            var eastIndex = random.Next(0, 4);

            var players = new List<PlayerPivot>(4);
            for (var i = 0; i < 4; i++)
            {
                players.Add(new PlayerPivot(
                    i == GamePivot.HUMAN_INDEX ? humanPlayerName : $"{CPU_NAME_PREFIX}{i}",
                    GetWindFromIndex(eastIndex, i),
                    initialPointsRulePivot,
                    i != GamePivot.HUMAN_INDEX,
                    null
                ));
            }

            return players;
        }

        /// <summary>
        /// Generates a list of four <see cref="PlayerPivot"/> from permanent players.
        /// </summary>
        /// <param name="permanentPlayers">Four permanent players.</param>
        /// <param name="initialPointsRulePivot">Rule for initial points count.</param>
        /// <param name="random">Randomizer instance.</param>
        /// <returns>Four players generated.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="permanentPlayers"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException">Four players are required.</exception>
        /// <remarks>Keey the 'List' type in return.</remarks>
        internal static List<PlayerPivot> GetFourPlayersFromPermanent(IReadOnlyList<PermanentPlayerPivot> permanentPlayers, InitialPointsRules initialPointsRulePivot, Random random)
        {
            _ = permanentPlayers ?? throw new ArgumentNullException(nameof(permanentPlayers));

            if (permanentPlayers.Count != 4)
                throw new ArgumentException("Four players are required.", nameof(permanentPlayers));

            var eastIndex = random.Next(0, 4);

            return permanentPlayers
                .Select((p, i) => new PlayerPivot($"{CPU_NAME_PREFIX}{i}", GetWindFromIndex(eastIndex, i), initialPointsRulePivot, true, p))
                .ToList();
        }

        private static Winds GetWindFromIndex(int eastIndex, int i)
            => i == eastIndex ? Winds.East : (i > eastIndex ? (Winds)(i - eastIndex) : (Winds)(4 - eastIndex + i));

        private static string CheckName(string humanPlayerName)
        {
            humanPlayerName = (humanPlayerName ?? string.Empty).Trim();

            return humanPlayerName == string.Empty || humanPlayerName.ToUpperInvariant().StartsWith(CPU_NAME_PREFIX.ToUpperInvariant())
                ? null
                : humanPlayerName;
        }

        #endregion Static methods
    }
}
