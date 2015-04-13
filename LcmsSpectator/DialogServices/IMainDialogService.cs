// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IMainDialogService.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interface for dialog services for opening LCMSSpectator dialog boxes from a view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.DialogServices
{
    using System;
    using LcmsSpectator.ViewModels;
    
    /// <summary>
    /// Interface for dialog services for opening LCMSSpectator dialog boxes from a view model.
    /// </summary>
    public interface IMainDialogService : IDialogService
    {
        /// <summary>
        /// Open a dialog to search for a file on DMS.
        /// </summary>
        /// <param name="dmsLookupViewModel">The view model for the dialog.</param>
        /// <returns>The name of the data set and the name of the job selected.</returns>
        Tuple<string, string> OpenDmsLookup(DmsLookupViewModel dmsLookupViewModel);

        /// <summary>
        /// Open a dialog to edit application settings.
        /// </summary>
        /// <param name="settingsViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool OpenSettings(SettingsViewModel settingsViewModel);

        /// <summary>
        /// Open a dialog to edit heavy modification settings.
        /// </summary>
        /// <param name="heavyModificationsWindowVm">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool OpenHeavyModifications(HeavyModificationsWindowViewModel heavyModificationsWindowVm);

        /// <summary>
        /// Open a dialog to edit a modification.
        /// </summary>
        /// <param name="customModificationViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool OpenCustomModification(CustomModificationViewModel customModificationViewModel);

        /// <summary>
        /// Open a dialog to select a raw, id, and feature file path to open.
        /// </summary>
        /// <param name="openDataWindowViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool OpenDataWindow(OpenDataWindowViewModel openDataWindowViewModel);

        /// <summary>
        /// Open a dialog to select a data set.
        /// </summary>
        /// <param name="selectDataViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool OpenSelectDataWindow(SelectDataSetViewModel selectDataViewModel);

        /// <summary>
        /// Open a dialog to select a filter value.
        /// </summary>
        /// <param name="filterViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool FilterBox(FilterViewModel filterViewModel);

        /// <summary>
        /// Open an About Box dialog.
        /// </summary>
        void OpenAboutBox();

        /// <summary>
        /// Open an Error Map window.
        /// </summary>
        /// <param name="errorMapViewModel">The view model for the dialog.</param>
        void OpenErrorMapWindow(ErrorMapViewModel errorMapViewModel);
    }
}
