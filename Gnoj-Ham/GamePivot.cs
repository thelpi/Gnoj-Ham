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
        /// Debug option to not randomize the tile draw.
        /// </summary>
        public bool SortedDraw { get; private set; }
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
        /// <param name="withRedDoras">Optionnal; the <see cref="WithRedDoras"/> value; default value is <c>False</c>.</param>
        /// <param name="sortedDraw">Optionnal; the <see cref="SortedDraw"/> value; default value is <c>False</c>.</param>
        /// <param name="useNagashiMangan">Optionnal; the <see cref="UseNagashiMangan"/> value; default value is <c>False</c>.</param>
        /// <param name="useRenhou">Optionnal; the <see cref="UseRenhou"/> value; default value is <c>False</c>.</param>
        public GamePivot(string humanPlayerName, InitialPointsRulePivot initialPointsRule, bool withRedDoras = false, bool sortedDraw = false,
            bool useNagashiMangan = false, bool useRenhou = false)
        {
            _players = PlayerPivot.GetFourPlayers(humanPlayerName, initialPointsRule);
            DominantWind = WindPivot.East;
            EastIndexTurnCount = 1;
            EastIndex = FirstEastIndex;
            EastRank = 1;
            WithRedDoras = withRedDoras;
            SortedDraw = sortedDraw;
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
        /// <param name="sortedDraw">The new <see cref="SortedDraw"/> value.</param>
        /// <param name="useNagashiMangan">The new <see cref="UseNagashiMangan"/> value.</param>
        /// <param name="useRenhou">The new <see cref="UseRenhou"/> value.</param>
        public void UpdateConfiguration(string humanPlayerName, bool withRedDoras, bool sortedDraw, bool useNagashiMangan, bool useRenhou)
        {
            PlayerPivot.UpdateHumanPlayerName(this, humanPlayerName);
            WithRedDoras = withRedDoras;
            SortedDraw = sortedDraw;
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

        #endregion Public methods
    }
}
