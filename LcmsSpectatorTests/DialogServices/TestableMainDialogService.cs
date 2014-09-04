using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LcmsSpectator.DialogServices;

namespace LcmsSpectatorTests.DialogServices
{
    public class TestableMainDialogService: IMainDialogService
    {
        public Tuple<string, string> OpenDmsLookup(LcmsSpectator.ViewModels.DmsLookupViewModel dmsLookupViewModel)
        {
            throw new NotImplementedException();
        }

        public bool OpenSettings(LcmsSpectator.ViewModels.SettingsViewModel settingsViewModel)
        {
            return true;
        }

        public bool OpenHeavyModifications(LcmsSpectator.ViewModels.HeavyModificationsWindowViewModel heavyModificationsWindowVm)
        {
            return true;
        }

        public bool AboutBoxOpened { get; private set; }
        public void OpenAboutBox()
        {
            AboutBoxOpened = true;
        }

        public bool OpenFileOpened { get; private set; }
        public string OpenFile(string defaultExt, string filter)
        {
            OpenFileOpened = true;
            return "";
        }

        public bool SaveFileOpened { get; private set; }
        public string SaveFile(string defaultExt, string filter)
        {
            SaveFileOpened = true;
            return "";
        }

        public bool ConfirmationBox(string message, string title)
        {
            throw new NotImplementedException();
        }

        public void MessageBox(string text)
        {
            throw new NotImplementedException();
        }

        public void ExceptionAlert(Exception e)
        {
            throw new NotImplementedException();
        }
    }
}
