using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Gnoj_HamView.Converters
{
    /// <summary>
    /// Conversion of <see cref="CpuSpeed"/> for display.
    /// </summary>
    /// <seealso cref="IValueConverter"/>
    public class CpuSpeedConverter : IValueConverter
    {
        /// <summary>
        /// Transforms every elements of the enum <see cref="CpuSpeed"/> to its <see cref="string"/> representation.
        /// </summary>
        /// <param name="value">Ignorable.</param>
        /// <param name="targetType">Ignorable.</param>
        /// <param name="parameter">Ignorable.</param>
        /// <param name="culture">Ignorable.</param>
        /// <returns>A list of <see cref="string"/> representations.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Enum.GetValues(typeof(CpuSpeed)).OfType<CpuSpeed>().Select(v =>
            {
                int intParsedValue = System.Convert.ToInt32(v.ToString().Replace("S", string.Empty));

                if (intParsedValue >= 1000)
                {
                    return $"{intParsedValue / 1000} sec";
                }
                else
                {
                    return $"{intParsedValue} ms";
                }
            });
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="value">Any.</param>
        /// <param name="targetType">Any.</param>
        /// <param name="parameter">Any.</param>
        /// <param name="culture">Any.</param>
        /// <returns>None.</returns>
        /// <exception cref="NotImplementedException">Not implemented.</exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
