using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_View;

/// <summary>
/// Displays a <see cref="TileViewModel"/>: same look as <see cref="TileButton"/> (a button holding the
/// tile image), but driven by data binding instead of constructor arguments, so it can be used in a
/// data template.
/// </summary>
public sealed class TileView : Button
{
    /// <summary>
    /// The tile to display.
    /// </summary>
    public static readonly DependencyProperty TileProperty = DependencyProperty.Register(
        nameof(Tile), typeof(TileViewModel), typeof(TileView), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>
    /// The scale applied to the tile's default size.
    /// </summary>
    public static readonly DependencyProperty RateProperty = DependencyProperty.Register(
        nameof(Rate), typeof(double), typeof(TileView), new PropertyMetadata(1.0, OnDisplayChanged));

    /// <summary>
    /// The tile to display.
    /// </summary>
    public TileViewModel? Tile
    {
        get => (TileViewModel?)GetValue(TileProperty);
        set => SetValue(TileProperty, value);
    }

    /// <summary>
    /// The scale applied to the tile's default size.
    /// </summary>
    public double Rate
    {
        get => (double)GetValue(RateProperty);
        set => SetValue(RateProperty, value);
    }

    private static void OnDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((TileView)d).Refresh();

    private void Refresh()
    {
        var tile = Tile;
        if (tile == null)
        {
            Content = null;
            ToolTip = null;
            return;
        }

        Height = (tile.IsSideways ? TileButton.TILE_WIDTH : TileButton.TILE_HEIGHT) * Rate;
        Width = (tile.IsSideways ? TileButton.TILE_HEIGHT : TileButton.TILE_WIDTH) * Rate;

        Content = new Image
        {
            Source = TileImages.Get(tile.ImageResourceName),
            LayoutTransform = new RotateTransform(tile.AngleDegrees)
        };

        ToolTip = tile.ToolTip;
    }
}
