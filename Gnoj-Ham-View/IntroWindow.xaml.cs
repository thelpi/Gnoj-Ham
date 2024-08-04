using System.Windows;
using Gnoj_Ham_Library;
using Gnoj_Ham_Library.Enums;
using Gnoj_Ham_View.Properties;

namespace Gnoj_Ham_View;

/// <summary>
/// Interaction logic for IntroWindow.xaml
/// </summary>
public partial class IntroWindow : Window
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public IntroWindow()
    {
        InitializeComponent();

        DevelopmentTab.Visibility = Visibility.Visible;

        CbbEndOfGameRule.ItemsSource = GraphicTools.GetEndOfGameRuleDisplayValue();
        CbbPointsRule.ItemsSource = GraphicTools.GetInitialPointsRuleDisplayValue();
        CbbChronoSpeed.ItemsSource = GraphicTools.GetChronoDisplayValues();
        CbbCpuSpeed.ItemsSource = GraphicTools.GetCpuSpeedDisplayValues();

        LoadConfiguration();
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        SaveConfiguration();

        Hide();

        var (save, error) = PlayerSavePivot.GetOrCreateSave();

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques ne seront pas sauvegardées.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        var ruleset = new RulePivot((InitialPointsRulePivot)CbbPointsRule.SelectedIndex,
            (EndOfGameRulePivot)CbbEndOfGameRule.SelectedIndex,
            ChkUseRedDoras.IsChecked == true,
            ChkUseNagashiMangan.IsChecked == true,
            ChkDebugMode.IsChecked == true,
            ChkDiscardTip.IsChecked == true);

        if (ChkFourCpus.IsChecked == true)
        {
            new AutoPlayWindow(ruleset).ShowDialog();
        }
        else
        {
            new MainWindow(TxtPlayerName.Text, ruleset, save).ShowDialog();
        }

        // The configuration might be updated in-game.
        LoadConfiguration();

        ShowDialog();
    }

    private void BtnQuit_Click(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }

    private void LoadConfiguration()
    {
        // Rules
        CbbPointsRule.SelectedIndex = (int)RulePivot.Default.InitialPointsRule;
        CbbEndOfGameRule.SelectedIndex = (int)RulePivot.Default.EndOfGameRule;
        ChkUseRedDoras.IsChecked = RulePivot.Default.UseRedDoras;
        ChkUseNagashiMangan.IsChecked = RulePivot.Default.UseNagashiMangan;

        // Options as rules
        ChkDiscardTip.IsChecked = RulePivot.Default.DiscardTip;

        // Options
        TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
        CbbChronoSpeed.SelectedIndex = Settings.Default.ChronoSpeed;
        CbbCpuSpeed.SelectedIndex = Settings.Default.CpuSpeed;
        ChkSounds.IsChecked = Settings.Default.PlaySounds;
        ChkAutoTsumoRon.IsChecked = Settings.Default.AutoCallMahjong;

        // Dvelopment tools
        ChkDebugMode.IsChecked = false;
        ChkFourCpus.IsChecked = false;
    }

    private void SaveConfiguration()
    {
        Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
        Settings.Default.ChronoSpeed = CbbChronoSpeed.SelectedIndex;
        Settings.Default.CpuSpeed = CbbCpuSpeed.SelectedIndex;
        Settings.Default.PlaySounds = ChkSounds.IsChecked == true;
        Settings.Default.AutoCallMahjong = ChkAutoTsumoRon.IsChecked == true;

        Settings.Default.Save();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        LoadConfiguration();
    }

    private void HlkAbout_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Bientôt !", "Gnoj-Ham - Information");
    }

    private void HlkYakus_Click(object sender, RoutedEventArgs e)
    {
        new RulesWindow().ShowDialog();
    }

    private void HlkPlayerStats_Click(object sender, RoutedEventArgs e)
    {
        var (save, error) = PlayerSavePivot.GetOrCreateSave();

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques seront vides.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        new PlayerSaveStatsWindow(save).ShowDialog();
    }
}
