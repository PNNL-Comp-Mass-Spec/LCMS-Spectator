using System;
using System.Globalization;
using System.Windows.Controls;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for FeatureViewerView.xaml
    /// </summary>
    public partial class FeatureViewerView: UserControl
    {
        public FeatureViewerView()
        {
            InitializeComponent();
        }

        public void UpClicked(object sender, EventArgs args)
        {
            int num = 0;
            if (!Int32.TryParse(PointsDisplayedTextBox.Text, out num)) return;
            PointsDisplayedTextBox.Text = (num + 100).ToString(CultureInfo.InvariantCulture);
        }

        public void DownClicked(object sender, EventArgs args)
        {
            int num = 0;
            if (!Int32.TryParse(PointsDisplayedTextBox.Text, out num)) return;
            var newValue = Math.Max(num - 100, 0);
            PointsDisplayedTextBox.Text = (newValue).ToString(CultureInfo.InvariantCulture);
        }
    }
}
