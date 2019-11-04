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
    }
}
