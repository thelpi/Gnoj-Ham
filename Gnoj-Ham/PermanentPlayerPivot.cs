using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a CPU player across several games.
    /// </summary>
    public class PermanentPlayerPivot
    {
        private readonly List<PlayerScorePivot> _scores = new List<PlayerScorePivot>(10);

        /// <summary>
        /// Unique identifier.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Number of games played.
        /// </summary>
        internal int GamesCount => _scores.Count;

        /// <summary>
        /// Cumulated score.
        /// </summary>
        public int TotalScore => _scores.Sum(s => s.Score);

        /// <summary>
        /// Number of first places.
        /// </summary>
        public int FirstPlaceCount => _scores.Count(s => s.Rank == 1);

        /// <summary>
        /// Number of first or second places.
        /// </summary>
        public int FirstOrSecondPlaceCount => _scores.Count(s => s.Rank == 1 || s.Rank == 2);

        /// <summary>
        /// Number of last places.
        /// </summary>
        public int LastPlaceCount => _scores.Count(s => s.Rank == 4);

        /// <summary>
        /// Average score.
        /// </summary>
        public double AverageScore => _scores.Average(s => s.Score);

        /// <summary>
        /// Average rank.
        /// </summary>
        public double AverageRank => _scores.Average(s => s.Rank);

        /// <summary>
        /// Constructor.
        /// </summary>
        public PermanentPlayerPivot()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>
        /// Add a score sheet for on game.
        /// </summary>
        /// <param name="score"></param>
        /// <exception cref="ArgumentNullException"><paramref name="score"/> is <c>Null</c>.</exception>
        internal void AddGameScore(PlayerScorePivot score)
        {
            _ = score ?? throw new ArgumentNullException(nameof(score));

            _scores.Add(score);
        }

        /// <summary>
        /// Resets player's score.
        /// </summary>
        internal void ResetScores()
        {
            _scores.Clear();
        }
    }
}
