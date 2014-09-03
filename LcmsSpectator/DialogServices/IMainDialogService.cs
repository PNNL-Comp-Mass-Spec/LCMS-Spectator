using System;
using LcmsSpectator.ViewModels;

namespace LcmsSpectator.DialogServices
{
    public interface IMainDialogService: IDialogService
    {
        Tuple<string, string> OpenDmsLookup(DmsLookupViewModel dmsLookupViewModel);
        bool OpenSettings(SettingsViewModel settingsViewModel);
        bool OpenHeavyModifications(HeavyModificationsWindowViewModel heavyModificationsWindowVm);
        void OpenAboutBox();
    }
}
