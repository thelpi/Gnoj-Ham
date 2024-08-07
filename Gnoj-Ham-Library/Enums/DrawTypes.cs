﻿namespace Gnoj_Ham_Library.Enums;

/// <summary>
/// Enumeration of draw types.
/// </summary>
internal enum DrawTypes
{
    /// <summary>
    /// From wall.
    /// </summary>
    Wall,
    /// <summary>
    /// From compensation tile after calling a kan.
    /// </summary>
    Compensation,
    /// <summary>
    /// From opponent's discard.
    /// </summary>
    OpponentDiscard,
    /// <summary>
    /// From opponent's fourth tile while calling concealed kan.
    /// </summary>
    OpponentKanCallConcealed,
    /// <summary>
    /// From opponent's fourth tile while calling opened kan.
    /// </summary>
    OpponentKanCallOpen
}
