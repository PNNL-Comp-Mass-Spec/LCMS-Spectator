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
    using ViewModels;
    using ViewModels.Data;
    using ViewModels.Dms;
    using ViewModels.FileSelectors;
    using ViewModels.Filters;
    using ViewModels.Modifications;
    using ViewModels.Plots;
    using ViewModels.SequenceViewer;
    using ViewModels.StableIsotopeViewer;

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
        /// Open dialog to select a modification from the list.
        /// </summary>
        /// <param name="selectModificationViewModel">The view model for the dialog.</param>
        /// <returns>The selected modification.</returns>
        ModificationViewModel OpenSelectModificationWindow(SelectModificationViewModel selectModificationViewModel);

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

        /// <summary>
        /// Open a window for selecting scan numbers for display.
        /// </summary>
        /// <param name="scanSelectionViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        bool OpenScanSelectionWindow(ScanSelectionViewModel scanSelectionViewModel);

        /// <summary>
        /// Open a window displaying the sequence viewer.
        /// </summary>
        /// <param name="sequenceViewerViewModel">The view model for the window.</param>
        void OpenSequenceViewer(SequenceViewerViewModel sequenceViewerViewModel);

        /// <summary>
        /// Opens the window displaying the stable isotope viewer.
        /// </summary>
        /// <param name="viewModel">The view model for the window.</param>
        void OpenStableIsotopeViewer(StableIsotopeViewModel viewModel);

        /// <summary>
        /// Opens a window displaying the isotopic profile tuner tool.
        /// </summary>
        /// <param name="viewModel">The view model for the window.</param>
        void OpenIsotopicConcentrationTuner(IsotopicConcentrationTunerViewModel viewModel);

        /// <summary>
        /// Exit the program
        /// </summary>
        void QuitProgram();
    }
}
