using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Gnoj_Ham_ViewModel;

namespace Gnoj_Ham_View;

/// <summary>
/// Displays a <see cref="TileViewModel"/>: a button holding the tile image, driven by data binding so
/// it can be used in a data template.
/// </summary>
public sealed class TileView : Button
{
    // A tile, standing up, in device independent pixels.
    private const int TileWidth = 45;
    private const int TileHeight = 60;

    private const double HighlightedImageOpacity = 0.8;

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
    /// Indicates if the tile stands out from the others.
    /// </summary>
    public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
        nameof(IsHighlighted), typeof(bool), typeof(TileView), new PropertyMetadata(false, OnDisplayChanged));

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

    /// <summary>
    /// Indicates if the tile stands out from the others.
    /// </summary>
    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
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

        Height = (tile.IsSideways ? TileWidth : TileHeight) * Rate;
        Width = (tile.IsSideways ? TileHeight : TileWidth) * Rate;

        Content = new Image
        {
            Source = TileImages.Get(tile.ImageResourceName),
            LayoutTransform = new RotateTransform(tile.AngleDegrees),
            Opacity = IsHighlighted ? HighlightedImageOpacity : 1
        };

        if (IsHighlighted)
        {
            Background = Brushes.DarkMagenta;
        }
        else
        {
            ClearValue(BackgroundProperty);
        }

        ToolTip = tile.ToolTip;
    }
}
