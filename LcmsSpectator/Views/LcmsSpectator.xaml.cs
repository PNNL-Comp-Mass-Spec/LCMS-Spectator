using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.ViewModels;
using LcmsSpectatorModels.Models;
using MsPathViewer.Views;
using Ookii.Dialogs;
using DataGrid = System.Windows.Controls.DataGrid;
using TreeView = System.Windows.Controls.TreeView;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for MsPathViewer.xaml
    /// </summary>
    public partial class LcmsSpectator: Window
    {
        public LcmsSpectatorViewModel Ms2ViewerViewModel { get; set; }
        public LcmsSpectator()
        {
            InitializeComponent();
            Ms2ViewerViewModel = new LcmsSpectatorViewModel();
            DataContext = Ms2ViewerViewModel;
            Ms2ViewerViewModel.UpdateSelections += UpdateSelections;
            _updatePlots = false;
            foreach (var baseIonType in Ms2ViewerViewModel.SelectedBaseIonTypes)
            {
                IonTypes.SelectedItems.Add(baseIonType);
            }
            foreach (var neutralLoss in Ms2ViewerViewModel.SelectedNeutralLosses)
            {
                NeutralLosses.SelectedItems.Add(neutralLoss);
            }
            foreach (var fragment in Ms2ViewerViewModel.SelectedFragmentLabels)
            {
                FragmentIons.SelectedItems.Add(fragment);
            }
            foreach (var precursorIon in Ms2ViewerViewModel.SelectedPrecursorLabels)
            {
                PrecursorIsotopes.SelectedItems.Add(precursorIon);
            }
            _updatePlots = true;
        }

        private void OpenRawFile(object sender, RoutedEventArgs e)
        {
            OpenRawFile();
        }

        private void OpenRawFile()
        {
            var dialog = new VistaOpenFileDialog { DefaultExt = ".raw", Filter = @"Raw Files (*.raw)|*.raw" };

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Ms2ViewerViewModel.OpenRawFile(dialog.FileName);
            }   
        }

        private void OpenIdFile(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaOpenFileDialog { DefaultExt = ".txt", Filter = @"IC ID Files (*.tsv)|*.tsv" };

            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Ms2ViewerViewModel.OpenTsvFile(dialog.FileName);
            }   
        }

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFile();
            openFileDialog.ShowDialog();
            InvalidateVisual();
            Ms2ViewerViewModel.OpenFile(openFileDialog.IdFileName, openFileDialog.RawFileName);
        }

        private void Settings(object sender, RoutedEventArgs e)
        {
            var settingsDialog = new Settings();
            settingsDialog.ShowDialog();
            InvalidateVisual();
        }

        private void UpdateSelections(object sender, System.EventArgs e)
        {
            _updatePlots = false;
            foreach (var fragment in Ms2ViewerViewModel.SelectedFragmentLabels)
            {
                FragmentIons.SelectedItems.Add(fragment);
            }
            foreach (var precursorIon in Ms2ViewerViewModel.SelectedPrecursorLabels)
            {
                PrecursorIsotopes.SelectedItems.Add(precursorIon);
            }
            _updatePlots = true;
        }

        private void IonTypeSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatePlots) return;
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                var selectediontypes = dataGrid.SelectedItems;
                var ionTypeList = selectediontypes.Cast<BaseIonType>().ToList();
                Ms2ViewerViewModel.SelectedBaseIonTypes = ionTypeList;
            }
        }

        private void NeutralLossSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatePlots) return;
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                var selectedneutralLosses = dataGrid.SelectedItems;
                var neutralLossList = selectedneutralLosses.Cast<NeutralLoss>().ToList();
                Ms2ViewerViewModel.SelectedNeutralLosses = neutralLossList;
            }
        }

        private void FragmentIonSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatePlots) return;
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                var selectedfragments = dataGrid.SelectedItems;
                var fragmentList = selectedfragments.Cast<LabeledIon>().ToList();
                Ms2ViewerViewModel.SelectedFragmentLabels = fragmentList;
            }
        }

        private void PrecursorIsotopeSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (!_updatePlots) return;
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                var selectedisotopes = dataGrid.SelectedItems;
                var isotopeList = selectedisotopes.Cast<LabeledIon>().ToList();
                Ms2ViewerViewModel.SelectedPrecursorLabels = isotopeList;
            }
        }

        private void ScanSelectionChange(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (!_updatePlots) return;
            var treeView = sender as TreeView;
            if (treeView != null)
            {
                if ((treeView.SelectedItem as PrSm) != null)
                {
                    var selectedPrSm = treeView.SelectedItem as PrSm;
                    Ms2ViewerViewModel.SelectedPrSm = selectedPrSm;
                }
                else
                {
                    var selected = (IIdData) treeView.SelectedItem;
                    var highest = selected.GetHighestScoringPrSm();
                    Ms2ViewerViewModel.SelectedPrSm = highest;
                }
            }
        }

        private bool _updatePlots;
    }
}
