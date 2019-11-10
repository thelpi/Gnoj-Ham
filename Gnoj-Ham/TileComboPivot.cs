using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a combination of <see cref="TilePivot"/>.
    /// </summary>
    /// <seealso cref="IEquatable{T}"/>
    public class TileComboPivot : IEquatable<TileComboPivot>
    {
        #region Embedded properties

        private readonly List<TilePivot> _tiles;

        /// <summary>
        /// Inferred; list of tiles; includes <see cref="OpenTile"/>.
        /// </summary>
        /// <remarks>Sorted by <see cref="IComparable{TilePivot}"/>.</remarks>
        public IReadOnlyCollection<TilePivot> Tiles
        {
            get
            {
                return _tiles;
            }
        }
        /// <summary>
        /// Optionnal tile not concealed (from a call "pon", "chi" or "kan").
        /// The tile from a call "ron" is not considered as an open tile.
        /// </summary>
        public readonly TilePivot OpenTile;
        /// <summary>
        /// If <see cref="OpenTile"/> is specified, indicates the wind which the tile has been stolen from; otherwise <c>Null</c>.
        /// </summary>
        public readonly WindPivot? StolenFrom;

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the combination is concealed.
        /// </summary>
        public bool IsConcealed
        {
            get
            {
                return OpenTile == null;
            }
        }

        /// <summary>
        /// Inferred; indicates if the combination is a pair.
        /// </summary>
        public bool IsPair
        {
            get
            {
                return _tiles.Count == 2;
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is a brelan.
        /// </summary>
        public bool IsBrelan
        {
            get
            {
                return _tiles.Count == 3 && !IsSequence;
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is a square.
        /// </summary>
        public bool IsSquare
        {
            get
            {
                return _tiles.Count == 4;
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is a sequence.
        /// </summary>
        public bool IsSequence
        {
            get
            {
                return _tiles.Count == 3 && _tiles[0].Number != _tiles[1].Number;
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is a brelan or a square.
        /// </summary>
        public bool IsBrelanOrSquare
        {
            get
            {
                return IsBrelan || IsSquare;
            }
        }

        /// <summary>
        /// Inferred; gets the combination <see cref="FamilyPivot"/>.
        /// </summary>
        public FamilyPivot Family
        {
            get
            {
                return _tiles[0].Family;
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is formed of honors.
        /// </summary>
        public bool IsHonor
        {
            get
            {
                return Family == FamilyPivot.Dragon || Family == FamilyPivot.Wind;
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is formed of terminals.
        /// </summary>
        /// <remarks><see cref="HasTerminal"/> is necessarily <c>True</c> in that case.</remarks>
        public bool IsTerminal
        {
            get
            {
                return !IsHonor && _tiles.All(t => t.Number == 1 || t.Number == 9);
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is formed with at least one terminal.
        /// </summary>
        public bool HasTerminal
        {
            get
            {
                return !IsHonor && _tiles.Any(t => t.Number == 1 || t.Number == 9);
            }
        }
        /// <summary>
        /// Inferred; indicates if the combination is <see cref="HasTerminal"/> or <see cref="IsHonor"/>.
        /// </summary>
        public bool HasTerminalOrHonor
        {
            get
            {
                return HasTerminal || IsHonor;
            }
        }
        /// <summary>
        /// Inferred; if sequence, the first number of it; otherwise <c>0</c>.
        /// </summary>
        public byte SequenceFirstNumber
        {
            get
            {
                return _tiles.Min(t => t.Number);
            }
        }
        /// <summary>
        /// Inferred; if sequence, the last number of it; otherwise <c>0</c>.
        /// </summary>
        public byte SequenceLastNumber
        {
            get
            {
                return _tiles.Max(t => t.Number);
            }
        }

        #endregion Inferred properties

        #region Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="concealedTiles">List of concealed tiles.</param>
        /// <param name="openTile">Optionnal; the <see cref="OpenTile"/> value; default value is <c>Null</c>.</param>
        /// <param name="stolenFrom">Optionnal; the <see cref="StolenFrom"/> value; default value is <c>Null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="concealedTiles"/> is <c>Null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidTilesCount"/></exception>
        /// <exception cref="ArgumentException"><see cref="Messages.InvalidCombination"/></exception>
        /// <exception cref="ArgumentException"><see cref="Messages.StolenFromNotSpecified"/></exception>
        public TileComboPivot(IEnumerable<TilePivot> concealedTiles, TilePivot openTile = null, WindPivot? stolenFrom = null)
        {
            if (concealedTiles is null)
            {
                throw new ArgumentNullException(nameof(concealedTiles));
            }

            var tiles = new List<TilePivot>(concealedTiles);
            if (openTile != null)
            {
                tiles.Add(openTile);
            }

            if (tiles.Count() < 2 || tiles.Count() > 4)
            {
                throw new ArgumentException(Messages.InvalidTilesCount, nameof(concealedTiles));
            }

            if (openTile != null && !stolenFrom.HasValue)
            {
                throw new ArgumentException(Messages.StolenFromNotSpecified, nameof(stolenFrom));
            }

            OpenTile = openTile;
            StolenFrom = stolenFrom;

            // The sort is important here...
            _tiles = tiles.OrderBy(t => t).ToList();

            // ...to check the validity of a potential sequence
            if (!IsValidCombination())
            {
                throw new ArgumentException(Messages.InvalidCombination, nameof(tiles));
            }
        }

        #endregion

        #region Interfaces implementation and overrides from base

        /// <summary>
        /// Overriden; checks equality between an instance of <see cref="TileComboPivot"/> and any object.
        /// </summary>
        /// <param name="tile">The <see cref="TileComboPivot"/> instance.</param>
        /// <param name="obj">Any <see cref="object"/>.</param>
        /// <returns><c>True</c> if instances are equal or both <c>Null</c>; <c>False</c> otherwise.</returns>
        public static bool operator ==(TileComboPivot tile, object obj)
        {
            return tile is null ? obj is null : tile.Equals(obj);
        }

        /// <summary>
        /// Overriden; checks inequality between an instance of <see cref="TileComboPivot"/> and any object.
        /// </summary>
        /// <param name="tile">The <see cref="TileComboPivot"/> instance.</param>
        /// <param name="obj">Any <see cref="object"/>.</param>
        /// <returns><c>False</c> if instances are equal or both <c>Null</c>; <c>True</c> otherwise.</returns>
        public static bool operator !=(TileComboPivot tile, object obj)
        {
            return !(tile == obj);
        }

        /// <summary>
        /// Checks the equality between this instance and another one.
        /// </summary>
        /// <param name="other">The second instance.</param>
        /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
        public bool Equals(TileComboPivot other)
        {
            return !(other is null) && _tiles.IsBijection(other.Tiles);
        }
        
        /// <summary>
        /// Overriden; provides an hashcode for this instance.
        /// </summary>
        /// <returns>Hashcode of this instance.</returns>
        public override int GetHashCode()
        {
            if (_tiles.Count == 2)
            {
                return Tuple.Create(_tiles[0], _tiles[1]).GetHashCode();
            }
            else if (_tiles.Count == 4)
            {
                return Tuple.Create(_tiles[0], _tiles[1], _tiles[2], _tiles[3]).GetHashCode();
            }
            else
            {
                return Tuple.Create(_tiles[0], _tiles[1], _tiles[2]).GetHashCode();
            }
        }

        /// <summary>
        /// Overriden; checks the equality between this instance and any other object.
        /// If <paramref name="obj"/> is a <see cref="TileComboPivot"/>, see <see cref="Equals(TileComboPivot)"/>.
        /// </summary>
        /// <param name="obj">Any <see cref="object"/>.</param>
        /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TileComboPivot);
        }

        /// <summary>
        /// Overriden; provides a textual representation of the instance.
        /// </summary>
        /// <returns>Textual representation of the instance.</returns>
        public override string ToString()
        {
            if (IsPair)
            {
                if (Family == FamilyPivot.Dragon)
                {
                    return $"Pair {Family} {_tiles[0].Dragon.Value.ToString()}";
                }
                else if (Family == FamilyPivot.Wind)
                {
                    return $"Pair {Family} {_tiles[0].Wind.Value.ToString()}";
                }
                else
                {
                    return $"Pair {Family} {_tiles[0].Number}";
                }
            }
            else if (IsBrelan)
            {
                if (Family == FamilyPivot.Dragon)
                {
                    return $"Brelan {Family} {_tiles[0].Dragon.Value.ToString()}";
                }
                else if (Family == FamilyPivot.Wind)
                {
                    return $"Brelan {Family} {_tiles[0].Wind.Value.ToString()}";
                }
                else
                {
                    return $"Brelan {Family} {_tiles[0].Number}";
                }
            }
            else if (IsSquare)
            {
                if (Family == FamilyPivot.Dragon)
                {
                    return $"Square {Family} {_tiles[0].Dragon.Value.ToString()}";
                }
                else if (Family == FamilyPivot.Wind)
                {
                    return $"Square {Family} {_tiles[0].Wind.Value.ToString()}";
                }
                else
                {
                    return $"Square {Family} {_tiles[0].Number}";
                }
            }
            else
            {
                return $"Sequence {Family} [{_tiles[0].Number}, {_tiles[1].Number}, {_tiles[2].Number}]";
            }
        }

        #endregion Interfaces implementation and overrides from base

        #region Private methods

        // Checks if the list of tiles forms a valid combination.
        private bool IsValidCombination()
        {
            IEnumerable<FamilyPivot> families = _tiles.Select(t => t.Family).Distinct();
            if (families.Count() > 1)
            {
                // KO : more than one family.
                return false;
            }

            FamilyPivot family = families.First();
            if (family == FamilyPivot.Dragon)
            {
                // Expected : only one type of dragon.
                return _tiles.Select(t => t.Dragon).Distinct().Count() == 1;
            }
            else if (family == FamilyPivot.Wind)
            {
                // Expected : only one type of wind.
                return _tiles.Select(t => t.Wind).Distinct().Count() == 1;
            }

            if (_tiles.Count() == 3)
            {
                if (_tiles.Select(t => t.Number).Distinct().Count() == 1)
                {
                    // OK : only one number of caracter / circle / bamboo.
                    return true;
                }
                else
                {
                    // Expected : tiles form a sequence [0 / +1 / +2]
                    return _tiles.ElementAt(0).Number == _tiles.ElementAt(1).Number - 1
                        && _tiles.ElementAt(1).Number == _tiles.ElementAt(2).Number - 1;
                }
            }
            else
            {
                // Expected : only one number of caracter / circle / bamboo.
                return _tiles.Select(t => t.Number).Distinct().Count() == 1;
            }
        }

        // Gets the tile corresponding to the specified wind in the purpose to create a sorted list for display.
        private KeyValuePair<TilePivot, bool> GetTileForSortedListAtSpecifiedWind(WindPivot wind, List<TilePivot> concealedOnly, ref int i)
        {
            if (wind == StolenFrom.Value)
            {
                return new KeyValuePair<TilePivot, bool>(OpenTile, true);
            }
            else
            {
                i++;
                return new KeyValuePair<TilePivot, bool>(concealedOnly[i - 1], false);
            }
        }

        #endregion Private methods

        #region Static methods

        /// <summary>
        /// Builds a pair from the specified tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>The pair.</returns>
        public static TileComboPivot BuildPair(TilePivot tile)
        {
            return Build(tile, 2);
        }

        /// <summary>
        /// Builds a brelan from the specified tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>The brelan.</returns>
        public static TileComboPivot BuildBrelan(TilePivot tile)
        {
            return Build(tile, 3);
        }

        /// <summary>
        /// Builds a square from the specified tile.
        /// </summary>
        /// <param name="tile">The tile.</param>
        /// <returns>The square.</returns>
        public static TileComboPivot BuildSquare(TilePivot tile)
        {
            return Build(tile, 4);
        }

        // Builds a pair, brelan or square of the specified tile.
        private static TileComboPivot Build(TilePivot tile, int k)
        {
            return new TileComboPivot(Enumerable.Range(0, k).Select(i => tile));
        }

        #endregion Static methods

        #region Public methods

        /// <summary>
        /// Gets the list of tiles from the combination, sorted by wind logic for display.
        /// </summary>
        /// <param name="ownerWind">The current wind of the owner.</param>
        /// <returns>List of tiles; value is <c>True</c> when the tile is the opened one.</returns>
        public List<KeyValuePair<TilePivot, bool>> GetSortedTilesForDisplay(WindPivot ownerWind)
        {
            if (!StolenFrom.HasValue)
            {
                return Tiles.Select(t => new KeyValuePair<TilePivot, bool>(t, false)).ToList();
            }

            var concealedOnly = new List<TilePivot>(_tiles);
            concealedOnly.Remove(OpenTile);

            int i = 0;

            var tiles = new List<KeyValuePair<TilePivot, bool>>
            {
                GetTileForSortedListAtSpecifiedWind(ownerWind.Left(), concealedOnly, ref i),
                GetTileForSortedListAtSpecifiedWind(ownerWind.Opposite(), concealedOnly, ref i)
            };

            // For a square, the third tile is never from an opponent.
            if (IsSquare)
            {
                tiles.Add(new KeyValuePair<TilePivot, bool>(concealedOnly[i], false));
                i++;
            }

            tiles.Add(GetTileForSortedListAtSpecifiedWind(ownerWind.Right(), concealedOnly, ref i));

            return tiles;
        }

        #endregion Public methods
    }
}
