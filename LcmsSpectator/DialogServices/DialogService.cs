using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Win32;

namespace LcmsSpectator.DialogServices
{
    public class DialogService: IDialogService
    {
        public virtual bool ConfirmationBox(string message, string title)
        {
            var dialogResult = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo);
            return (dialogResult == MessageBoxResult.Yes);
        }

        public virtual void MessageBox(string text)
        {
            System.Windows.MessageBox.Show(text);
        }

        public virtual string OpenFile(string defaultExt, string filter)
        {
            var dialog = new OpenFileDialog { DefaultExt = defaultExt, Filter = filter };

            var result = dialog.ShowDialog();
            string fileName = "";
            if (result == true) fileName = dialog.FileName;
            return fileName;
        }

        public IEnumerable<string> MultiSelectOpenFile(string defaultExt, string filter)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = defaultExt, Filter = filter, Multiselect = true
            };

            var result = dialog.ShowDialog();
            IEnumerable<string> fileNames = null;
            if (result == true) fileNames = dialog.FileNames;
            return fileNames;
        }

        public virtual string SaveFile(string defaultExt, string filter)
        {
            var dialog = new SaveFileDialog { DefaultExt = defaultExt, Filter = filter };
            var result = dialog.ShowDialog();
            string fileName = "";
            if (result == true) fileName = dialog.FileName;
            return fileName;
        }

        public virtual void ExceptionAlert(Exception e)
        {
            System.Windows.MessageBox.Show(e.Message, "", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
