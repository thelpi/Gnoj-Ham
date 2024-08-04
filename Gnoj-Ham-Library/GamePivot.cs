using System;
using System.Collections.Generic;
using System.Linq;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library
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
        private readonly Random _random;

        /// <summary>
        /// List of players.
        /// </summary>
        public IReadOnlyList<PlayerPivot> Players => _players;
        /// <summary>
        /// Current dominant wind.
        /// </summary>
        public Winds DominantWind { get; private set; }
        /// <summary>
        /// Index of the player in <see cref="_players"/> currently east.
        /// </summary>
        internal int EastIndex { get; private set; }
        /// <summary>
        /// Number of rounds with the current <see cref="EastIndex"/>.
        /// </summary>
        internal int EastIndexTurnCount { get; private set; }
        /// <summary>
        /// Honba count.
        /// </summary>
        /// <remarks>For scoring, <see cref="HonbaCountBeforeScoring"/> should be used.</remarks>
        public int HonbaCount { get; private set; }
        /// <summary>
        /// Honba count before scoring.
        /// </summary>
        internal int HonbaCountBeforeScoring => HonbaCount > 0 ? HonbaCount - 1 : 0;
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
        internal PlayerPivot CurrentEastPlayer => _players[EastIndex];
        /// <summary>
        /// Inferred; get players sorted by their ranking.
        /// </summary>
        internal IReadOnlyList<PlayerPivot> PlayersRanked => _players.OrderByDescending(p => p.Points).ThenBy(p => (int)p.InitialWind).ToList();

        /// <summary>
        /// Inferred; gets the player index which was the first <see cref="Winds.East"/>.
        /// </summary>
        internal int FirstEastIndex => _players.FindIndex(p => p.InitialWind == Winds.East);

        /// <summary>
        /// Inferred; indicates the game is between CPU only.
        /// </summary>
        internal bool CpuVs => _players.All(_ => _.IsCpu);

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="humanPlayerName">The name of the human player; other players will be <see cref="PlayerPivot.IsCpu"/>.</param>
        /// <param name="ruleset">Ruleset for the game.</param>
        /// <param name="save">Player save stats.</param>
        /// <param name="random">Randomizer instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="save"/> is <c>Null</c> while ruleset is default.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="ruleset"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="humanPlayerName"/> is invalid.</exception>
        public GamePivot(string humanPlayerName, RulePivot ruleset, PlayerSavePivot save, Random random)
        {
            _ = ruleset ?? throw new ArgumentNullException(nameof(ruleset));

            if (ruleset.AreDefaultRules() && save == null)
            {
                throw new ArgumentNullException(nameof(save));
            }
            
            _save = save;

            Ruleset = ruleset;
            _players = PlayerPivot.GetFourPlayers(humanPlayerName, Ruleset.InitialPointsRule, random);
            DominantWind = Winds.East;
            EastIndexTurnCount = 1;
            EastIndex = FirstEastIndex;
            EastRank = 1;
            _random = random;

            Round = new RoundPivot(this, EastIndex, random);
        }

        /// <summary>
        /// Constructor for CPU game with permanent players.
        /// </summary>
        /// <param name="ruleset">Ruleset for the game.</param>
        /// <param name="permanentPlayers">Four permanent players.</param>
        /// <param name="random">Randomizer instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="ruleset"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="permanentPlayers"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException">Four players are required.</exception>
        public GamePivot(RulePivot ruleset, IReadOnlyList<PermanentPlayerPivot> permanentPlayers, Random random)
        {
            _ = ruleset ?? throw new ArgumentNullException(nameof(ruleset));

            Ruleset = ruleset;
            _players = PlayerPivot.GetFourPlayersFromPermanent(permanentPlayers, Ruleset.InitialPointsRule, random);
            DominantWind = Winds.East;
            EastIndexTurnCount = 1;
            EastIndex = FirstEastIndex;
            EastRank = 1;
            _random = random;

            Round = new RoundPivot(this, EastIndex, random);
        }

        #endregion Constructors

        #region Public methods

        /// <summary>
        /// Computes the rank and score of every players at the current state of the game.
        /// </summary>
        /// <returns>A list of player with score, order by ascending rank.</returns>
        public IReadOnlyList<PlayerScorePivot> ComputeCurrentRanking()
        {
            var playersOrdered = new List<PlayerScorePivot>(4);

            var i = 1;
            foreach (var player in Players.OrderByDescending(p => p.Points))
            {
                playersOrdered.Add(new PlayerScorePivot(player, i, ScoreTools.ComputeUma(i), Ruleset.InitialPointsRule.GetInitialPointsFromRule()));
                i++;
            }

            return playersOrdered;
        }

        /// <summary>
        /// Adds a pending riichi.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        internal void AddPendingRiichi(int playerIndex)
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
            else
            {
                HonbaCount = 0;
            }

            if (Ruleset.EndOfGameRule.TobiRuleApply() && _players.Any(p => p.Points < 0))
            {
                endOfRoundInformations.EndOfGame = true;
                ClearPendingRiichi();
                goto Exit;
            }

            if (DominantWind == Winds.West || DominantWind == Winds.North)
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
                    if (DominantWind == Winds.South)
                    {
                        if (Ruleset.EndOfGameRule.EnchousenRuleApply()
                            && Ruleset.InitialPointsRule == InitialPointsRules.K25
                            && _players.All(p => p.Points < 30000))
                        {
                            DominantWind = Winds.West;
                        }
                        else
                        {
                            endOfRoundInformations.EndOfGame = true;
                            ClearPendingRiichi();
                            goto Exit;
                        }
                    }
                    else if (DominantWind == Winds.West)
                    {
                        DominantWind = Winds.North;
                    }
                    else if (DominantWind == Winds.North)
                    {
                        endOfRoundInformations.EndOfGame = true;
                        ClearPendingRiichi();
                        goto Exit;
                    }
                    else
                    {
                        DominantWind = Winds.South;
                    }
                }
            }
            else
            {
                EastIndexTurnCount++;
            }

            Round = new RoundPivot(this, EastIndex, _random);

        Exit:
            string error = null;
            if (Ruleset.AreDefaultRules())
            {
                var humanPlayer = Players.FirstOrDefault(_ => !_.IsCpu);
                if (humanPlayer != null)
                {
                    error = _save.UpdateAndSave(endOfRoundInformations,
                        ronPlayerIndex.HasValue,
                        humanIsRiichi,
                        humanIsConcealed,
                        Players.OrderByDescending(_ => _.Points).ToList().IndexOf(humanPlayer),
                        humanPlayer.Points);
                }
            }

            return (endOfRoundInformations, error);
        }



        /// <summary>
        /// Gets the current <see cref="Winds"/> of the specified player.
        /// </summary>
        /// <param name="playerIndex">The player index in <see cref="Players"/>.</param>
        /// <returns>The <see cref="Winds"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="playerIndex"/> is out of range.</exception>
        public Winds GetPlayerCurrentWind(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex > 3)
            {
                throw new ArgumentOutOfRangeException(nameof(playerIndex));
            }

            if (playerIndex == EastIndex + 1 || playerIndex == EastIndex - 3)
            {
                return Winds.South;
            }
            else if (playerIndex == EastIndex + 2 || playerIndex == EastIndex - 2)
            {
                return Winds.West;
            }
            else if (playerIndex == EastIndex + 3 || playerIndex == EastIndex - 1)
            {
                return Winds.North;
            }

            return Winds.East;
        }

        /// <summary>
        /// Gets the player index for the specified wind.
        /// </summary>
        /// <param name="wind">The wind.</param>
        /// <returns>The player index.</returns>
        internal int GetPlayerIndexByCurrentWind(Winds wind)
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
