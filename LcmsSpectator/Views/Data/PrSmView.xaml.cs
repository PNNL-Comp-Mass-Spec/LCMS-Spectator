// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PrSmView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for PrSmView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using LcmsSpectator.Models.DTO;

namespace LcmsSpectator.Views
{
    using System;
    using System.Windows;
    using System.Windows.Controls;

    using LcmsSpectator.Models;

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
            this.InitializeComponent();

            this.DataContextChanged += (o, e) =>
                {
                    var textBox = this.ModificationLocationBox;
                    var prsm = this.DataContext as PrSm;
                    if (prsm != null)
                    {
                        var text = prsm.ModificationLocations;
                        textBox.Visibility = !string.IsNullOrWhiteSpace(text) ? Visibility.Visible : Visibility.Collapsed;
                    }
                };
        }
    }
}
