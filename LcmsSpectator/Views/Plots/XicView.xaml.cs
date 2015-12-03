// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for XicView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views.Plots
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
            this.FragmentIonXic.Loaded += (o, e) => { this.FragmentIonXic.ContextMenu.DataContext = this.FragmentIonXic.DataContext; };
            this.HeavyFragmentIonXic.Loaded += (o, e) => { this.HeavyFragmentIonXic.ContextMenu.DataContext = this.HeavyFragmentIonXic.DataContext; };
            this.PrecursorIonXic.Loaded += (o, e) => { this.PrecursorIonXic.ContextMenu.DataContext = this.PrecursorIonXic.DataContext; };
            this.HeavyPrecursorIonXic.Loaded += (o, e) => { this.HeavyPrecursorIonXic.ContextMenu.DataContext = this.HeavyPrecursorIonXic.DataContext; };
            if (this.ShowHeavy.IsChecked == true)
            {
                this.LightColumn.Width = new GridLength(50, GridUnitType.Star);
                this.HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
                this.LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
                if (this.ShowFragment.IsChecked == true)
                {
                    this.FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                    this.FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
                }
            }
            else
            {
                this.LightColumn.Width = new GridLength(100, GridUnitType.Star);
                this.HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
                this.FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
                this.LinkColumn.Width = new GridLength(0, GridUnitType.Pixel);
            }

            if (this.ShowFragment.IsChecked == true)
            {
                this.FragmentPlotRow.Height = new GridLength(60, GridUnitType.Star);
                this.FragmentTitleRow.Height = new GridLength(4, GridUnitType.Star);
                this.PrecursorPlotRow.Height = new GridLength(40, GridUnitType.Star);
                this.FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
                if (this.ShowHeavy.IsChecked == true)
                {
                    this.FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                    this.LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
                }
            }
            else
            {
                this.FragmentPlotRow.Height = new GridLength(0, GridUnitType.Pixel);
                this.FragmentTitleRow.Height = new GridLength(0, GridUnitType.Pixel);
                this.PrecursorPlotRow.Height = new GridLength(100, GridUnitType.Star);
                this.FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
                this.FragmentLinkRow.Height = new GridLength(0, GridUnitType.Pixel);
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
            this.FragmentPlotRow.Height = new GridLength(60, GridUnitType.Star);
            this.FragmentTitleRow.Height = new GridLength(4, GridUnitType.Star);
            this.FragmentIonXic.Visibility = Visibility.Visible;
            this.FragmentIonXic.UpdateLayout();
            this.PrecursorPlotRow.Height = new GridLength(40, GridUnitType.Star);
            this.FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
            if (this.ShowHeavy.IsChecked == true)
            {
                this.HeavyFragmentIonXic.Visibility = Visibility.Visible;
                this.FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                this.HeavyFragmentIonXic.UpdateLayout();
                this.LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
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
            this.FragmentPlotRow.Height = new GridLength(0, GridUnitType.Pixel);
            this.FragmentTitleRow.Height = new GridLength(0, GridUnitType.Pixel);
            this.PrecursorPlotRow.Height = new GridLength(100, GridUnitType.Star);
            this.FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);
            this.FragmentLinkRow.Height = new GridLength(0, GridUnitType.Pixel);
            this.FragmentIonXic.Visibility = Visibility.Collapsed;
            this.FragmentIonXic.UpdateLayout();
            this.HeavyFragmentIonXic.Visibility = Visibility.Collapsed;
            this.HeavyFragmentIonXic.UpdateLayout();
        }

        /// <summary>
        /// Event handler for the ShowHeavy checkbox.
        /// Shows or hides heavy XICs based on checkbox value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ShowHeavy_OnChecked(object sender, RoutedEventArgs e)
        {
            this.LightColumn.Width = new GridLength(50, GridUnitType.Star);
            this.HeavyColumn.Width = new GridLength(50, GridUnitType.Star);
            this.PrecursorIonXic.Visibility = Visibility.Visible;
            this.PrecursorIonXic.UpdateLayout();
            this.LinkColumn.Width = new GridLength(30, GridUnitType.Pixel);
            if (this.ShowFragment.IsChecked == true)
            {
                this.HeavyFragmentIonXic.Visibility = Visibility.Visible;
                this.HeavyFragmentIonXic.UpdateLayout();
                this.FragmentAreaRow.Height = new GridLength(20, GridUnitType.Pixel);
                this.FragmentLinkRow.Height = new GridLength(30, GridUnitType.Pixel);
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
            this.LightColumn.Width = new GridLength(100, GridUnitType.Star);
            this.HeavyColumn.Width = new GridLength(0, GridUnitType.Star);
            this.FragmentAreaRow.Height = new GridLength(0, GridUnitType.Pixel);

            this.HeavyFragmentIonXic.Visibility = Visibility.Collapsed;
            this.HeavyFragmentIonXic.UpdateLayout();
            this.LinkColumn.Width = new GridLength(0, GridUnitType.Pixel);
        }
    }
}
