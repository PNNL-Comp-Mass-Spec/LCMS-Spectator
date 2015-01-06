using System;
using System.Collections.Generic;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels;

namespace LcmsSpectatorTests.DialogServices
{
    /// <summary>
    /// The purpose of this class is to private a testable dialog service that does not actually
    /// open any dialogs or even touch the WPF framework. Each method in this class simply sets
    /// a boolean flag when it is called and records its arguments.
    /// </summary>
    public class TestableMainDialogService: IMainDialogService
    {
        /// <summary>
        /// This creates a new TestableMainDialogService.
        /// </summary>
        /// <param name="filePath">
        /// File path that open and save dialog methods will report being selected.
        /// </param>
        public TestableMainDialogService(string filePath="")
        {
            _filePath = filePath;
            DmsLookupOpened = false;
            SettingsOpened = false;
            HeavyModificationsOpened = false;
            AboutBoxOpened = false;
            OpenFileOpened = false;
            SaveFileOpened = false;
            ConfirmationBoxOpened = false;
            MessageBoxOpened = false;
            ExceptionAlertOpened = false;
            MultiSelectOpenFileOpened = false;
            FilterOpened = false;
        }
        
        public bool DmsLookupOpened { get; private set; }
        public DmsLookupViewModel DmsLookupViewModel { get; private set; }
        public Tuple<string, string> OpenDmsLookup(DmsLookupViewModel dmsLookupViewModel)
        {
            DmsLookupViewModel = dmsLookupViewModel;
            DmsLookupOpened = true;
            return new Tuple<string, string>(_filePath,_filePath);
        }

        public bool SettingsOpened { get; private set; }
        public SettingsViewModel SettingsViewModel { get; private set; }
        public bool OpenSettings(SettingsViewModel settingsViewModel)
        {
            SettingsViewModel = settingsViewModel;
            SettingsOpened = true;
            return true;
        }

        public bool HeavyModificationsOpened { get; private set; }
        public HeavyModificationsWindowViewModel HeavyModificationsWindowViewModel { get; private set; }
        public bool OpenHeavyModifications(HeavyModificationsWindowViewModel heavyModificationsWindowVm)
        {
            HeavyModificationsWindowViewModel = heavyModificationsWindowVm;
            HeavyModificationsOpened = true;
            return true;
        }

        public bool AboutBoxOpened { get; private set; }
        public void OpenAboutBox()
        {
            AboutBoxOpened = true;
        }

        public bool OpenFileOpened { get; private set; }
        public string OpenFileDefaultExt { get; private set; }
        public string OpenFileFilter { get; private set; }
        public string OpenFile(string defaultExt, string filter)
        {
            OpenFileDefaultExt = defaultExt;
            OpenFileFilter = filter;
            OpenFileOpened = true;
            return _filePath;
        }

        public bool SaveFileOpened { get; private set; }
        public string SaveFileDefaultExt { get; private set; }
        public string SaveFileFilter { get; private set; }
        public string SaveFile(string defaultExt, string filter)
        {
            SaveFileDefaultExt = defaultExt;
            SaveFileFilter = filter;
            SaveFileOpened = true;
            return _filePath;
        }

        public bool ConfirmationBoxOpened { get; private set; }
        public string ConfirmationBoxMessage { get; private set; }
        public string ConfirmationBoxTitle { get; private set; }
        public bool ConfirmationBox(string message, string title)
        {
            ConfirmationBoxMessage = message;
            ConfirmationBoxTitle = title;
            ConfirmationBoxOpened = true;
            return true;
        }

        public bool MessageBoxOpened { get; private set; }
        public string MessageBoxMessage { get; private set; }
        public void MessageBox(string message)
        {
            MessageBoxMessage = message;
            MessageBoxOpened = true;
        }

        public bool ExceptionAlertOpened { get; private set; }
        public string ExceptionAlertMessage { get; private set; }
        public void ExceptionAlert(Exception e)
        {
            ExceptionAlertMessage = e.Message;
            ExceptionAlertOpened = true;
        }

        public bool MultiSelectOpenFileOpened { get; private set; }
        public string MultiSelectOpenFileDefaultExt { get; private set; }
        public string MultiSelectOpenFileFilter { get; private set; }
        public IEnumerable<string> MultiSelectOpenFile(string defaultExt, string filter)
        {
            MultiSelectOpenFileDefaultExt = defaultExt;
            MultiSelectOpenFileFilter = filter;
            MultiSelectOpenFileOpened = true;
            return new List<string> { _filePath };
        }

        public bool FilterOpened { get; private set; }
        public bool FilterBox(FilterViewModel filterViewModel)
        {
            FilterOpened = true;
            return true;
        }

        // private members
        private readonly string _filePath;


        public bool OpenCustomModification(CustomModificationViewModel customModificationViewModel)
        {
            throw new NotImplementedException();
        }


		public bool OpenDataWindow(OpenDataWindowViewModel openDataWindowViewModel)
		{
			throw new NotImplementedException();
		}

		public bool OpenSelectDataWindow(SelectDataSetViewModel selectDataViewModel)
		{
			throw new NotImplementedException();
		}
	}
}
