using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;

namespace Gnoj_HamView
{
    /// <summary>
    /// Global tools.
    /// </summary>
    public static class GlobalTools
    {
        /// <summary>
        /// Extension; transfoms a <see cref="Bitmap"/> to a <see cref="BitmapImage"/>.
        /// </summary>
        /// <param name="bitmap">The <see cref="Bitmap"/> to transform.</param>
        /// <returns>The converted <see cref="BitmapImage"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="bitmap"/> is <c>Null</c>.</exception>
        public static BitmapImage ToBitmapImage(this Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }
    }
}
