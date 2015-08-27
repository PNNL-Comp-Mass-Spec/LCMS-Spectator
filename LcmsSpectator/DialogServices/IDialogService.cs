// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IDialogService.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for service for opening dialogs from a view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using LcmsSpectator.Models.Dataset;

namespace LcmsSpectator.DialogServices
{
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// Interface for service for opening dialogs from a view model.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Open a dialog for selecting a file to open.
        /// </summary>
        /// <param name="defaultExt">The default file extension.</param>
        /// <param name="filter">The file extension filter.</param>
        /// <returns>The path for the selected file.</returns>
        string OpenFile(string defaultExt, string filter);

        /// <summary>
        /// Open a dialog for selecting multiple files to open.
        /// </summary>
        /// <param name="defaultExt">The default file extension.</param>
        /// <param name="filter">The file extension filter.</param>
        /// <returns>The paths for the selected file.</returns>
        IEnumerable<string> MultiSelectOpenFile(string defaultExt, string filter);

        /// <summary>
        /// Open a dialog for selecting a file to path to save to.
        /// </summary>
        /// <param name="defaultExt">The default file extension.</param>
        /// <param name="filter">The file extension filter.</param>
        /// <returns>The path for the selected file.</returns>
        string SaveFile(string defaultExt, string filter);

        /// <summary>
        /// Open a dialog for selecting a folder path to save to.
        /// </summary>
        /// <param name="description">The descriptive text displayed above the Tree View in the dialog box.</param>
        /// <returns>The path for the selected folder.</returns>
        string OpenFolder(string description = "");

        /// <summary>
        /// Open a confirmation box.
        /// </summary>
        /// <param name="message">Message to display on confirmation box.</param>
        /// <param name="title">The title of the confirmation box.</param>
        /// <returns>A value indicating whether the user confirmed.</returns>
        bool ConfirmationBox(string message, string title);

        /// <summary>
        /// Open a message box.
        /// </summary>
        /// <param name="text">Message to display on message box.</param>
        void MessageBox(string text);

        /// <summary>
        /// Open a error alert box.
        /// </summary>
        /// <param name="e">Exception to display on message box.</param>
        void ExceptionAlert(Exception e);
    }
}
