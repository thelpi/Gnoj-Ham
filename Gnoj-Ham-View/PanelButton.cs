namespace Gnoj_Ham_View;

/// <summary>
/// Represents a button inside a panel.
/// Used for human player only, so the player index of the panel is always <see cref="Gnoj_Ham.GamePivot.HUMAN_INDEX"/>.
/// Alternatively, can be used to target a specified button, not inside a panel.
/// </summary>
internal class PanelButton
{
    /// <summary>
    /// The panel name without the player index.
    /// Alternatively, the full name of the button.
    /// </summary>
    internal string PanelBaseName { get; private set; }
    /// <summary>
    /// The index of the button inside the panel.
    /// <c>-1</c> to target a specified button.
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
