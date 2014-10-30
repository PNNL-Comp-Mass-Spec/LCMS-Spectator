using System;
using System.Windows.Documents;
using LcmsSpectator.ViewModels;
using LcmsSpectator.Views;

namespace LcmsSpectator.DialogServices
{
    public class MainDialogService: DialogService, IMainDialogService
    {
        public Tuple<string, string> OpenDmsLookup(DmsLookupViewModel dmsLookupViewModel)
        {
            var dmsLookupDialog = new DmsLookupView { DataContext = dmsLookupViewModel };
            dmsLookupViewModel.ReadyToClose += (o, e) => dmsLookupDialog.Close();
            dmsLookupDialog.ShowDialog();
            Tuple<string, string> data = null;
            if (dmsLookupViewModel.Status)
            {
                var dataSetFolderPath = (dmsLookupViewModel.SelectedDataset == null) ? "" : 
                                    dmsLookupViewModel.SelectedDataset.DatasetFolderPath;
                var jobFolderPath = (dmsLookupViewModel.SelectedJob == null) ? "" : 
                                    dmsLookupViewModel.SelectedJob.JobFolderPath;
                data = new Tuple<string, string>(dataSetFolderPath, jobFolderPath);
            }
            return data;
        }

        public bool OpenSettings(SettingsViewModel settingsViewModel)
        {
            var settingsDialog = new Settings {DataContext = settingsViewModel};
            settingsViewModel.ReadyToClose += (o, e) => settingsDialog.Close();
            settingsDialog.ShowDialog();
            return settingsViewModel.Status;
        }

        public bool OpenHeavyModifications(HeavyModificationsWindowViewModel heavyModificationsWindowVm)
        {
            var heavyModificationsDialog = new HeavyModificationsWindow { DataContext = heavyModificationsWindowVm };
            heavyModificationsWindowVm.ReadyToClose += (o, e) => heavyModificationsDialog.Close();
            heavyModificationsDialog.ShowDialog();
            return heavyModificationsWindowVm.Status;
        }

        public bool OpenCustomModification(CustomModificationViewModel customModificationViewModel)
        {
            var customModificationDialog = new CustomModificationView { DataContext = customModificationViewModel };
            customModificationViewModel.ReadyToClose += (o, e) => customModificationDialog.Close();
            customModificationDialog.ShowDialog();
            return customModificationViewModel.Status;
        }

        public bool FilterBox(FilterViewModel filterViewModel)
        {
            var filterDialog = new FilterView
            {
                DataContext = filterViewModel, 
                Title = filterViewModel.Title
            };
            filterViewModel.ReadyToClose += (o, e) => filterDialog.Close();
            filterDialog.ShowDialog();
            return filterViewModel.Status;
        }

        public void OpenAboutBox()
        {
            var dialog = new AboutBox();
            dialog.ShowDialog();
        }
    }
}
