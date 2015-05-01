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
        }
    }
}
