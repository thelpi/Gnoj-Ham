using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a tile.
/// </summary>
/// <seealso cref="IEquatable{T}"/>
/// <seealso cref="IComparable{T}"/>
public class TilePivot : IEquatable<TilePivot>, IComparable<TilePivot>
{
    #region Embedded properties

    // a unique code for a tile (consider this as the hashcode value)
    private readonly int _code;

    /// <summary>
    /// Family.
    /// </summary>
    public Families Family { get; }
    /// <summary>
    /// Number, between <c>1</c> and <c>9</c>.
    /// <c>0</c> for <see cref="Families.Wind"/> and <see cref="Families.Dragon"/>.
    /// </summary>
    public byte Number { get; }
    /// <summary>
    /// Wind.
    /// <c>Null</c> if not <see cref="Families.Wind"/>.
    /// </summary>
    public Winds? Wind { get; }
    /// <summary>
    /// Dragon.
    /// <c>Null</c> if not <see cref="Families.Dragon"/>.
    /// </summary>
    public Dragons? Dragon { get; }
    /// <summary>
    /// Indicates if the instance is a red dora.
    /// </summary>
    public bool IsRedDora { get; }

    #endregion Embedded properties

    #region Inferred properties

    /// <summary>
    /// Inferred; indicates if the instance is an honor.
    /// </summary>
    public bool IsHonor => Family == Families.Dragon || Family == Families.Wind;
    /// <summary>
    /// Inferred; indicates if the instance is a terminal.
    /// </summary>
    public bool IsTerminal => Number == 1 || Number == 9;
    /// <summary>
    /// Inferred; indicates if the instance is an honor or a terminal.
    /// </summary>
    public bool IsHonorOrTerminal => IsTerminal || IsHonor;

    #endregion Inferred properties

    #region Constructors

    // Constructor for non-honor families.
    private TilePivot(Families family, byte number, bool isRedDora = false)
    {
        Family = family;
        Number = number;
        IsRedDora = isRedDora;
        _code = number + (10 * (int)family);
    }

    // Constructor for wind.
    private TilePivot(Winds wind)
    {
        Family = Families.Wind;
        Wind = wind;
        _code = (int)(wind + 1) * 1000;
    }

    // Constructor for dragon.
    private TilePivot(Dragons dragon)
    {
        Family = Families.Dragon;
        Dragon = dragon;
        _code = (int)(dragon + 1) * 100;
    }

    #endregion Constructors

    #region Interfaces implementation and overrides from base

    /// <summary>
    /// Overriden; checks equality between an instance of <see cref="TilePivot"/> and any object.
    /// </summary>
    /// <param name="tile">The <see cref="TilePivot"/> instance.</param>
    /// <param name="obj">Any <see cref="object"/>.</param>
    /// <returns><c>True</c> if instances are equal or both <c>Null</c>; <c>False</c> otherwise.</returns>
    public static bool operator ==(TilePivot? tile, object? obj)
    {
        return tile is null ? obj is null : tile.Equals(obj);
    }

    /// <summary>
    /// Overriden; checks inequality between an instance of <see cref="TilePivot"/> and any object.
    /// </summary>
    /// <param name="tile">The <see cref="TilePivot"/> instance.</param>
    /// <param name="obj">Any <see cref="object"/>.</param>
    /// <returns><c>False</c> if instances are equal or both <c>Null</c>; <c>True</c> otherwise.</returns>
    public static bool operator !=(TilePivot? tile, object? obj)
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
    public int CompareTo(TilePivot? other)
    {
        if (other is null)
        {
            return 1;
        }

        return Family switch
        {
            Families.Dragon => other.Family == Families.Dragon ? Dragon!.Value < other.Dragon!.Value ? -1 : (Dragon.Value == other.Dragon.Value ? 0 : 1) : 1,
            Families.Wind => other.Family == Families.Wind
                                ? Wind!.Value < other.Wind!.Value ? -1 : (Wind.Value == other.Wind.Value ? 0 : 1)
                                : other.Family == Families.Dragon ? -1 : 1,
            _ => other.Family == Family
                                ? Number < other.Number
                                    ? -1
                                    : Number > other.Number ? 1 : IsRedDora && !other.IsRedDora ? -1 : !IsRedDora && other.IsRedDora ? 1 : 0
                                : other.Family < Family ? 1 : -1,
        };
    }

    /// <summary>
    /// Checks the equality between this instance and another one.
    /// <see cref="IsRedDora"/> value is ignored for this comparison.
    /// </summary>
    /// <param name="other">The second instance.</param>
    /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
    public bool Equals(TilePivot? other)
    {
        return other is not null
            && other._code == _code;
    }

    /// <summary>
    /// Overriden; provides an hashcode for this instance.
    /// </summary>
    /// <returns>Hashcode of this instance.</returns>
    public override int GetHashCode()
    {
        return _code;
    }

    /// <summary>
    /// Overriden; checks the equality between this instance and any other object.
    /// If <paramref name="obj"/> is a <see cref="TilePivot"/>, see <see cref="Equals(TilePivot)"/>.
    /// </summary>
    /// <param name="obj">Any <see cref="object"/>.</param>
    /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as TilePivot);
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
    internal static IReadOnlyList<TilePivot> GetCompleteSet(bool withRedDoras = false)
    {
        var tiles = new List<TilePivot>(136);

        foreach (var family in Enum.GetValues<Families>())
        {
            if (family == Families.Dragon)
            {
                foreach (var dragon in Enum.GetValues<Dragons>())
                {
                    for (var i = 0; i < 4; i++)
                    {
                        tiles.Add(new TilePivot(dragon));
                    }
                }
            }
            else if (family == Families.Wind)
            {
                foreach (var wind in Enum.GetValues<Winds>())
                {
                    for (var i = 0; i < 4; i++)
                    {
                        tiles.Add(new TilePivot(wind));
                    }
                }
            }
            else
            {
                for (byte j = 1; j <= 9; j++)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        tiles.Add(new TilePivot(family, j, withRedDoras && j == 5 && i == 3));
                    }
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// For unit tests purpose; gets a tile by criteria from a set.
    /// </summary>
    /// <param name="tilesSet">The set of tiles.</param>
    /// <param name="family">The <see cref="Family"/> value.</param>
    /// <param name="number">Optionnal; the <see cref="Number"/> value; default value is <c>Null</c>.</param>
    /// <param name="dragon">Optionnal; the <see cref="Dragon"/> value; default value is <c>Null</c>.</param>
    /// <param name="wind">Optionnal; the <see cref="Wind"/> value; default value is <c>Null</c>.</param>
    /// <param name="isRedDora">Optionnal; the <see cref="IsRedDora"/> value; default value is <c>Null</c>.</param>
    /// <returns></returns>
    internal static TilePivot GetTile(IEnumerable<TilePivot> tilesSet, Families family, byte? number = null,
        Dragons? dragon = null, Winds? wind = null, bool? isRedDora = null)
    {
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

        return tilesSet.First();
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Provides the resource name ( a file) associated to the tile.
    /// </summary>
    /// <returns>The resource name.</returns>
    public string ToResourceName()
    {
        return Family switch
        {
            Families.Dragon => $"{Family.ToString().ToLowerInvariant()}_{Dragon!.Value.ToString().ToLowerInvariant()}",
            Families.Wind => $"{Family.ToString().ToLowerInvariant()}_{Wind!.Value.ToString().ToLowerInvariant()}",
            _ => $"{Family.ToString().ToLowerInvariant()}_{Number}" + (IsRedDora ? "_red" : string.Empty),
        };
    }

    #endregion Public methods

    #region Internal methods

    /// <summary>
    /// Checks if this instance is dora when compared to another tile.
    /// </summary>
    /// <param name="other">The previous tile.</param>
    /// <returns><c>True</c> if dora; <c>False</c> otherwise.</returns>
    internal bool IsDoraNext(TilePivot other)
    {
        if (other.Family != Family)
        {
            return false;
        }

        return Family switch
        {
            Families.Dragon => other.Dragon!.Value == Dragons.Red
                                ? Dragon!.Value == Dragons.White
                                : other.Dragon.Value == Dragons.White ? Dragon!.Value == Dragons.Green : Dragon!.Value == Dragons.Red,
            Families.Wind => other.Wind!.Value == Winds.East
                                ? Wind!.Value == Winds.South
                                : other.Wind.Value == Winds.South
                                    ? Wind!.Value == Winds.West
                                    : other.Wind.Value == Winds.West ? Wind!.Value == Winds.North : Wind!.Value == Winds.East,
            _ => Number == (other.Number == 9 ? 1 : other.Number + 1),
        };
    }

    /// <summary>
    /// Checks if the tile is on the closed edge of a sequence combination.
    /// </summary>
    /// <param name="combo">The combination.</param>
    /// <returns><c>True</c> if on the closed edge; <c>False</c> otherwise.</returns>
    internal bool TileIsEdgeWait(TileComboPivot combo)
    {
        return combo.IsSequence && combo.Tiles.Contains(this)
            && (
                (combo.SequenceFirstNumber == 1 && combo.SequenceLastNumber == Number)
                || (combo.SequenceLastNumber == 9 && combo.SequenceFirstNumber == Number)
            );
    }

    /// <summary>
    /// Checks if the tile is in the middle of a sequence combination.
    /// </summary>
    /// <param name="combo">The combination.</param>
    /// <returns><c>True</c> if in the middle; <c>False</c> otherwise.</returns>
    internal bool TileIsMiddleWait(TileComboPivot combo)
    {
        return combo.IsSequence && combo.Tiles.Contains(this)
            && combo.SequenceFirstNumber != Number
            && combo.SequenceLastNumber != Number;
    }

    /// <summary>
    /// Computes the distance with the middle (<see cref="Number"/> 5). <c>0</c> if 5, <c>4</c> if 1 or 9.
    /// </summary>
    /// <param name="honorINotMiddle">If enabled, Honor are 5; otherwise 0.</param>
    /// <returns>The distance.</returns>
    internal int DistanceToMiddle(bool honorINotMiddle)
        => !honorINotMiddle && Number == 0 ? 0 : Math.Abs(Number - 5);

    #endregion Public methods
}
