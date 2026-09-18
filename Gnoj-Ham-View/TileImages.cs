using System.IO;
using System.Windows.Media.Imaging;

namespace Gnoj_Ham_View;

/// <summary>
/// Loads tile images from the resources, once each: a tile image never changes, and a hand shows the
/// same few dozen over and over.
/// </summary>
internal static class TileImages
{
    private static readonly Dictionary<string, BitmapImage> Cache = new();

    /// <summary>
    /// Gets the image of a resource.
    /// </summary>
    /// <param name="resourceName">The image resource name.</param>
    /// <returns>The (frozen) image.</returns>
    internal static BitmapImage Get(string resourceName)
    {
        if (!Cache.TryGetValue(resourceName, out var image))
        {
            var bytes = (byte[])Properties.Resources.ResourceManager.GetObject(resourceName)!;

            image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(bytes);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();

            Cache[resourceName] = image;
        }

        return image;
    }
}
