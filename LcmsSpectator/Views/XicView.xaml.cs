using System.Windows;
using System.Windows.Controls;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for XicView.xaml
    /// </summary>
    public partial class XicView : UserControl
    {
        public XicView()
        {
            InitializeComponent();
            if (ShowHeavy.IsChecked == true)
            {
                LightColumn.Width = new GridLength(50, GridUnitType.Star);
                HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
                FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
            }
            else
            {
                LightColumn.Width = new GridLength(100, GridUnitType.Star);
                HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
                FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        private void ShowHeavy_OnChecked(object sender, RoutedEventArgs e)
        {
            LightColumn.Width = new GridLength(50, GridUnitType.Star);
            HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
            FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
        }

        private void ShowHeavy_OnUnchecked(object sender, RoutedEventArgs e)
        {
            LightColumn.Width = new GridLength(100, GridUnitType.Star);
            HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
            FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
        }
    }
}
