using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Gnoj_Ham_Library;

namespace Gnoj_Ham_View;

/// <summary>
/// Logique d'interaction pour TileButton.xaml
/// </summary>
public partial class TileButton : Button
{
    private const string CONCEALED_TILE_RSC_NAME = "concealed";

    internal const int TILE_WIDTH = 45;
    internal const int TILE_HEIGHT = 60;
    internal const int DEFAULT_TILE_MARGIN = 10;

    public TilePivot? Tile { get; }

    public TileButton(TilePivot? tile,
        RoutedEventHandler? handler = null,
        AnglePivot angle = AnglePivot.A0,
        bool concealed = false,
        double rate = 1)
    {
        InitializeComponent();

        Tile = tile;

        var rscName = concealed ? CONCEALED_TILE_RSC_NAME : tile!.ToResourceName();

        var tileBitmap = Properties.Resources.ResourceManager.GetObject(rscName) as byte[];

        Height = angle == AnglePivot.A0 || angle == AnglePivot.A180 ? (TILE_HEIGHT * rate) : (TILE_WIDTH * rate);
        Width = angle == AnglePivot.A0 || angle == AnglePivot.A180 ? (TILE_WIDTH * rate) : (TILE_HEIGHT * rate);

        Content = new Image
        {
            Source = ToBitmapImage(tileBitmap!),
            LayoutTransform = new RotateTransform(Convert.ToDouble(angle.ToString().Replace("A", string.Empty)))
        };

        ToolTip = concealed ? null : tile!.TileDisplay();

        if (handler != null)
        {
            Click += handler;
        }
    }

    private static BitmapImage ToBitmapImage(byte[] bitmap)
    {
        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = new MemoryStream(bitmap);
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }
}
