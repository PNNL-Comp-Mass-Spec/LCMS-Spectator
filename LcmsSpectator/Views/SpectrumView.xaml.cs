using System.Windows.Controls;
using LcmsSpectator.ViewModels;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for SpectrumView.xaml
    /// </summary>
    public partial class SpectrumView : UserControl
    {
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
        }
    }
}
