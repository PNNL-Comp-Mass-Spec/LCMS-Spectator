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
                data = new Tuple<string, string>(dmsLookupViewModel.SelectedDataset.DatasetFolderPath,
                    dmsLookupViewModel.SelectedJob.JobFolderPath);
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


        public void OpenAboutBox()
        {
            var dialog = new AboutBox();
            dialog.ShowDialog();
        }
    }
}
