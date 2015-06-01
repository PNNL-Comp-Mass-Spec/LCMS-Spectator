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
    using LcmsSpectator.ViewModels.Dms;
    using LcmsSpectator.ViewModels.FileSelectors;
    using LcmsSpectator.ViewModels.Filters;
    using LcmsSpectator.ViewModels.Modifications;
    using LcmsSpectator.ViewModels.Plots;

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
        /// Open a dialog to manage modifications.
        /// </summary>
        /// <param name="manageModificationsViewModel">The view model for the dialog.</param>
        void OpenManageModifications(ManageModificationsViewModel manageModificationsViewModel);

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
        bool FilterBox(IFilter filterViewModel);

        /// <summary>
        /// Open an About Box dialog.
        /// </summary>
        void OpenAboutBox();

        /// <summary>
        /// Open an Error Map window.
        /// </summary>
        /// <param name="errorMapViewModel">The view model for the dialog.</param>
        void OpenErrorMapWindow(ErrorMapViewModel errorMapViewModel);

        /// <summary>
        /// Open the MSPathFinder Search Settings Window.
        /// </summary>
        /// <param name="searchSettingsViewModel">The view model for the dialog.</param>
        void SearchSettingsWindow(SearchSettingsViewModel searchSettingsViewModel);

        /// <summary>
        /// Open a window for selecting a dataset to export to file.
        /// </summary>
        /// <param name="exportDatasetViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool ExportDatasetWindow(ExportDatasetViewModel exportDatasetViewModel);

        /// <summary>
        /// Open a window for exporting a PlotModel to an image.
        /// </summary>
        /// <param name="exportImageViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool ExportImageWindow(ExportImageViewModel exportImageViewModel);
    }
}
