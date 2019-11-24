using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Gnoj_HamView.Converters
{
    /// <summary>
    /// Conversion of <see cref="ChronoPivot"/> for display.
    /// </summary>
    /// <seealso cref="IValueConverter"/>
    public class ChronoConverter : IValueConverter
    {
        /// <summary>
        /// Transforms every elements of the enum <see cref="ChronoPivot"/> to its <see cref="string"/> representation.
        /// </summary>
        /// <param name="value">Ignorable.</param>
        /// <param name="targetType">Ignorable.</param>
        /// <param name="parameter">Ignorable.</param>
        /// <param name="culture">Ignorable.</param>
        /// <returns>A list of <see cref="string"/> representations.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var results = new List<string>();

            foreach (ChronoPivot ch in Enum.GetValues(typeof(ChronoPivot)).OfType<ChronoPivot>())
            {
                switch (ch)
                {
                    case ChronoPivot.None:
                        results.Add("None");
                        break;
                    case ChronoPivot.Short:
                    case ChronoPivot.Long:
                        results.Add($"{ch.ToString()} ({ch.GetDelay()} sec)");
                        break;
                }
            }

            return results;
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
