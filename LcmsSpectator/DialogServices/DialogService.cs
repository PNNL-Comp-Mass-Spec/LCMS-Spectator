// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DialogService.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Service for opening dialogs from a view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.DialogServices
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using Microsoft.Win32;

    /// <summary>
    /// Service for opening dialogs from a view model.
    /// </summary>
    public class DialogService : IDialogService
    {
        /// <summary>
        /// Open a confirmation box.
        /// </summary>
        /// <param name="message">Message to display on confirmation box.</param>
        /// <param name="title">The title of the confirmation box.</param>
        /// <returns>A value indicating whether the user confirmed.</returns>
        public virtual bool ConfirmationBox(string message, string title)
        {
            var dialogResult = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo);
            return dialogResult == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Open a message box.
        /// </summary>
        /// <param name="text">Message to display on message box.</param>
        public virtual void MessageBox(string text)
        {
            System.Windows.MessageBox.Show(text);
        }

        /// <summary>
        /// Open a dialog for selecting a file to open.
        /// </summary>
        /// <param name="defaultExt">The default file extension.</param>
        /// <param name="filter">The file extension filter.</param>
        /// <returns>The path for the selected file.</returns>
        public virtual string OpenFile(string defaultExt, string filter)
        {
            var dialog = new OpenFileDialog { DefaultExt = defaultExt, Filter = filter };

            var result = dialog.ShowDialog();
            string fileName = string.Empty;
            if (result == true)
            {
                fileName = dialog.FileName;
            }

            return fileName;
        }

        /// <summary>
        /// Open a dialog for selecting multiple files to open.
        /// </summary>
        /// <param name="defaultExt">The default file extension.</param>
        /// <param name="filter">The file extension filter.</param>
        /// <returns>The paths for the selected file.</returns>
        public IEnumerable<string> MultiSelectOpenFile(string defaultExt, string filter)
        {
            var dialog = new OpenFileDialog
            {
                DefaultExt = defaultExt, Filter = filter, Multiselect = true
            };

            var result = dialog.ShowDialog();
            IEnumerable<string> fileNames = null;
            if (result == true)
            {
                fileNames = dialog.FileNames;
            }

            return fileNames;
        }

        /// <summary>
        /// Open a dialog for selecting a file to path to save to.
        /// </summary>
        /// <param name="defaultExt">The default file extension.</param>
        /// <param name="filter">The file extension filter.</param>
        /// <returns>The path for the selected file.</returns>
        public virtual string SaveFile(string defaultExt, string filter)
        {
            var dialog = new SaveFileDialog { DefaultExt = defaultExt, Filter = filter };
            var result = dialog.ShowDialog();
            string fileName = string.Empty;
            if (result == true)
            {
                fileName = dialog.FileName;
            }

            return fileName;
        }

        /// <summary>
        /// Open a error alert box.
        /// </summary>
        /// <param name="e">Exception to display on message box.</param>
        public virtual void ExceptionAlert(Exception e)
        {
            System.Windows.MessageBox.Show(e.Message, string.Empty, MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
