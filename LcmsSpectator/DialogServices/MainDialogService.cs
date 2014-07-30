using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using LcmsSpectator.Views;
using Ookii.Dialogs;

namespace LcmsSpectator.DialogServices
{
    public class MainDialogService: IMainDialogService
    {
        public bool OpenSettings()
        {
            var settingsDialog = new Settings();
            settingsDialog.ShowDialog();
            return settingsDialog.Status;
        }

        public bool ConfirmationBox(string message, string title)
        {
            var dialogResult = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo);
            return (dialogResult == MessageBoxResult.Yes);
        }

        public void MessageBox(string text)
        {
            System.Windows.MessageBox.Show(text);
        }

        public string OpenFile(string defaultExt, string filter)
        {
            var dialog = new VistaOpenFileDialog { DefaultExt = defaultExt, Filter = filter };

            DialogResult result = dialog.ShowDialog();
            string fileName = "";
            if (result == DialogResult.OK) fileName = dialog.FileName;
            return fileName;
        }


        public void ExceptionAlert(Exception e)
        {
            System.Windows.MessageBox.Show(e.Message, "", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
