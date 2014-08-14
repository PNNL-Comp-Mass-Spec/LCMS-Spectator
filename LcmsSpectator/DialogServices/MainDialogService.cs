using LcmsSpectator.ViewModels;
using LcmsSpectator.Views;

namespace LcmsSpectator.DialogServices
{
    public class MainDialogService: DialogService, IMainDialogService
    {
        public bool OpenSettings(SettingsViewModel settingsViewModel)
        {
            var settingsDialog = new Settings {DataContext = settingsViewModel};
            settingsViewModel.ReadyToClose += (o, e) => settingsDialog.Close();
            settingsDialog.ShowDialog();
            return settingsViewModel.Status;
        }
    }
}
