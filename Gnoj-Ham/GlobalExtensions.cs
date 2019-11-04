using System;
using System.Collections.Generic;

namespace Gnoj_Ham
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class GlobalExtensions
    {
        /// <summary>
        /// Extension; adds an item to the collection a specified number of times.
        /// </summary>
        /// <typeparam name="T">The collection targeted type.</typeparam>
        /// <param name="sourceList">The source list.</param>
        /// <param name="newElement">The element to add.</param>
        /// <param name="count">Adding count.</param>
        public static void Add<T>(this List<T> sourceList, T newElement, int count)
        {
            if (count > 0 && sourceList != null)
            {
                for (int i = 0; i < count; i++)
                {
                    sourceList.Add(newElement);
                }
            }
        }

        /// <summary>
        /// Gets the number of points from the specified <see cref="InitialPointsRulePivot"/> value.
        /// </summary>
        /// <param name="initialPointsRule">The <see cref="InitialPointsRulePivot"/> value.</param>
        /// <returns>The number of points.</returns>
        public static int GetInitialPointsFromRule(this InitialPointsRulePivot initialPointsRule)
        {
            switch (initialPointsRule)
            {
                case InitialPointsRulePivot.K25:
                    return 25000;
                case InitialPointsRulePivot.K30:
                    return 30000;
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
