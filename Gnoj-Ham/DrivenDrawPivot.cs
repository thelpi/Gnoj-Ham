using System.Collections.Generic;
using System.Linq;

namespace Gnoj_Ham
{
    /// <summary>
    /// Debug tools to transform a random draw of tiles into a specifc draw.
    /// </summary>
    internal static class DrivenDrawPivot
    {
        /// <summary>
        /// Transforms the specified draw to make the human player tenpai.
        /// </summary>
        /// <param name="_fullTilesList">The draw.</param>
        internal static void HumanTenpai(List<TilePivot> _fullTilesList)
        {
            var keep = new List<TilePivot>
            {
                _fullTilesList.First(t => t.Dragon == DragonPivot.Red),
                _fullTilesList.Last(t => t.Dragon == DragonPivot.Red),
                _fullTilesList.First(t => t.Dragon == DragonPivot.Green),
                _fullTilesList.Last(t => t.Dragon == DragonPivot.Green),
                _fullTilesList.First(t => t.Dragon == DragonPivot.White),
                _fullTilesList.Last(t => t.Dragon == DragonPivot.White),
                _fullTilesList.First(t => t.Wind == WindPivot.East),
                _fullTilesList.Last(t => t.Wind == WindPivot.East),
                _fullTilesList.First(t => t.Wind == WindPivot.South),
                _fullTilesList.Last(t => t.Wind == WindPivot.South),
                _fullTilesList.First(t => t.Wind == WindPivot.West),
                _fullTilesList.Last(t => t.Wind == WindPivot.West),
                _fullTilesList.First(t => t.Wind == WindPivot.North)
            };
            _fullTilesList.RemoveAll(t => keep.Any(k => ReferenceEquals(k, t)));
            for (int i = 0; i < keep.Count; i++)
            {
                _fullTilesList.Insert(i, keep[i]);
            }
        }
    }
}
