// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for XicView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace LcmsSpectator.Views.Plots
{
    /// <summary>
    /// Interaction logic for XicView.xaml
    /// </summary>
    public partial class XicView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XicView"/> class.
        /// </summary>
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
                LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
                if (ShowFragment.IsChecked == true)
                {
                    FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                    FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
                }
            }
            else
            {
                LightColumn.Width = new GridLength(100, GridUnitType.Star);
                HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
                FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
                LinkColumn.Width = new GridLength(0, GridUnitType.Pixel);
            }

            if (ShowFragment.IsChecked == true)
            {
                FragmentPlotRow.Height = new GridLength(60, GridUnitType.Star);
                FragmentTitleRow.Height = new GridLength(4, GridUnitType.Star);
                PrecursorPlotRow.Height = new GridLength(40, GridUnitType.Star);
                FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
                if (ShowHeavy.IsChecked == true)
                {
                    FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                    LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
                }
            }
            else
            {
                FragmentPlotRow.Height = new GridLength(0, GridUnitType.Pixel);
                FragmentTitleRow.Height = new GridLength(0, GridUnitType.Pixel);
                PrecursorPlotRow.Height = new GridLength(100, GridUnitType.Star);
                FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
                FragmentLinkRow.Height = new GridLength(0, GridUnitType.Pixel);
            }
        }

        /// <summary>
        /// Event handler for the ShowFragment checkbox.
        /// Shows or hides fragment XICs based on checkbox value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ShowFragment_OnChecked(object sender, RoutedEventArgs e)
        {
            FragmentPlotRow.Height = new GridLength(60, GridUnitType.Star);
            FragmentTitleRow.Height = new GridLength(4, GridUnitType.Star);
            FragmentIonXic.Visibility = Visibility.Visible;
            FragmentIonXic.UpdateLayout();
            PrecursorPlotRow.Height = new GridLength(40, GridUnitType.Star);
            FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
            if (ShowHeavy.IsChecked == true)
            {
                HeavyFragmentIonXic.Visibility = Visibility.Visible;
                FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                HeavyFragmentIonXic.UpdateLayout();
                LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
            }
        }

        /// <summary>
        /// Event handler for the ShowFragment checkbox.
        /// Shows or hides fragment XICs based on checkbox value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ShowFragment_OnUnChecked(object sender, RoutedEventArgs e)
        {
            FragmentPlotRow.Height = new GridLength(0, GridUnitType.Pixel);
            FragmentTitleRow.Height = new GridLength(0, GridUnitType.Pixel);
            PrecursorPlotRow.Height = new GridLength(100, GridUnitType.Star);
            FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
            FragmentLinkRow.Height = new GridLength(0, GridUnitType.Pixel);
            FragmentIonXic.Visibility = Visibility.Collapsed;
            FragmentIonXic.UpdateLayout();
            HeavyFragmentIonXic.Visibility = Visibility.Collapsed;
            HeavyFragmentIonXic.UpdateLayout();
        }

        /// <summary>
        /// Event handler for the ShowHeavy checkbox.
        /// Shows or hides heavy XICs based on checkbox value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ShowHeavy_OnChecked(object sender, RoutedEventArgs e)
        {
            LightColumn.Width = new GridLength(50, GridUnitType.Star);
            HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
            PrecursorIonXic.Visibility = Visibility.Visible;
            PrecursorIonXic.UpdateLayout();
            LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
            if (ShowFragment.IsChecked == true)
            {
                HeavyFragmentIonXic.Visibility = Visibility.Visible;
                HeavyFragmentIonXic.UpdateLayout();
                FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
            }
        }

        /// <summary>
        /// Event handler for the ShowHeavy checkbox.
        /// Shows or hides heavy XICs based on checkbox value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ShowHeavy_OnUnchecked(object sender, RoutedEventArgs e)
        {
            LightColumn.Width = new GridLength(100, GridUnitType.Star);
            HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
            FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);

            HeavyFragmentIonXic.Visibility = Visibility.Collapsed;
            HeavyFragmentIonXic.UpdateLayout();
            LinkColumn.Width = new GridLength(0, GridUnitType.Pixel);
        }
    }
}
