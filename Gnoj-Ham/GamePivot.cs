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

        private bool _isEndOfRoundWithTurningWind;
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
        /// Riichi pending count.
        /// </summary>
        public int RiichiPendingCount { get; private set; }
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
            _isEndOfRoundWithTurningWind = false;

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

            RiichiPendingCount++;
            _players[playerIndex].AddPoints(-ScoreTools.RIICHI_COST);
        }

        /// <summary>
        /// Manages the end of a round.
        /// </summary>
        /// <remarks><see cref="Round"/> is set to <c>Null</c> to avoid any alteration.</remarks>
        /// <param name="winnerPlayersIndex">List of winners index with list of yakus for each.</param>
        /// <param name="loserPlayerIndex">Loser index, if any.</param>
        /// <exception cref="ArgumentNullException"><paramref name="winnerPlayersIndex"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidEndOfroundPlayer"/></exception>
        public void EndOfRound(Dictionary<int, List<YakuPivot>> winnerPlayersIndex, int? loserPlayerIndex)
        {
            if (winnerPlayersIndex == null)
            {
                throw new ArgumentNullException(nameof(winnerPlayersIndex));
            }

            if (winnerPlayersIndex.Any(w => w.Key < 0 || w.Key > 3 || w.Value == null || w.Value.Count == 0))
            {
                throw new ArgumentException(Messages.InvalidEndOfroundPlayer, nameof(winnerPlayersIndex));
            }

            if ((loserPlayerIndex.HasValue && (loserPlayerIndex.Value < 0 || loserPlayerIndex.Value > 3))
                || (winnerPlayersIndex.Count > 1 && !loserPlayerIndex.HasValue)
                || (winnerPlayersIndex.Count == 0 && loserPlayerIndex.HasValue)
                || (loserPlayerIndex.HasValue && winnerPlayersIndex.ContainsKey(loserPlayerIndex.Value)))
            {
                throw new ArgumentException(Messages.InvalidEndOfroundPlayer, nameof(loserPlayerIndex));
            }

            var pointsByPlayer = new Dictionary<int, int>();

            // Ryuukyoku (no winner).
            if (winnerPlayersIndex.Count == 0)
            {
                List<int> tenpaiPlayersIndex = Enumerable.Range(0, 4).Where(i => Round.IsTenpai(i)).ToList();
                List<int> notTenpaiPlayersIndex = Enumerable.Range(0, 4).Except(tenpaiPlayersIndex).ToList();

                // Wind turns if East is not tenpai.
                _isEndOfRoundWithTurningWind = notTenpaiPlayersIndex.Any(tpi => GetPlayerCurrentWind(tpi) == WindPivot.East);
                
                Tuple<int, int> points = ScoreTools.GetRyuukyokuPoints(tenpaiPlayersIndex.Count);

                tenpaiPlayersIndex.ForEach(i => pointsByPlayer.Add(i, points.Item1));
                notTenpaiPlayersIndex.ForEach(i => pointsByPlayer.Add(i, points.Item2));
            }
            else
            {
                // TODO : Sekinin barai :-(

                int eastOrLoserLostCumul = 0;
                int notEastLostCumul = 0;
                foreach (int pIndex in winnerPlayersIndex.Keys)
                {
                    HandPivot phand = Round.Hands.ElementAt(pIndex);

                    int dorasCount = phand.AllTiles.Sum(t => Round.DoraIndicatorTiles.Take(Round.VisibleDorasCount).Count(d => t.IsDoraNext(d)));
                    int uraDorasCount = winnerPlayersIndex[pIndex].Contains(YakuPivot.Riichi) || winnerPlayersIndex[pIndex].Contains(YakuPivot.DaburuRiichi) ?
                        phand.AllTiles.Sum(t => Round.UraDoraIndicatorTiles.Take(Round.VisibleDorasCount).Count(d => t.IsDoraNext(d))) : 0;
                    int redDorasCount = phand.AllTiles.Count(t => t.IsRedDora);

                    int fuCount = 0;
                    int fanCount = ScoreTools.GetFanCount(winnerPlayersIndex[pIndex], phand.IsConcealed, dorasCount, uraDorasCount, redDorasCount);
                    if (fanCount < 5)
                    {
                        fuCount = ScoreTools.GetFuCount(phand, !loserPlayerIndex.HasValue);
                    }

                    Tuple<int, int> finalScore = ScoreTools.GetPoints(fanCount, fuCount, EastIndexTurnCount, winnerPlayersIndex.Count,
                        !loserPlayerIndex.HasValue, GetPlayerCurrentWind(pIndex), RiichiPendingCount);
                    
                    pointsByPlayer.Add(pIndex, finalScore.Item1 + finalScore.Item2 * 2);
                    eastOrLoserLostCumul += finalScore.Item1;
                    notEastLostCumul += finalScore.Item2;
                }

                if (loserPlayerIndex.HasValue)
                {
                    pointsByPlayer.Add(loserPlayerIndex.Value, eastOrLoserLostCumul);
                }
                else
                {
                    for (int pIndex = 0; pIndex < 4; pIndex++)
                    {
                        if (!winnerPlayersIndex.ContainsKey(pIndex))
                        {
                            pointsByPlayer.Add(pIndex, GetPlayerCurrentWind(pIndex) == WindPivot.East ? eastOrLoserLostCumul : notEastLostCumul);
                        }
                    }
                }

                RiichiPendingCount = 0;
            }

            foreach (int pIndex in pointsByPlayer.Keys)
            {
                _players[pIndex].AddPoints(pointsByPlayer[pIndex]);
            }

            Round = null;
        }

        /// <summary>
        /// Generates a new round. <see cref="Round"/> stays <c>Null</c> at the end of the game.
        /// </summary>
        public void NewRound()
        {
            if (_isEndOfRoundWithTurningWind)
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
                        return;
                    }
                    DominantWind = WindPivot.South;
                }
            }
            else
            {
                EastIndexTurnCount++;
            }

            Round = new RoundPivot(this, EastIndex, _withRedDoras);
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
