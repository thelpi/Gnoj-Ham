namespace Gnoj_HamView
{
    /// <summary>
    /// Represents a button inside a panel.
    /// Used for human player only, so the plaeyr index is always <see cref="Gnoj_Ham.GamePivot.HUMAN_INDEX"/>.
    /// </summary>
    internal class PanelButton
    {
        /// <summary>
        /// The panel name without the player index.
        /// </summary>
        internal string PanelBaseName { get; private set; }
        /// <summary>
        /// The index of the button inside the panel.
        /// </summary>
        internal int ChildrenButtonIndex { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="panelBaseName">The <see cref="PanelBaseName"/> value.</param>
        /// <param name="childrenButtonIndex">The <see cref="ChildrenButtonIndex"/> value.</param>
        internal PanelButton(string panelBaseName, int childrenButtonIndex)
        {
            PanelBaseName = panelBaseName;
            ChildrenButtonIndex = childrenButtonIndex;
        }
    }
}
