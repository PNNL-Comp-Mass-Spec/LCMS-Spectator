// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureViewerView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for FeatureViewerView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;
using System.Windows.Controls;

namespace LcmsSpectator.Views.Plots
{
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
            InitializeComponent();

            AdjustXRow.Height = new GridLength(0, GridUnitType.Pixel);
            AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
            XMin.Visibility = Visibility.Collapsed;
            XMax.Visibility = Visibility.Collapsed;
            YMin.Visibility = Visibility.Collapsed;
            YMax.Visibility = Visibility.Collapsed;
            ManualAdjustment.Checked += ManualAdjustmentChecked;
            ManualAdjustment.Unchecked += ManualAdjustmentChecked;
        }

        /// <summary>
        /// Event handler for checkbox on Manual Adjustment context menu item.
        /// </summary>
        /// <param name="sender">The menu item that was checked.</param>
        /// <param name="args">The event arguments.</param>
        public void ManualAdjustmentChecked(object sender, EventArgs args)
        {
            if (ManualAdjustment.IsChecked)
            {
                AdjustXRow.Height = new GridLength(25, GridUnitType.Pixel);
                AdjustYColumn.Width = new GridLength(25, GridUnitType.Pixel);
                XMin.Visibility = Visibility.Visible;
                XMax.Visibility = Visibility.Visible;
                YMin.Visibility = Visibility.Visible;
                YMax.Visibility = Visibility.Visible;
                InvalidateVisual();
            }
            else
            {
                AdjustXRow.Height = new GridLength(0, GridUnitType.Pixel);
                AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
                XMin.Visibility = Visibility.Collapsed;
                XMax.Visibility = Visibility.Collapsed;
                YMin.Visibility = Visibility.Collapsed;
                YMax.Visibility = Visibility.Collapsed;
                InvalidateVisual();
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
