using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Extension; gets the number of points from the specified <see cref="InitialPointsRulePivot"/> value.
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

        /// <summary>
        /// Extension; generates the cartesian product of two lists.
        /// </summary>
        /// <typeparam name="T">The underlying type of both list.</typeparam>
        /// <param name="firstList">The first list.</param>
        /// <param name="secondList">The second list.</param>
        /// <returns>The cartesian product; empty list if at least one argument is <c>Null</c>.</returns>
        public static List<List<T>> CartesianProduct<T>(this List<List<T>> firstList, List<List<T>> secondList)
        {
            if (firstList == null || secondList == null)
            {
                return new List<List<T>>();
            }

            return firstList.SelectMany(elem1 => secondList, (elem1, elem2) =>
                                        {
                                            List<T> elemsJoin = new List<T>(elem1);
                                            elemsJoin.AddRange(elem2);
                                            return elemsJoin;
                                        }).ToList();
        }

        /// <summary>
        /// Extension; checks if a list is a bijection of another list.
        /// </summary>
        /// <typeparam name="T">The underlying type in both lists; must implement <see cref="IEquatable{T}"/>.</typeparam>
        /// <param name="list1">The first list.</param>
        /// <param name="list2">The second list.</param>
        /// <returns><c>True</c> if <paramref name="list1"/> is a bijection of <paramref name="list2"/>; <c>False</c> otherwise.</returns>
        public static bool IsBijection<T>(this IEnumerable<T> list1, IEnumerable<T> list2) where T : IEquatable<T>
        {
            return list1 != null && list2 != null
                && list1.All(e1 => list2.Contains(e1))
                && list2.All(e2 => list1.Contains(e2));
        }

        /// <summary>
        /// Extension; checks if the specified <see cref="DrawTypePivot"/> is a self draw.
        /// </summary>
        /// <param name="drawType">The <see cref="DrawTypePivot"/>.</param>
        /// <returns><c>True</c> if self draw; <c>False</c> otherwise.</returns>
        public static bool IsSelfDraw(this DrawTypePivot drawType)
        {
            return drawType == DrawTypePivot.Wall || drawType == DrawTypePivot.Compensation;
        }
    }
}
