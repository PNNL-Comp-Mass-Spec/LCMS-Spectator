using System;
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
            FragmentIonXic.Loaded += (o, e) => { FragmentIonXic.ContextMenu.DataContext = FragmentIonXic.DataContext; };
            HeavyFragmentIonXic.Loaded += (o, e) => { HeavyFragmentIonXic.ContextMenu.DataContext = HeavyFragmentIonXic.DataContext; };
            PrecursorIonXic.Loaded += (o, e) => { PrecursorIonXic.ContextMenu.DataContext = PrecursorIonXic.DataContext; };
            HeavyPrecursorIonXic.Loaded += (o, e) => { HeavyPrecursorIonXic.ContextMenu.DataContext = HeavyPrecursorIonXic.DataContext; };
            if (ShowHeavy.IsChecked == true)
            {
                LightColumn.Width = new GridLength(50, GridUnitType.Star);
                HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
                if (ShowFragment.IsChecked == true) FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
            }
            else
            {
                LightColumn.Width = new GridLength(100, GridUnitType.Star);
                HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
                FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
            }

            if (ShowFragment.IsChecked == true)
            {
                FragmentPlotRow.Height = new GridLength(60, GridUnitType.Star);
                FragmentTitleRow.Height = new GridLength(4, GridUnitType.Star);
                PrecursorPlotRow.Height = new GridLength(40, GridUnitType.Star);
                if (ShowHeavy.IsChecked == true)
                {
                    HeavyFragmentIonXic.Visibility = Visibility.Visible;
                    FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                }
            }
            else
            {
                FragmentPlotRow.Height = new GridLength(0, GridUnitType.Pixel);
                FragmentTitleRow.Height = new GridLength(0, GridUnitType.Pixel);
                FragmentIonXic.Visibility = Visibility.Collapsed;
                HeavyFragmentIonXic.Visibility = Visibility.Collapsed;
                PrecursorPlotRow.Height = new GridLength(100, GridUnitType.Star);
                FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        private void FragmentIonXicOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {

        }

        private void ShowFragment_OnChecked(object sender, RoutedEventArgs e)
        {
            FragmentPlotRow.Height = new GridLength(60, GridUnitType.Star);
            FragmentTitleRow.Height = new GridLength(4, GridUnitType.Star);
            FragmentIonXic.Visibility = Visibility.Visible;
            PrecursorPlotRow.Height = new GridLength(40, GridUnitType.Star);
            if (ShowHeavy.IsChecked == true)
            {
                HeavyFragmentIonXic.Visibility = Visibility.Visible;
                FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
            }
        }

        private void ShowFragment_OnUnChecked(object sender, RoutedEventArgs e)
        {
            FragmentPlotRow.Height = new GridLength(0, GridUnitType.Pixel);
            FragmentTitleRow.Height = new GridLength(0, GridUnitType.Pixel);
            FragmentIonXic.Visibility = Visibility.Collapsed;
            HeavyFragmentIonXic.Visibility = Visibility.Collapsed;
            PrecursorPlotRow.Height = new GridLength(100, GridUnitType.Star);
            FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
        }

        private void ShowHeavy_OnChecked(object sender, RoutedEventArgs e)
        {
            LightColumn.Width = new GridLength(50, GridUnitType.Star);
            HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
            if (ShowFragment.IsChecked == true)
            {
                HeavyFragmentIonXic.Visibility = Visibility.Visible;
                FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
            }
        }

        private void ShowHeavy_OnUnchecked(object sender, RoutedEventArgs e)
        {
            LightColumn.Width = new GridLength(100, GridUnitType.Star);
            HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
            FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
        }
    }
}
