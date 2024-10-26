using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_Library;

/// <summary>
/// Extension methods.
/// </summary>
public static class GlobalExtensions
{
    /// <summary>
    /// Extension; gets the number of points from the specified <see cref="InitialPointsRules"/> value.
    /// </summary>
    /// <param name="initialPointsRule">The <see cref="InitialPointsRules"/> value.</param>
    /// <returns>The number of points.</returns>
    /// <exception cref="NotImplementedException">The rule is not implemented.</exception>
    internal static int GetInitialPointsFromRule(this InitialPointsRules initialPointsRule)
    {
        return initialPointsRule switch
        {
            InitialPointsRules.K25 => 25000,
            InitialPointsRules.K30 => 30000,
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    /// Extension; checks if a list is a bijection of another list.
    /// </summary>
    /// <typeparam name="T">The underlying type in both lists; must implement <see cref="IEquatable{T}"/>.</typeparam>
    /// <param name="list1">The first list.</param>
    /// <param name="list2">The second list.</param>
    /// <returns><c>True</c> if <paramref name="list1"/> is a bijection of <paramref name="list2"/>; <c>False</c> otherwise.</returns>
    internal static bool IsBijection<T>(this IReadOnlyList<T>? list1, IReadOnlyList<T>? list2) where T : IEquatable<T>
    {
        return list1 != null && list2 != null
            && list1.All(e1 => list2.Contains(e1))
            && list2.All(e2 => list1.Contains(e2));
    }

    /// <summary>
    /// Extension; checks if the specified <see cref="DrawTypes"/> is a self draw.
    /// </summary>
    /// <param name="drawType">The <see cref="DrawTypes"/>.</param>
    /// <returns><c>True</c> if self draw; <c>False</c> otherwise.</returns>
    internal static bool IsSelfDraw(this DrawTypes drawType)
    {
        return drawType == DrawTypes.Wall || drawType == DrawTypes.Compensation;
    }

    /// <summary>
    /// Extension; gets the wind to the left of the specified wind.
    /// </summary>
    /// <param name="origin">The wind.</param>
    /// <returns>The left wind.</returns>
    internal static Winds Left(this Winds origin)
    {
        return origin switch
        {
            Winds.East => Winds.North,
            Winds.South => Winds.East,
            Winds.West => Winds.South,
            _ => Winds.West,
        };
    }

    /// <summary>
    /// Extension; gets the wind to the right of the specified wind.
    /// </summary>
    /// <param name="origin">The wind.</param>
    /// <returns>The right wind.</returns>
    internal static Winds Right(this Winds origin)
    {
        return origin switch
        {
            Winds.East => Winds.South,
            Winds.South => Winds.West,
            Winds.West => Winds.North,
            _ => Winds.East,
        };
    }

    /// <summary>
    /// Extension; gets the wind to the opposite of the specified wind.
    /// </summary>
    /// <param name="origin">The wind.</param>
    /// <returns>The opposite wind.</returns>
    internal static Winds Opposite(this Winds origin)
    {
        return origin switch
        {
            Winds.East => Winds.West,
            Winds.South => Winds.North,
            Winds.West => Winds.East,
            _ => Winds.South,
        };
    }

    /// <summary>
    /// Extension; computes the N-index player after (or before) the specified player index.
    /// </summary>
    /// <param name="playerIndex">The player index.</param>
    /// <param name="nIndex">The N value.</param>
    /// <returns>The relative player index.</returns>
    public static PlayerIndices RelativePlayerIndex(this PlayerIndices playerIndex, int nIndex)
    {
        if (nIndex == 0)
        {
            return playerIndex;
        }

        var nIndexMod = nIndex % 4;
        var newIndex = (int)playerIndex + nIndexMod;

        if (nIndex > 0 && newIndex > 3)
        {
            newIndex %= 4;
        }
        else if (nIndex < 0 && newIndex < 0)
        {
            newIndex = 4 - Math.Abs(newIndex % 4);
        }

        return (PlayerIndices)newIndex;
    }

    /// <summary>
    /// Extension; checks if a <see cref="EndOfGameRules"/> applies "Tobi" or not.
    /// </summary>
    /// <param name="endOfGameRule">The <see cref="EndOfGameRules"/> to check.</param>
    /// <returns><c>True</c> if applies rule; <c>False</c> otherwise.</returns>
    internal static bool TobiRuleApply(this EndOfGameRules endOfGameRule)
    {
        return endOfGameRule == EndOfGameRules.Tobi || endOfGameRule == EndOfGameRules.EnchousenAndTobi;
    }

    /// <summary>
    /// Extension; checks if a <see cref="EndOfGameRules"/> applies "Enchousen" or not.
    /// </summary>
    /// <param name="endOfGameRule">The <see cref="EndOfGameRules"/> to check.</param>
    /// <returns><c>True</c> if applies rule; <c>False</c> otherwise.</returns>
    internal static bool EnchousenRuleApply(this EndOfGameRules endOfGameRule)
    {
        return endOfGameRule == EndOfGameRules.Enchousen || endOfGameRule == EndOfGameRules.EnchousenAndTobi;
    }

    /// <summary>
    /// Adds an element in a list while respecting ascending order.
    /// </summary>
    /// <typeparam name="T">targeted items type.</typeparam>
    /// <param name="list">The source list.</param>
    /// <param name="item">The item.</param>
    internal static void AddSorted<T>(this List<T> list, T item) where T : IComparable<T>
    {
        if (list.Count == 0)
        {
            list.Add(item);
        }
        else if (list[^1].CompareTo(item) <= 0)
        {
            list.Add(item);
        }
        else if (list[0].CompareTo(item) >= 0)
        {
            list.Insert(0, item);
        }
        else
        {
            var index = list.BinarySearch(item);
            if (index < 0)
                index = ~index;
            list.Insert(index, item);
        }
    }
}
