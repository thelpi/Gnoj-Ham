using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;
using Gnoj_Ham;

namespace Gnoj_HamView.Converters
{
    internal class TileToBitmapConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var tile = (TilePivot)value;

            var tileBitmap = Properties.Resources.ResourceManager.GetObject(tile.ToResourceName()) as Bitmap;

            return tileBitmap.ToBitmapImage();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
