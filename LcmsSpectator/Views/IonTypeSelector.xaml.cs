using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for IonTypeSelector.xaml
    /// </summary>
    public partial class IonTypeSelector : UserControl
    {
        public IonTypeSelector()
        {
            InitializeComponent();
            var vm = DataContext as IonTypeSelectorViewModel;
            _updateVm = false;
            if (vm != null)
            {
                foreach (var baseIonType in vm.SelectedBaseIonTypes)
                {
                    IonTypes.SelectedItems.Add(baseIonType);
                }
                foreach (var neutralLoss in vm.SelectedNeutralLosses)
                {
                    NeutralLosses.SelectedItems.Add(neutralLoss);
                }
            }
            _updateVm = true;
        }

        private void IonTypeSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (!_updateVm) return;
            var vm = DataContext as IonTypeSelectorViewModel;
            if (vm == null) return;
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                var selectediontypes = dataGrid.SelectedItems;
                var ionTypeList = selectediontypes.Cast<BaseIonType>().ToList();
                vm.SelectedBaseIonTypes = ionTypeList;
            }
        }

        private void NeutralLossSelectionChange(object sender, SelectionChangedEventArgs e)
        {
            if (!_updateVm) return;
            var vm = DataContext as IonTypeSelectorViewModel;
            if (vm == null) return;
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                var selectedneutralLosses = dataGrid.SelectedItems;
                var neutralLossList = selectedneutralLosses.Cast<NeutralLoss>().ToList();
                vm.SelectedNeutralLosses = neutralLossList;
            }
        }

        private readonly bool _updateVm;
    }
}
