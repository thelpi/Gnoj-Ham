﻿using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents a combination of <see cref="TilePivot"/>.
/// </summary>
/// <seealso cref="IEquatable{T}"/>
public class TileComboPivot : IEquatable<TileComboPivot>
{
    #region Embedded properties

    private readonly TilePivot[] _tiles;

    /// <summary>
    /// Inferred; list of tiles; includes <see cref="OpenTile"/>.
    /// </summary>
    /// <remarks>Sorted by <see cref="IComparable{TilePivot}"/>.</remarks>
    public IReadOnlyList<TilePivot> Tiles => _tiles;
    /// <summary>
    /// Optionnal tile not concealed (from a call "pon", "chi" or "kan").
    /// The tile from a call "ron" is not considered as an open tile.
    /// </summary>
    public readonly TilePivot? OpenTile;
    /// <summary>
    /// If <see cref="OpenTile"/> is specified, indicates the wind which the tile has been stolen from; otherwise <c>Null</c>.
    /// </summary>
    public readonly Winds? StolenFrom;

    #endregion Embedded properties

    #region Inferred properties

    /// <summary>
    /// Inferred; indicates if the combination is concealed.
    /// </summary>
    public bool IsConcealed => OpenTile == null;
    /// <summary>
    /// Inferred; indicates if the combination is a pair.
    /// </summary>
    public bool IsPair => _tiles.Length == 2;
    /// <summary>
    /// Inferred; indicates if the combination is a brelan.
    /// </summary>
    public bool IsBrelan => _tiles.Length == 3 && !IsSequence;
    /// <summary>
    /// Inferred; indicates if the combination is a square.
    /// </summary>
    public bool IsSquare => _tiles.Length == 4;
    /// <summary>
    /// Inferred; indicates if the combination is a sequence.
    /// </summary>
    public bool IsSequence => _tiles.Length == 3 && _tiles[0].Number != _tiles[1].Number;
    /// <summary>
    /// Inferred; indicates if the combination is a brelan or a square.
    /// </summary>
    public bool IsBrelanOrSquare => IsBrelan || IsSquare;

    /// <summary>
    /// Inferred; gets the combination <see cref="Families"/>.
    /// </summary>
    public Families Family => _tiles[0].Family;
    /// <summary>
    /// Inferred; indicates if the combination is formed of honors.
    /// </summary>
    public bool IsHonor => Family == Families.Dragon || Family == Families.Wind;
    /// <summary>
    /// Inferred; indicates if the combination is formed of terminals.
    /// </summary>
    /// <remarks><see cref="HasTerminal"/> is necessarily <c>True</c> in that case.</remarks>
    public bool IsTerminal => !IsHonor && _tiles.All(t => t.Number == 1 || t.Number == 9);
    /// <summary>
    /// Inferred; indicates if the combination is formed with at least one terminal.
    /// </summary>
    public bool HasTerminal => !IsHonor && _tiles.Any(t => t.Number == 1 || t.Number == 9);
    /// <summary>
    /// Inferred; indicates if the combination is <see cref="HasTerminal"/> or <see cref="IsHonor"/>.
    /// </summary>
    public bool HasTerminalOrHonor => HasTerminal || IsHonor;
    /// <summary>
    /// Inferred; if sequence, the first number of it; otherwise <c>0</c>.
    /// </summary>
    public byte SequenceFirstNumber => _tiles.Min(t => t.Number);
    /// <summary>
    /// Inferred; if sequence, the last number of it; otherwise <c>0</c>.
    /// </summary>
    public byte SequenceLastNumber => _tiles.Max(t => t.Number);

    #endregion Inferred properties

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="concealedTiles">List of concealed tiles.</param>
    /// <param name="openTile">Optionnal; the <see cref="OpenTile"/> value; default value is <c>Null</c>.</param>
    /// <param name="stolenFrom">Optionnal; the <see cref="StolenFrom"/> value; default value is <c>Null</c>.</param>
    internal TileComboPivot(IEnumerable<TilePivot> concealedTiles, TilePivot? openTile = null, Winds? stolenFrom = null)
    {
        var tiles = new List<TilePivot>(concealedTiles);
        if (openTile != null)
        {
            tiles.Add(openTile);
        }

        OpenTile = openTile;
        StolenFrom = stolenFrom;

        // The sort is important here...
        _tiles = tiles.OrderBy(t => t).ToArray();
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tiles">List of concealed tiles.</param>
    internal TileComboPivot(params TilePivot[] tiles)
    {
        OpenTile = null;
        StolenFrom = null;

        // The sort is important here...
        _tiles = tiles;
    }

    #endregion

    #region Interfaces implementation and overrides from base

    /// <summary>
    /// Overriden; checks equality between an instance of <see cref="TileComboPivot"/> and any object.
    /// </summary>
    /// <param name="tile">The <see cref="TileComboPivot"/> instance.</param>
    /// <param name="obj">Any <see cref="object"/>.</param>
    /// <returns><c>True</c> if instances are equal or both <c>Null</c>; <c>False</c> otherwise.</returns>
    public static bool operator ==(TileComboPivot? tile, object? obj)
    {
        return tile is null ? obj is null : tile.Equals(obj);
    }

    /// <summary>
    /// Overriden; checks inequality between an instance of <see cref="TileComboPivot"/> and any object.
    /// </summary>
    /// <param name="tile">The <see cref="TileComboPivot"/> instance.</param>
    /// <param name="obj">Any <see cref="object"/>.</param>
    /// <returns><c>False</c> if instances are equal or both <c>Null</c>; <c>True</c> otherwise.</returns>
    public static bool operator !=(TileComboPivot? tile, object? obj)
    {
        return !(tile == obj);
    }

    /// <summary>
    /// Checks the equality between this instance and another one.
    /// </summary>
    /// <param name="other">The second instance.</param>
    /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
    public bool Equals(TileComboPivot? other)
    {
        return other is not null && _tiles.IsBijection(other.Tiles);
    }

    /// <summary>
    /// Overriden; provides an hashcode for this instance.
    /// </summary>
    /// <returns>Hashcode of this instance.</returns>
    public override int GetHashCode()
    {
        return _tiles.Length == 2
            ? Tuple.Create(_tiles[0], _tiles[1]).GetHashCode()
            : _tiles.Length == 4
                ? Tuple.Create(_tiles[0], _tiles[1], _tiles[2], _tiles[3]).GetHashCode()
                : Tuple.Create(_tiles[0], _tiles[1], _tiles[2]).GetHashCode();
    }

    /// <summary>
    /// Overriden; checks the equality between this instance and any other object.
    /// If <paramref name="obj"/> is a <see cref="TileComboPivot"/>, see <see cref="Equals(TileComboPivot)"/>.
    /// </summary>
    /// <param name="obj">Any <see cref="object"/>.</param>
    /// <returns><c>True</c> if both instances are equal; <c>False</c> otherwise.</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as TileComboPivot);
    }

    #endregion Interfaces implementation and overrides from base

    #region Private methods

    // Gets the tile corresponding to the specified wind in the purpose to create a sorted list for display.
    private (TilePivot, bool) GetTileForSortedListAtSpecifiedWind(Winds wind, IReadOnlyList<TilePivot> concealedOnly, ref int i)
    {
        if (StolenFrom.HasValue && wind == StolenFrom.Value)
        {
            return (OpenTile!, true);
        }
        else
        {
            i++;
            return (concealedOnly[i - 1], false);
        }
    }

    #endregion Private methods

    #region Static methods

    /// <summary>
    /// Builds a pair from the specified tile.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>The pair.</returns>
    internal static TileComboPivot BuildPair(TilePivot tile)
    {
        return Build(tile, 2);
    }

    /// <summary>
    /// Builds a brelan from the specified tile.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>The brelan.</returns>
    internal static TileComboPivot BuildBrelan(TilePivot tile)
    {
        return Build(tile, 3);
    }

    /// <summary>
    /// Builds a square from the specified tile.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <returns>The square.</returns>
    internal static TileComboPivot BuildSquare(TilePivot tile)
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
    /// Checks if the specified tile index must be displayed as concealed (aka tiles 2 and 3 from a square).
    /// </summary>
    /// <param name="i">The tile index.</param>
    /// <returns><c>True</c> if concealed display; <c>False</c> otherwise.</returns>
    public bool IsConcealedDisplay(int i)
    {
        return IsSquare && IsConcealed && i > 0 && i < 3;
    }

    /// <summary>
    /// Gets the list of tiles from the combination, sorted by wind logic for display.
    /// </summary>
    /// <param name="ownerWind">The current wind of the owner.</param>
    /// <returns>List of tiles tuple; the second item is <c>True</c> when the tile is the opened one.</returns>
    public IReadOnlyList<(TilePivot tile, bool stolen)> GetSortedTilesForDisplay(Winds ownerWind)
    {
        if (!StolenFrom.HasValue)
        {
            return Tiles.Select(t => (t, false)).ToList();
        }

        var concealedOnly = new List<TilePivot>(_tiles);
        concealedOnly.Remove(OpenTile!);

        var i = 0;

        var tiles = new List<(TilePivot, bool)>
        {
            GetTileForSortedListAtSpecifiedWind(ownerWind.Left(), concealedOnly, ref i),
            GetTileForSortedListAtSpecifiedWind(ownerWind.Opposite(), concealedOnly, ref i)
        };

        // For a square, the third tile is never from an opponent.
        if (IsSquare)
        {
            tiles.Add((concealedOnly[i], false));
            i++;
        }

        tiles.Add(GetTileForSortedListAtSpecifiedWind(ownerWind.Right(), concealedOnly, ref i));

        return tiles;
    }

    #endregion Public methods
}
