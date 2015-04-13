// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FeatureViewerView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for FeatureViewerView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views
{
    using System;
    using System.Globalization;
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
        }

        /// <summary>
        /// Event handler for up button click.
        /// Increments the value in the PointsDisplayedTextBox.
        /// </summary>
        /// <param name="sender">The sender Button</param>
        /// <param name="args">The event arguments.</param>
        public void UpClicked(object sender, EventArgs args)
        {
            int num;
            if (!int.TryParse(PointsDisplayedTextBox.Text, out num))
            {
                return;
            }

            PointsDisplayedTextBox.Text = (num + 100).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Event handler for down button click.
        /// Decrements the value in the PointsDisplayedTextBox.
        /// </summary>
        /// <param name="sender">The sender Button</param>
        /// <param name="args">The event arguments.</param>
        public void DownClicked(object sender, EventArgs args)
        {
            int num;
            if (!int.TryParse(PointsDisplayedTextBox.Text, out num))
            {
                return;
            }

            var newValue = Math.Max(num - 100, 0);
            PointsDisplayedTextBox.Text = newValue.ToString(CultureInfo.InvariantCulture);
        }
    }
}
