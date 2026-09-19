namespace Gnoj_Ham_ViewModel;

/// <summary>
/// A combination a player has declared (pon, chii or kan), laid out in display order.
/// </summary>
/// <param name="Tiles">The combination's tiles, in display order.</param>
public sealed record CombinationViewModel(IReadOnlyList<TileViewModel> Tiles);
