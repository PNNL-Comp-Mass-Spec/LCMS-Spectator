// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureViewerView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for FeatureViewerView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views.Plots
{
    using System;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for FeatureViewerView.xaml
    /// </summary>
    public partial class FeatureViewerView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeatureViewerView"/> class.
        /// </summary>
        public FeatureViewerView()
        {
            this.InitializeComponent();

            this.AdjustXRow.Height = new GridLength(0, GridUnitType.Pixel);
            this.AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
            this.XMin.Visibility = Visibility.Collapsed;
            this.XMax.Visibility = Visibility.Collapsed;
            this.YMin.Visibility = Visibility.Collapsed;
            this.YMax.Visibility = Visibility.Collapsed;
            this.ManualAdjustment.Checked += this.ManualAdjustmentChecked;
            this.ManualAdjustment.Unchecked += this.ManualAdjustmentChecked;
        }

        /// <summary>
        /// Event handler for checkbox on Manual Adjustment context menu item.
        /// </summary>
        /// <param name="sender">The menu item that was checked.</param>
        /// <param name="args">The event arguments.</param>
        public void ManualAdjustmentChecked(object sender, EventArgs args)
        {
            if (this.ManualAdjustment.IsChecked)
            {
                this.AdjustXRow.Height = new GridLength(25, GridUnitType.Pixel);
                this.AdjustYColumn.Width = new GridLength(25, GridUnitType.Pixel);
                this.XMin.Visibility = Visibility.Visible;
                this.XMax.Visibility = Visibility.Visible;
                this.YMin.Visibility = Visibility.Visible;
                this.YMax.Visibility = Visibility.Visible;
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

        ////// <summary>
        ////// Event handler for up button click.
        ////// Increments the value in the PointsDisplayedTextBox.
        ////// </summary>
        ////// <param name="sender">The sender Button</param>
        ////// <param name="args">The event arguments.</param>
        //////public void UpClicked(object sender, EventArgs args)
        //////{
        //////    int num;
        //////    if (!int.TryParse(this.PointsDisplayedTextBox.Text, out num))
        //////    {
        //////        return;
        //////    }

        //////    this.PointsDisplayedTextBox.Text = (num + 100).ToString(CultureInfo.InvariantCulture);
        //////}

        ////// <summary>
        ////// Event handler for down button click.
        ////// Decrements the value in the PointsDisplayedTextBox.
        ////// </summary>
        ////// <param name="sender">The sender Button</param>
        ////// <param name="args">The event arguments.</param>
        //////public void DownClicked(object sender, EventArgs args)
        //////{
        //////    int num;
        //////    if (!int.TryParse(this.PointsDisplayedTextBox.Text, out num))
        //////    {
        //////        return;
        //////    }

        //////    var newValue = Math.Max(num - 100, 0);
        //////    this.PointsDisplayedTextBox.Text = newValue.ToString(CultureInfo.InvariantCulture);
        //////}
    }
}
