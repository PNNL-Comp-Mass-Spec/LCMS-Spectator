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
            InitializeComponent();
            Ms2Spectrum.Loaded += (o, e) => { Ms2Spectrum.ContextMenu.DataContext = Ms2Spectrum.DataContext; };
            PreviousMs1.Loaded += (o, e) => { PreviousMs1.ContextMenu.DataContext = PreviousMs1.DataContext; };
            NextMs1.Loaded += (o, e) => { NextMs1.ContextMenu.DataContext = NextMs1.DataContext; };

            Ms2Spectrum.DataContextChanged += (o, e) =>
            {
                Ms2Spectrum.ContextMenu.DataContext = Ms2Spectrum.DataContext;
            };
            PreviousMs1.DataContextChanged += (o, e) =>
            {
                PreviousMs1.ContextMenu.DataContext = PreviousMs1.DataContext;
            };
            NextMs1.DataContextChanged += (o, e) =>
            {
                NextMs1.ContextMenu.DataContext = NextMs1.DataContext;
            };

            AdjustXRow.Height = new GridLength(0, GridUnitType.Pixel);
            AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
            XMin.Visibility = Visibility.Collapsed;
            XMax.Visibility = Visibility.Collapsed;
            YMin.Visibility = Visibility.Collapsed;
            YMax.Visibility = Visibility.Collapsed;

            AutoAdjustYCheck.Checked += ManualAdjustmentChecked;
            AutoAdjustYCheck.Unchecked += ManualAdjustmentChecked;
            ManualAdjustmentCheck.Checked += ManualAdjustmentChecked;
            ManualAdjustmentCheck.Unchecked += ManualAdjustmentChecked;
        }

        /// <summary>
        /// Event handler for checkbox on Manual Adjustment context menu item.
        /// </summary>
        /// <param name="sender">The menu item that was checked.</param>
        /// <param name="args">The event arguments.</param>
        public void ManualAdjustmentChecked(object sender, EventArgs args)
        {
            if (ManualAdjustmentCheck.IsChecked)
            {
                AdjustXRow.Height = new GridLength(25, GridUnitType.Pixel);
                XMin.Visibility = Visibility.Visible;
                XMax.Visibility = Visibility.Visible;
                if (!AutoAdjustYCheck.IsChecked)
                {
                    AdjustYColumn.Width = new GridLength(25, GridUnitType.Pixel);
                    YMin.Visibility = Visibility.Visible;
                    YMax.Visibility = Visibility.Visible;
                }
                else
                {
                    AdjustYColumn.Width = new GridLength(0, GridUnitType.Pixel);
                    YMin.Visibility = Visibility.Collapsed;
                    YMax.Visibility = Visibility.Collapsed;
                }

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
    }
}
