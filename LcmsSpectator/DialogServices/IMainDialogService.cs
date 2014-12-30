using System;
using LcmsSpectator.ViewModels;

namespace LcmsSpectator.DialogServices
{
    public interface IMainDialogService: IDialogService
    {
        Tuple<string, string> OpenDmsLookup(DmsLookupViewModel dmsLookupViewModel);
        bool OpenSettings(SettingsViewModel settingsViewModel);
        bool OpenHeavyModifications(HeavyModificationsWindowViewModel heavyModificationsWindowVm);
        bool OpenCustomModification(CustomModificationViewModel customModificationViewModel);
        bool OpenDataWindow(OpenDataWindowViewModel openDataWindowViewModel);
        bool OpenSelectDataWindow(SelectDataSetViewModel selectDataViewModel);
        bool FilterBox(FilterViewModel filterViewModel);
        void OpenAboutBox();
    }
}
