using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Gnoj_Ham;

namespace Gnoj_HamView.Converters
{
    /// <summary>
    /// Conversion of <see cref="EndOfGameRulePivot"/> for display.
    /// </summary>
    /// <seealso cref="IValueConverter"/>
    public class EndOfGameRuleConverter : IValueConverter
    {
        /// <summary>
        /// Transforms every elements of the enum <see cref="EndOfGameRulePivot"/> to its <see cref="string"/> representation.
        /// </summary>
        /// <param name="value">Ignorable.</param>
        /// <param name="targetType">Ignorable.</param>
        /// <param name="parameter">Ignorable.</param>
        /// <param name="culture">Ignorable.</param>
        /// <returns>A list of <see cref="string"/> representations.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var results = new List<string>();

            foreach (EndOfGameRulePivot rule in Enum.GetValues(typeof(EndOfGameRulePivot)).OfType<EndOfGameRulePivot>())
            {
                switch (rule)
                {
                    case EndOfGameRulePivot.Enchousen:
                        results.Add("Enchousen");
                        break;
                    case EndOfGameRulePivot.EnchousenAndTobi:
                        results.Add("Enchousen & Tobi");
                        break;
                    case EndOfGameRulePivot.Oorasu:
                        results.Add("Oorasu");
                        break;
                    case EndOfGameRulePivot.Tobi:
                        results.Add("Tobi");
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
