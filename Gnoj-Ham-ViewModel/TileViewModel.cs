using Gnoj_Ham_Library;

namespace Gnoj_Ham_ViewModel;

/// <summary>
/// What a tile looks like on screen: which tile, how it's turned, whether it's face down. The image
/// itself is the view's business - only its resource name is exposed here.
/// </summary>
public sealed class TileViewModel
{
    /// <summary>
    /// Image resource name of a face-down tile.
    /// </summary>
    public const string ConcealedImageResourceName = "concealed";

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="tile">The tile.</param>
    /// <param name="angle">The tile rotation.</param>
    /// <param name="isConcealed"><c>True</c> to show the tile face down.</param>
    /// <param name="isApart"><c>True</c> to set the tile slightly apart from the previous one (e.g. the winning tile).</param>
    public TileViewModel(TilePivot tile, AnglePivot angle = AnglePivot.A0, bool isConcealed = false, bool isApart = false)
    {
        Tile = tile;
        Angle = angle;
        IsConcealed = isConcealed;
        IsApart = isApart;
    }

    /// <summary>
    /// The tile.
    /// </summary>
    public TilePivot Tile { get; }

    /// <summary>
    /// The tile rotation.
    /// </summary>
    public AnglePivot Angle { get; }

    /// <summary>
    /// Indicates if the tile is shown face down.
    /// </summary>
    public bool IsConcealed { get; }

    /// <summary>
    /// Indicates if the tile is set slightly apart from the previous one.
    /// </summary>
    public bool IsApart { get; }

    /// <summary>
    /// Inferred; the image resource to display.
    /// </summary>
    public string ImageResourceName => IsConcealed ? ConcealedImageResourceName : Tile.ToResourceName();

    /// <summary>
    /// Inferred; the tooltip text, or <c>Null</c> for a face-down tile (which reveals nothing).
    /// </summary>
    public string? ToolTip => IsConcealed ? null : Tile.TileDisplay();

    /// <summary>
    /// Inferred; the rotation in degrees.
    /// </summary>
    public int AngleDegrees => Angle switch
    {
        AnglePivot.A0 => 0,
        AnglePivot.A90 => 90,
        AnglePivot.A180 => 180,
        _ => 270,
    };

    /// <summary>
    /// Inferred; indicates if the tile lies on its side (its width and height are swapped).
    /// </summary>
    public bool IsSideways => Angle == AnglePivot.A90 || Angle == AnglePivot.A270;
}
