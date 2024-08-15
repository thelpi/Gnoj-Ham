using System.Windows.Controls;
using Gnoj_Ham_Library.Enums;

namespace Gnoj_Ham_View;

/// <summary>
/// Logique d'interaction pour CallButton.xaml
/// </summary>
public partial class CallButton : Button
{
    public PlayerIndices PlayerIndex { get; private set; }

    public new System.Windows.Visibility Visibility { get; private set; }

    public CallButton()
    {
        InitializeComponent();
    }

    public void MakeVisible(PlayerIndices playerIndex)
    {
        Visibility = System.Windows.Visibility.Visible;
        base.Visibility = System.Windows.Visibility.Visible;
        PlayerIndex = playerIndex;
    }

    public void Collapse()
    {
        Visibility = System.Windows.Visibility.Collapsed;
        base.Visibility = System.Windows.Visibility.Collapsed;
    }
}
