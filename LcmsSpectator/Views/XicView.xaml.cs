// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for XicView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views
{
    using System.Windows;
    using System.Windows.Controls;
    
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
            this.InitializeComponent();
            FragmentIonXic.Loaded += (o, e) => { FragmentIonXic.ContextMenu.DataContext = FragmentIonXic.DataContext; };
            HeavyFragmentIonXic.Loaded += (o, e) => { HeavyFragmentIonXic.ContextMenu.DataContext = HeavyFragmentIonXic.DataContext; };
            PrecursorIonXic.Loaded += (o, e) => { PrecursorIonXic.ContextMenu.DataContext = PrecursorIonXic.DataContext; };
            HeavyPrecursorIonXic.Loaded += (o, e) => { HeavyPrecursorIonXic.ContextMenu.DataContext = HeavyPrecursorIonXic.DataContext; };
            if (ShowHeavy.IsChecked == true)
            {
                LightColumn.Width = new GridLength(50, GridUnitType.Star);
                HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
                if (ShowFragment.IsChecked == true)
                {
                    FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                }
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
                    FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                }
            }
            else
            {
                FragmentPlotRow.Height = new GridLength(0, GridUnitType.Pixel);
                FragmentTitleRow.Height = new GridLength(0, GridUnitType.Pixel);
                PrecursorPlotRow.Height = new GridLength(100, GridUnitType.Star);
                FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
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
            FragmentIonXic.UpdateLayout();
            PrecursorPlotRow.Height = new GridLength(40, GridUnitType.Star);
            if (ShowHeavy.IsChecked == true)
            {
                HeavyFragmentIonXic.Visibility = Visibility.Visible;
                FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                HeavyFragmentIonXic.UpdateLayout();
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
            HeavyPrecursorIonXic.UpdateLayout();
            if (ShowFragment.IsChecked == true)
            {
                HeavyFragmentIonXic.UpdateLayout();
                FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
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
        }
    }
}
