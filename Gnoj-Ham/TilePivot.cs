using System;
using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Represents a tile.
    /// </summary>
    /// <seealso cref="IEquatable{T}"/>
    /// <seealso cref="IComparable{T}"/>
    public class TilePivot : IEquatable<TilePivot>, IComparable<TilePivot>
    {
        #region Embedded properties

        /// <summary>
        /// Family.
        /// </summary>
        public FamilyPivot Family { get; private set; }
        /// <summary>
        /// Number, between <c>1</c> and <c>9</c>.
        /// <c>0</c> for <see cref="FamilyPivot.Wind"/> and <see cref="FamilyPivot.Dragon"/>.
        /// </summary>
        public byte Number { get; private set; }
        /// <summary>
        /// Wind.
        /// <c>Null</c> if not <see cref="FamilyPivot.Wind"/>.
        /// </summary>
        public WindPivot? Wind { get; private set; }
        /// <summary>
        /// Dragon.
        /// <c>Null</c> if not <see cref="FamilyPivot.Dragon"/>.
        /// </summary>
        public DragonPivot? Dragon { get; private set; }
        /// <summary>
        /// Indicates if the instance is a red dora.
        /// </summary>
        public bool IsRedDora { get; private set; }

        #endregion Embedded properties

        #region Inferred properties

        /// <summary>
        /// Inferred; indicates if the instance is an honor.
        /// </summary>
        public bool IsHonor
        {
            get
            {
                return Family == FamilyPivot.Dragon || Family == FamilyPivot.Wind;
            }
        }
        /// <summary>
        /// Inferred; indicates if the instance is a terminal.
        /// </summary>
        public bool IsTerminal
        {
            get
            {
                return Number == 1 || Number == 9;
            }
        }

        #endregion Inferred properties

        #region Constructors

        // Constructor for non-honor families.
        private TilePivot(FamilyPivot family, byte number, bool isRedDora = false)
        {
            Family = family;
            Number = number;
            IsRedDora = isRedDora;
        }

        // Constructor for wind.
        private TilePivot(WindPivot wind)
        {
            Family = FamilyPivot.Wind;
            Wind = wind;
        }

        // Constructor for dragon.
        private TilePivot(DragonPivot dragon)
        {
            Family = FamilyPivot.Dragon;
            Dragon = dragon;
        }

        #endregion Constructors

        #region Interfaces implementation and overrides from base

        /// <summary>
        /// Overriden; checks equality between an instance of <see cref="TilePivot"/> and any object.
        /// </summary>
        /// <param name="tile">The <see cref="TilePivot"/> instance.</param>
        /// <param name="obj">Any <see cref="object"/>.</param>
        /// <returns><c>True</c> if instances are equal or both <c>Null</c>; <c>False</c> otherwise.</returns>
        public static bool operator ==(TilePivot tile, object obj)
        {
            return tile is null ? obj is null : tile.Equals(obj);
        }

        /// <summary>
        /// Overriden; checks inequality between an instance of <see cref="TilePivot"/> and any object.
        /// </summary>
        /// <param name="tile">The <see cref="TilePivot"/> instance.</param>
        /// <param name="obj">Any <see cref="object"/>.</param>
        /// <returns><c>False</c> if instances are equal or both <c>Null</c>; <c>True</c> otherwise.</returns>
        public static bool operator !=(TilePivot tile, object obj)
        {
            return !(tile == obj);
        }

        /// <summary>
        /// Compares this instance with another one.
        /// </summary>
        /// <param name="other">The second instance.</param>
        /// <returns>
        /// <c>-1</c> if this instance precedes <paramref name="other"/>.
        /// <c>0</c> if this instance is equal to <paramref name="other"/>.
        /// <c>1</c> if <paramref name="other"/> precedes this instance.
        /// </returns>
        public int CompareTo(TilePivot other)
        {
            if (other is null)
            {
                return 1;
            }

            switch (Family)
            {
                case FamilyPivot.Dragon:
                    if (other.Family == FamilyPivot.Dragon)
                    {
                        return Dragon.Value < other.Dragon.Value ? -1 : (Dragon.Value == other.Dragon.Value ? 0 : 1);
                    }
                    else
                    {
                        return 1;
                    }
                case FamilyPivot.Wind:
                    if (other.Family == FamilyPivot.Wind)
                    {
                        return Wind.Value < other.Wind.Value ? -1 : (Wind.Value == other.Wind.Value ? 0 : 1);
                    }
                    else if (other.Family == FamilyPivot.Dragon)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                default:
                    if (other.Family == Family)
                    {
                        return Number < other.Number ? -1 : (Number == other.Number ? 0 : 1);
                    }
                    else if (other.Family < Family)
                    {
                        return 1;
                    }
                    else
                    {
                        return -1;
                    }
            }
        }

        /// <summary>
        /// Checks the equality between this instance and another one.
        /// <see cref="IsRedDora"/> value is ignored for this comparison.
        /// </summary>
        /// <param name="other">The second instance.</param>
        /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
        public bool Equals(TilePivot other)
        {
            return !(other is null)
                && other.Family == Family
                && other.Wind == Wind
                && other.Dragon == Dragon
                && other.Number == Number;
        }

        /// <summary>
        /// Overriden; provides an hashcode for this instance.
        /// </summary>
        /// <returns>Hashcode of this instance.</returns>
        public override int GetHashCode()
        {
            return Tuple.Create(Family, Number, Wind, Dragon, IsRedDora).GetHashCode();
        }

        /// <summary>
        /// Overriden; checks the equality between this instance and any other object.
        /// If <paramref name="obj"/> is a <see cref="TilePivot"/>, see <see cref="Equals(TilePivot)"/>.
        /// </summary>
        /// <param name="obj">Any <see cref="object"/>.</param>
        /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as TilePivot);
        }

        /// <summary>
        /// Overriden; provides a textual representation of the instance.
        /// </summary>
        /// <returns>Textual representation of the instance.</returns>
        public override string ToString()
        {
            switch (Family)
            {
                case FamilyPivot.Dragon:
                    return $"{Family.ToString()} - {Dragon.Value.ToString()}";
                case FamilyPivot.Wind:
                    return $"{Family.ToString()} - {Wind.Value.ToString()}";
                default:
                    return $"{Family.ToString()} - {Number.ToString()}" + (IsRedDora ? " (R)" : string.Empty);
            }
        }

        #endregion Interfaces implementation and overrides from base

        #region Static methods

        /// <summary>
        /// Gets a complete set of <see cref="TilePivot"/>.
        /// </summary>
        /// <param name="withRedDoras">
        /// Optionnal; indicates if the set contains red doras; default value is <c>False</c>.
        /// Selected tiles are <c>5</c> of non-honor families (one of each).
        /// </param>
        /// <returns>A list of <see cref="TilePivot"/>.</returns>
        public static List<TilePivot> GetCompleteSet(bool withRedDoras = false)
        {
            var tiles = new List<TilePivot>();

            foreach (FamilyPivot family in Enum.GetValues(typeof(FamilyPivot)).Cast<FamilyPivot>())
            {
                if (family == FamilyPivot.Dragon)
                {
                    foreach (DragonPivot dragon in Enum.GetValues(typeof(DragonPivot)).Cast<DragonPivot>())
                    {
                        tiles.Add(new TilePivot(dragon), 4);
                    }
                }
                else if (family == FamilyPivot.Wind)
                {
                    foreach (WindPivot wind in Enum.GetValues(typeof(WindPivot)).Cast<WindPivot>())
                    {
                        tiles.Add(new TilePivot(wind), 4);
                    }
                }
                else
                {
                    for (byte j = 1; j <= 9; j++)
                    {
                        if (withRedDoras && j == 5)
                        {
                            tiles.Add(new TilePivot(family, j), 3);
                            tiles.Add(new TilePivot(family, j, true));
                        }
                        else
                        {
                            tiles.Add(new TilePivot(family, j), 4);
                        }
                    }
                }
            }

            return tiles;
        }

        /// <summary>
        /// Gets a tile by criteria from a set.
        /// </summary>
        /// <param name="tilesSet">The set of tiles.</param>
        /// <param name="family">The <see cref="Family"/> value.</param>
        /// <param name="number">Optionnal; the <see cref="Number"/> value; default value is <c>Null</c>.</param>
        /// <param name="dragon">Optionnal; the <see cref="Dragon"/> value; default value is <c>Null</c>.</param>
        /// <param name="wind">Optionnal; the <see cref="Wind"/> value; default value is <c>Null</c>.</param>
        /// <param name="isRedDora">Optionnal; the <see cref="IsRedDora"/> value; default value is <c>Null</c>.</param>
        /// <returns></returns>
        public static TilePivot GetTile(IEnumerable<TilePivot> tilesSet, FamilyPivot family, byte? number = null,
            DragonPivot? dragon = null, WindPivot? wind = null, bool? isRedDora = null)
        {
            if (tilesSet == null)
            {
                return null;
            }

            tilesSet = tilesSet.Where(t => t.Family == family);
            if (number.HasValue)
            {
                tilesSet = tilesSet.Where(t => t.Number == number.Value);
            }
            if (dragon.HasValue)
            {
                tilesSet = tilesSet.Where(t => t.Dragon == dragon.Value);
            }
            if (wind.HasValue)
            {
                tilesSet = tilesSet.Where(t => t.Wind == wind.Value);
            }
            if (isRedDora.HasValue)
            {
                tilesSet = tilesSet.Where(t => t.IsRedDora == isRedDora.Value);
            }

            return tilesSet.FirstOrDefault();
        }

        #endregion
    }
}
