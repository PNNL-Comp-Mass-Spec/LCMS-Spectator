// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpectrumView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for SpectrumView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views.Plots
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for SpectrumView.xaml
    /// </summary>
    public partial class SpectrumView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpectrumView"/> class.
        /// </summary>
        public SpectrumView()
        {
            this.InitializeComponent();
            this.Ms2Spectrum.Loaded += (o, e) => { this.Ms2Spectrum.ContextMenu.DataContext = this.Ms2Spectrum.DataContext; };
            this.PreviousMs1.Loaded += (o, e) => { this.PreviousMs1.ContextMenu.DataContext = this.PreviousMs1.DataContext; };
            this.NextMs1.Loaded += (o, e) => { this.NextMs1.ContextMenu.DataContext = this.NextMs1.DataContext; };

            this.Ms2Spectrum.DataContextChanged += (o, e) =>
            {
                this.Ms2Spectrum.ContextMenu.DataContext = this.Ms2Spectrum.DataContext;
            };
            this.PreviousMs1.DataContextChanged += (o, e) =>
            {
                this.PreviousMs1.ContextMenu.DataContext = this.PreviousMs1.DataContext;
            };
            this.NextMs1.DataContextChanged += (o, e) =>
            {
                this.NextMs1.ContextMenu.DataContext = this.NextMs1.DataContext;
            };

            this.AdjustXRow.Height = new GridLength(0, GridUnitType.Pixel);
            this.AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
            this.XMin.Visibility = Visibility.Collapsed;
            this.XMax.Visibility = Visibility.Collapsed;
            this.YMin.Visibility = Visibility.Collapsed;
            this.YMax.Visibility = Visibility.Collapsed;

            this.AutoAdjustYCheck.Checked += this.ManualAdjustmentChecked;
            this.AutoAdjustYCheck.Unchecked += this.ManualAdjustmentChecked;
            this.ManualAdjustmentCheck.Checked += this.ManualAdjustmentChecked;
            this.ManualAdjustmentCheck.Unchecked += this.ManualAdjustmentChecked;
        }

        /// <summary>
        /// Event handler for checkbox on Manual Adjustment context menu item.
        /// </summary>
        /// <param name="sender">The menu item that was checked.</param>
        /// <param name="args">The event arguments.</param>
        public void ManualAdjustmentChecked(object sender, EventArgs args)
        {
            if (this.ManualAdjustmentCheck.IsChecked)
            {
                this.AdjustXRow.Height = new GridLength(25, GridUnitType.Pixel);
                this.XMin.Visibility = Visibility.Visible;
                this.XMax.Visibility = Visibility.Visible;
                if (!this.AutoAdjustYCheck.IsChecked)
                {
                    this.AdjustYColumn.Width = new GridLength(25, GridUnitType.Pixel);
                    this.YMin.Visibility = Visibility.Visible;
                    this.YMax.Visibility = Visibility.Visible;
                }
                else
                {
                    this.AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
                    this.YMin.Visibility = Visibility.Collapsed;
                    this.YMax.Visibility = Visibility.Collapsed;
                }

                this.InvalidateVisual();
            }
            else
            {
                this.AdjustXRow.Height = new GridLength(0, GridUnitType.Pixel);
                this.AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
                this.XMin.Visibility = Visibility.Collapsed;
                this.XMax.Visibility = Visibility.Collapsed;
                this.YMin.Visibility = Visibility.Collapsed;
                this.YMax.Visibility = Visibility.Collapsed;
                this.InvalidateVisual();
            }
        }
    }
}
