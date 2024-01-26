using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Gnoj_Ham;

namespace Gnoj_HamView
{
    /// <summary>
    /// Logique d'interaction pour YakusWindow.xaml
    /// </summary>
    public partial class RulesWindow : Window
    {
        /// <summary>
        /// Ctor
        /// </summary>
        public RulesWindow()
        {
            InitializeComponent();

            LstYakus.ItemsSource = YakuPivot.Yakus
                .Except(new[] { YakuPivot.NagashiMangan })
                .OrderBy(x => x.ConcealedFanCount)
                .ThenBy(x => x.FanCount)
                .ThenBy(x => x.Name);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta / 7));
            e.Handled = true;
        }
    }
}
