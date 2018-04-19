// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrSmView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for PrSmView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using LcmsSpectator.Models;

namespace LcmsSpectator.Views.Data
{
    /// <summary>
    /// Interaction logic for PrSmView.xaml
    /// </summary>
    public partial class PrSmView : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrSmView"/> class.
        /// </summary>
        public PrSmView()
        {
            InitializeComponent();

            DataContextChanged += (o, e) =>
                {
                    var textBox = ModificationLocationBox;
                    if (DataContext is PrSm prsm)
                    {
                        var text = prsm.ModificationLocations;
                        textBox.Visibility = !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
        }
    }
}
