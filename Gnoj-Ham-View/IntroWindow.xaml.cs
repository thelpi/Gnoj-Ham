using System.Windows;
using System.Windows.Controls;
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
        CbbUmaRule.ItemsSource = GraphicTools.GetUmaRuleDisplayValue();
        CbbChronoSpeed.ItemsSource = GraphicTools.GetChronoDisplayValues();
        CbbCpuSpeed.ItemsSource = GraphicTools.GetCpuSpeedDisplayValues();
        CbbDrivenDrawScenario.ItemsSource = GraphicTools.GetDrivenDrawScenarioDisplayValue();

        LoadConfiguration();

        // A TabControl only ever measures its currently-selected tab's content, so SizeToContent
        // alone would make the window resize itself on every tab switch. Measuring every tab's own
        // content up front and taking the tallest one settles the window at that height regardless
        // of which tab ends up selected - no more manual Height resync when a tab's content grows.
        MainTabControl.MinHeight = MainTabControl.Items
            .OfType<TabItem>()
            .Max(tab =>
            {
                var content = (FrameworkElement)tab.Content;
                content.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return content.DesiredSize.Height;
            });
    }

    private void BtnStart_Click(object sender, RoutedEventArgs e)
    {
        SaveConfiguration();

        Hide();

        var (stats, error) = PlayerSaveStorage.Load();

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques ne seront pas sauvegardées.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        var ruleset = new RulePivot
        {
            InitialPointsRule = (InitialPointsRules)CbbPointsRule.SelectedIndex,
            EndOfGameRule = (EndOfGameRules)CbbEndOfGameRule.SelectedIndex,
            UseRedDoras = ChkUseRedDoras.IsChecked == true,
            UseNagashiMangan = ChkUseNagashiMangan.IsChecked == true,
            UseMultipleYakumans = ChkUseMultipleYakumans.IsChecked == true,
            UseKazoeYakuman = ChkUseKazoeYakuman.IsChecked == true,
            UseSuufonRenda = ChkUseSuufonRenda.IsChecked == true,
            UseDoubleYakuman = ChkUseDoubleYakuman.IsChecked == true,
            UmaRule = (UmaRules)CbbUmaRule.SelectedIndex
        };

        if (ChkFourCpus.IsChecked == true)
        {
            new AutoPlayWindow(ruleset).ShowDialog();
        }
        else
        {
            var drivenDraw = DrivenDrawPivot.Resolve((DrivenDrawScenarios)CbbDrivenDrawScenario.SelectedIndex, PlayerIndices.Zero);
            new MainWindow(TxtPlayerName.Text, ruleset, stats, drivenDraw, ChkDebugMode.IsChecked == true).ShowDialog();
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
        CbbPointsRule.SelectedIndex = Settings.Default.InitialPointsRule;
        CbbEndOfGameRule.SelectedIndex = Settings.Default.EndOfGameRule;
        ChkUseRedDoras.IsChecked = Settings.Default.UseRedDoras;
        ChkUseNagashiMangan.IsChecked = Settings.Default.UseNagashiMangan;
        ChkUseMultipleYakumans.IsChecked = Settings.Default.UseMultipleYakumans;
        ChkUseKazoeYakuman.IsChecked = Settings.Default.UseKazoeYakuman;
        ChkUseSuufonRenda.IsChecked = Settings.Default.UseSuufonRenda;
        ChkUseDoubleYakuman.IsChecked = Settings.Default.UseDoubleYakuman;
        CbbUmaRule.SelectedIndex = Settings.Default.UmaRule;

        // Options
        TxtPlayerName.Text = Settings.Default.DefaultPlayerName;
        CbbChronoSpeed.SelectedIndex = Settings.Default.ChronoSpeed;
        CbbCpuSpeed.SelectedIndex = Settings.Default.CpuSpeed;
        ChkSounds.IsChecked = Settings.Default.PlaySounds;
        ChkAutoTsumoRon.IsChecked = Settings.Default.AutoCallMahjong;
        ChkDiscardTip.IsChecked = Settings.Default.DiscardTip;

        // Dvelopment tools
        ChkDebugMode.IsChecked = false;
        ChkFourCpus.IsChecked = false;
        CbbDrivenDrawScenario.SelectedIndex = (int)DrivenDrawScenarios.None;
    }

    private void SaveConfiguration()
    {
        Settings.Default.DefaultPlayerName = TxtPlayerName.Text;
        Settings.Default.ChronoSpeed = CbbChronoSpeed.SelectedIndex;
        Settings.Default.CpuSpeed = CbbCpuSpeed.SelectedIndex;
        Settings.Default.PlaySounds = ChkSounds.IsChecked == true;
        Settings.Default.AutoCallMahjong = ChkAutoTsumoRon.IsChecked == true;
        Settings.Default.DiscardTip = ChkDiscardTip.IsChecked == true;

        Settings.Default.InitialPointsRule = CbbPointsRule.SelectedIndex;
        Settings.Default.EndOfGameRule = CbbEndOfGameRule.SelectedIndex;
        Settings.Default.UseRedDoras = ChkUseRedDoras.IsChecked == true;
        Settings.Default.UseNagashiMangan = ChkUseNagashiMangan.IsChecked == true;
        Settings.Default.UseMultipleYakumans = ChkUseMultipleYakumans.IsChecked == true;
        Settings.Default.UseKazoeYakuman = ChkUseKazoeYakuman.IsChecked == true;
        Settings.Default.UseSuufonRenda = ChkUseSuufonRenda.IsChecked == true;
        Settings.Default.UseDoubleYakuman = ChkUseDoubleYakuman.IsChecked == true;
        Settings.Default.UmaRule = CbbUmaRule.SelectedIndex;

        Settings.Default.Save();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        LoadConfiguration();
    }

    private void BtnResetRulesToDefault_Click(object sender, RoutedEventArgs e)
    {
        CbbPointsRule.SelectedIndex = (int)RulePivot.Default.InitialPointsRule;
        CbbEndOfGameRule.SelectedIndex = (int)RulePivot.Default.EndOfGameRule;
        ChkUseRedDoras.IsChecked = RulePivot.Default.UseRedDoras;
        ChkUseNagashiMangan.IsChecked = RulePivot.Default.UseNagashiMangan;
        ChkUseMultipleYakumans.IsChecked = RulePivot.Default.UseMultipleYakumans;
        ChkUseKazoeYakuman.IsChecked = RulePivot.Default.UseKazoeYakuman;
        ChkUseSuufonRenda.IsChecked = RulePivot.Default.UseSuufonRenda;
        ChkUseDoubleYakuman.IsChecked = RulePivot.Default.UseDoubleYakuman;
        CbbUmaRule.SelectedIndex = (int)RulePivot.Default.UmaRule;
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
        var (stats, error) = PlayerSaveStorage.Load();

        if (!string.IsNullOrWhiteSpace(error))
        {
            MessageBox.Show($"Une erreur est survenue pendant le chargement du fichier de statistiques du joueur ; les statistiques seront vides.\n\nDétails de l'erreur :\n{error}", "Gnoj-Ham - Avertissement");
        }

        new PlayerSaveStatsWindow(stats).ShowDialog();
    }
}
