using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gnoj_Ham
{
    /// <summary>
    /// Extension methods.
    /// </summary>
    public static class GlobalExtensions
    {
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
        /// <remarks>Keep the type 'List' here.</remarks>
        public static List<List<T>> CartesianProduct<T>(this List<List<T>> firstList, List<List<T>> secondList)
        {
            return firstList == null || secondList == null
                ? new List<List<T>>()
                : firstList.SelectMany(elem1 => secondList, (elem1, elem2) =>
                                        {
                                            var elemsJoin = new List<T>(elem1);
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
        public static bool IsBijection<T>(this IReadOnlyList<T> list1, IReadOnlyList<T> list2) where T : IEquatable<T>
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

        /// <summary>
        /// Extension; gets the wind to the left of the specified wind.
        /// </summary>
        /// <param name="origin">The wind.</param>
        /// <returns>The left wind.</returns>
        public static WindPivot Left(this WindPivot origin)
        {
            switch (origin)
            {
                case WindPivot.East:
                    return WindPivot.North;
                case WindPivot.South:
                    return WindPivot.East;
                case WindPivot.West:
                    return WindPivot.South;
                default:
                    return WindPivot.West;
            }
        }

        /// <summary>
        /// Extension; gets the wind to the right of the specified wind.
        /// </summary>
        /// <param name="origin">The wind.</param>
        /// <returns>The right wind.</returns>
        public static WindPivot Right(this WindPivot origin)
        {
            switch (origin)
            {
                case WindPivot.East:
                    return WindPivot.South;
                case WindPivot.South:
                    return WindPivot.West;
                case WindPivot.West:
                    return WindPivot.North;
                default:
                    return WindPivot.East;
            }
        }

        /// <summary>
        /// Extension; gets the wind to the opposite of the specified wind.
        /// </summary>
        /// <param name="origin">The wind.</param>
        /// <returns>The opposite wind.</returns>
        public static WindPivot Opposite(this WindPivot origin)
        {
            switch (origin)
            {
                case WindPivot.East:
                    return WindPivot.West;
                case WindPivot.South:
                    return WindPivot.North;
                case WindPivot.West:
                    return WindPivot.East;
                default:
                    return WindPivot.South;
            }
        }

        /// <summary>
        /// Extension; computes the N-index player after (or before) the specified player index.
        /// </summary>
        /// <param name="playerIndex">The player index.</param>
        /// <param name="nIndex">The N value.</param>
        /// <returns>The relative player index.</returns>
        public static int RelativePlayerIndex(this int playerIndex, int nIndex)
        {
            if (nIndex == 0)
            {
                return playerIndex;
            }

            var nIndexMod = nIndex % 4;
            var newIndex = playerIndex + nIndexMod;

            if (nIndex > 0 && newIndex > 3)
            {
                newIndex %= 4;
            }
            else if (nIndex < 0 && newIndex < 0)
            {
                newIndex = 4 - Math.Abs(newIndex % 4);
            }

            return newIndex;
        }

        /// <summary>
        /// Extension; checks if a <see cref="EndOfGameRulePivot"/> applies "Tobi" or not.
        /// </summary>
        /// <param name="endOfGameRule">The <see cref="EndOfGameRulePivot"/> to check.</param>
        /// <returns><c>True</c> if applies rule; <c>False</c> otherwise.</returns>
        internal static bool TobiRuleApply(this EndOfGameRulePivot endOfGameRule)
        {
            return endOfGameRule == EndOfGameRulePivot.Tobi || endOfGameRule == EndOfGameRulePivot.EnchousenAndTobi;
        }

        /// <summary>
        /// Extension; checks if a <see cref="EndOfGameRulePivot"/> applies "Enchousen" or not.
        /// </summary>
        /// <param name="endOfGameRule">The <see cref="EndOfGameRulePivot"/> to check.</param>
        /// <returns><c>True</c> if applies rule; <c>False</c> otherwise.</returns>
        internal static bool EnchousenRuleApply(this EndOfGameRulePivot endOfGameRule)
        {
            return endOfGameRule == EndOfGameRulePivot.Enchousen || endOfGameRule == EndOfGameRulePivot.EnchousenAndTobi;
        }
    }
}
