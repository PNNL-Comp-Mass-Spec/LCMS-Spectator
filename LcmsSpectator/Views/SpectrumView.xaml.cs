// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpectrumView.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for SpectrumView.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views
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
        }
    }
}
