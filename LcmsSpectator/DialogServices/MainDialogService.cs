using LcmsSpectator.Views;

namespace LcmsSpectator.DialogServices
{
    public class MainDialogService: DialogService, IMainDialogService
    {
        public bool OpenSettings()
        {
            var settingsDialog = new Settings();
            settingsDialog.ShowDialog();
            return settingsDialog.Status;
        }
    }
}
