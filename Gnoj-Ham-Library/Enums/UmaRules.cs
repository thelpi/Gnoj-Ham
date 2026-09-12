namespace Gnoj_Ham_Library.Enums;

/// <summary>
/// Enumeration of rules for the "uma" (rank bonus/malus applied to the final score).
/// </summary>
public enum UmaRules
{
    /// <summary>
    /// 1st: +10,000 ; 2nd: +5,000 ; 3rd: -5,000 ; 4th: -10,000.
    /// Most common casual/online convention (e.g. Tenhou's general room).
    /// </summary>
    FiveTen,
    /// <summary>
    /// 1st: +20,000 ; 2nd: +10,000 ; 3rd: -10,000 ; 4th: -20,000.
    /// Common alternative, notably used in tournaments.
    /// </summary>
    TenTwenty,
    /// <summary>
    /// 1st: +15,000 ; 2nd: +5,000 ; 3rd: -5,000 ; 4th: -15,000.
    /// European Mahjong Association competition rules (revised 2016 edition).
    /// </summary>
    Ema
}
