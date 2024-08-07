﻿using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Represents the context when a "ron" or a "tsumo" is called.
/// </summary>
internal class WinContextPivot
{
    #region Embedded properties

    /// <summary>
    /// The latest tile (from self-draw or not).
    /// </summary>
    internal TilePivot? LatestTile { get; }
    /// <summary>
    /// <c>True</c> if <see cref="LatestTile"/> was the last tile of the round (from wall or opponent discard).
    /// </summary>
    internal bool IsRoundLastTile { get; }
    /// <summary>
    /// <c>True</c> if the player has called riichi.
    /// </summary>
    internal bool IsRiichi { get; }
    /// <summary>
    /// <c>True</c> if the player has called riichi on the first turn.
    /// </summary>
    internal bool IsFirstTurnRiichi { get; }
    /// <summary>
    /// <c>True</c> if it's the first turn after calling riichi.
    /// </summary>
    internal bool IsIppatsu { get; }
    /// <summary>
    /// The current dominant wind.
    /// </summary>
    internal Winds DominantWind { get; }
    /// <summary>
    /// The current player wind.
    /// </summary>
    internal Winds PlayerWind { get; }
    /// <summary>
    /// <c>True</c> if it's first turn draw (without call made).
    /// </summary>
    internal bool IsFirstTurnDraw { get; }
    /// <summary>
    /// Draw type for <see cref="LatestTile"/>.
    /// </summary>
    internal DrawTypes DrawType { get; }
    /// <summary>
    /// <c>True</c> if nagashi mangan; <c>False</c> otherwise.
    /// </summary>
    internal bool IsNagashiMangan { get; }

    #endregion Embedded properties

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="latestTile">The <see cref="LatestTile"/> value.</param>
    /// <param name="drawType">The <see cref="DrawType"/> value.</param>
    /// <param name="dominantWind">The <see cref="DominantWind"/> value.</param>
    /// <param name="playerWind">The <see cref="PlayerWind"/> value.</param>
    /// <param name="isFirstOrLast">Optionnal; indicates a win at the first turn without any call made (<c>True</c>) or at the last tile of the round (<c>Null</c>); default value is <c>False</c>.</param>
    /// <param name="isRiichi">Optionnal; indicates if riichi (<c>True</c>) or riichi at first turn without any call made (<c>Null</c>); default value is <c>False</c>.</param>
    /// <param name="isIppatsu">Optionnal; indicates if it's a win by ippatsu (<paramref name="isRiichi"/> can't be <c>False</c> in such case); default value is <c>False</c>.</param>
    internal WinContextPivot(TilePivot latestTile, DrawTypes drawType, Winds dominantWind, Winds playerWind,
        bool? isFirstOrLast = false, bool? isRiichi = false, bool isIppatsu = false)
    {
        LatestTile = latestTile;
        IsRoundLastTile = isFirstOrLast == null;
        IsRiichi = isRiichi != false;
        IsFirstTurnRiichi = isRiichi == null;
        IsIppatsu = isIppatsu;
        DominantWind = dominantWind;
        PlayerWind = playerWind;
        IsFirstTurnDraw = isFirstOrLast == true;
        DrawType = drawType;
        IsNagashiMangan = false;
    }

    /// <summary>
    /// Empty constructor. To use when <see cref="IsNagashiMangan"/> is <c>True</c>.
    /// Every other properties are at their default value.
    /// </summary>
    internal WinContextPivot()
    {
        IsNagashiMangan = true;
    }

    #endregion Constructors

    #region Public methods

    /// <summary>
    /// Checks if the context gives the yaku <see cref="YakuPivot.Tenhou"/>.
    /// </summary>
    /// <returns><c>True</c> if it gives the yaku; <c>False</c> otherwise.</returns>
    internal bool IsTenhou()
    {
        return IsFirstTurnDraw && PlayerWind == Winds.East && DrawType.IsSelfDraw();
    }

    /// <summary>
    /// Checks if the context gives the yaku <see cref="YakuPivot.Chiihou"/>.
    /// </summary>
    /// <returns><c>True</c> if it gives the yaku; <c>False</c> otherwise.</returns>
    internal bool IsChiihou()
    {
        return IsFirstTurnDraw && PlayerWind != Winds.East && DrawType.IsSelfDraw();
    }

    /// <summary>
    /// Checks if the context gives the yaku <see cref="YakuPivot.Renhou"/>.
    /// </summary>
    /// <returns><c>True</c> if it gives the yaku; <c>False</c> otherwise.</returns>
    internal bool IsRenhou()
    {
        return IsFirstTurnDraw && !DrawType.IsSelfDraw();
    }

    #endregion Public methods
}
