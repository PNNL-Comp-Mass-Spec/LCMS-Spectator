using DocumentFormat.OpenXml.Wordprocessing;
using LcmsSpectator.ViewModels;

namespace LcmsSpectator.DialogServices
{
    public interface IMainDialogService: IDialogService
    {
        bool OpenSettings(SettingsViewModel settingsViewModel);
    }
}
