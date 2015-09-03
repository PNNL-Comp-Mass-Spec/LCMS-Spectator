// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainDialogService.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Dialog services for opening LCMSSpectator dialog boxes from a view model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Models.Dataset;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Settings;

namespace LcmsSpectator.DialogServices
{
    using System;
    using LcmsSpectator.ViewModels;
    using LcmsSpectator.ViewModels.Dms;
    using LcmsSpectator.ViewModels.FileSelectors;
    using LcmsSpectator.ViewModels.Filters;
    using LcmsSpectator.ViewModels.Modifications;
    using LcmsSpectator.ViewModels.Plots;
    using LcmsSpectator.Views;
    using LcmsSpectator.Views.FileSelectors;
    using LcmsSpectator.Views.Filters;
    using LcmsSpectator.Views.Modifications;
    using LcmsSpectator.Views.Plots;

    /// <summary>
    /// Dialog services for opening LCMSSpectator dialog boxes from a view model.
    /// </summary>
    public class MainDialogService : DialogService, IMainDialogService
    {
        /// <summary>
        /// Open a dialog to search for a file on DMS.
        /// </summary>
        /// <param name="dmsLookupViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool OpenDmsLookup(DmsLookupViewModel dmsLookupViewModel)
        {
            var dmsLookupDialog = new DmsLookupView { DataContext = dmsLookupViewModel };
            dmsLookupViewModel.ReadyToClose += (o, e) => dmsLookupDialog.Close();
            dmsLookupDialog.ShowDialog();
            return dmsLookupViewModel.Status;
        }

        /// <summary>
        /// Open a dialog to edit application settings.
        /// </summary>
        /// <param name="settingsViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool OpenSettings(SettingsViewModel settingsViewModel)
        {
            var settingsDialog = new Settings { DataContext = settingsViewModel };
            settingsViewModel.ReadyToClose += (o, e) => settingsDialog.Close();
            settingsDialog.ShowDialog();
            return settingsViewModel.Status;
        }

        /// <summary>
        /// Open a dialog to edit heavy modification settings.
        /// </summary>
        /// <param name="heavyModificationsWindowVm">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool OpenHeavyModifications(HeavyModificationsWindowViewModel heavyModificationsWindowVm)
        {
            var heavyModificationsDialog = new HeavyModificationsWindow { DataContext = heavyModificationsWindowVm };
            heavyModificationsWindowVm.ReadyToClose += (o, e) => heavyModificationsDialog.Close();
            heavyModificationsDialog.ShowDialog();
            return heavyModificationsWindowVm.Status;
        }

        /// <summary>
        /// Open a dialog to edit a modification.
        /// </summary>
        /// <param name="customModificationViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool OpenCustomModification(CustomModificationViewModel customModificationViewModel)
        {
            var customModificationDialog = new CustomModificationView { DataContext = customModificationViewModel };
            customModificationViewModel.ReadyToClose += (o, e) => customModificationDialog.Close();
            customModificationDialog.ShowDialog();
            return customModificationViewModel.Status;
        }

        /// <summary>
        /// Open a dialog to manage modifications.
        /// </summary>
        /// <param name="manageModificationsViewModel">The view model for the dialog.</param>
        public void OpenManageModifications(ManageModificationsViewModel manageModificationsViewModel)
        {
            var manageModificationsDialog = new ManageModificationsWindow { DataContext = manageModificationsViewModel };
            manageModificationsDialog.ShowDialog();
        }

        /// <summary>
        /// Open a dialog to select a raw, id, and feature file path to open.
        /// </summary>
        /// <param name="openDataWindowViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool OpenDataWindow(OpenDataWindowViewModel openDataWindowViewModel)
        {
            var openDataDialog = new OpenDataWindow { DataContext = openDataWindowViewModel };
            openDataWindowViewModel.ReadyToClose += (o, e) => openDataDialog.Close();
            openDataDialog.ShowDialog();
            return openDataWindowViewModel.Status;
        }

        /// <summary>
        /// Open a dialog to select a data set.
        /// </summary>
        /// <param name="selectDataViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool OpenSelectDataWindow(SelectDataSetViewModel selectDataViewModel)
        {
            var selectDataDialog = new SelectDataSetView { DataContext = selectDataViewModel };
            selectDataViewModel.ReadyToClose += (o, e) => selectDataDialog.Close();
            selectDataDialog.ShowDialog();
            return selectDataViewModel.Status;
        }

        /// <summary>
        /// Open a dialog to select a filter value.
        /// </summary>
        /// <param name="filterViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool FilterBox(IFilter filterViewModel)
        {
            filterViewModel.ResetStatus();

            if (filterViewModel is FilterViewModel)
            {
                var filterDialog = new FilterView
                {
                    DataContext = filterViewModel,
                    Title = filterViewModel.Title
                };
                filterViewModel.ReadyToClose += (o, e) => filterDialog.Close();
                filterDialog.ShowDialog();
            }
            else if (filterViewModel is MultiValueFilterViewModel)
            {
                var filterDialog = new MultiValueFilterView
                {
                    DataContext = filterViewModel,
                    Title = filterViewModel.Title
                };
                filterViewModel.ReadyToClose += (o, e) => filterDialog.Close();
                filterDialog.ShowDialog();
            }

            return filterViewModel.Status;
        }

        /// <summary>
        /// Open an About Box dialog.
        /// </summary>
        public void OpenAboutBox()
        {
            var dialog = new AboutBox();
            dialog.ShowDialog();
        }

        /// <summary>
        /// Select multiple raw file paths.
        /// </summary>
        /// <returns>List of dataset for those raw file paths.</returns>
        public IEnumerable<DatasetInfo> OpenRawFiles()
        {
            var files = this.MultiSelectOpenFile(FileConstants.RawFileExtensions[0], MassSpecDataReaderFactory.MassSpecDataTypeFilterString);
            if (files == null)
            {
                return new List<DatasetInfo>();
            }

            return files.Select(file => new DatasetInfo(new List<string> {file}));
        }

        /// <summary>
        /// Open an Error Map window.
        /// </summary>
        /// <param name="errorMapViewModel">The view model for the dialog.</param>
        public void OpenErrorMapWindow(ErrorMapViewModel errorMapViewModel)
        {
            var errorMapWindow = new ErrorMapWindow { DataContext = errorMapViewModel, Height = 600, Width = 800 };
            errorMapWindow.Show();
        }

        /// <summary>
        /// Open the MSPathFinder Search Settings Window.
        /// </summary>
        /// <param name="searchSettingsViewModel">The view model for the dialog.</param>
        public void SearchSettingsWindow(SearchSettingsViewModel searchSettingsViewModel)
        {
            var searchSettingsDialog = new SearchSettingsWindow { DataContext = searchSettingsViewModel };
            searchSettingsViewModel.ReadyToClose += (o, e) => searchSettingsDialog.Close();
            searchSettingsDialog.Show();
        }

        /// <summary>
        /// Open a window for selecting a dataset to export to file.
        /// </summary>
        /// <param name="exportDatasetViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool ExportDatasetWindow(ExportDatasetViewModel exportDatasetViewModel)
        {
            var exportDatasetDialog = new ExportDatasetWindow { DataContext = exportDatasetViewModel };
            exportDatasetViewModel.ReadyToClose += (o, e) => exportDatasetDialog.Close();
            exportDatasetDialog.ShowDialog();
            return exportDatasetViewModel.Status;
        }

        /// <summary>
        /// Open a window for exporting a PlotModel to an image.
        /// </summary>
        /// <param name="exportImageViewModel">The view model for the dialog.</param>
        /// <returns>A value indicating whether the user clicked OK on the dialog.</returns>
        public bool ExportImageWindow(ExportImageViewModel exportImageViewModel)
        {
            var exportImageDialog = new ExportImageWindow { DataContext = exportImageViewModel };
            exportImageViewModel.ReadyToClose += (o, e) => exportImageDialog.Close();
            exportImageDialog.ShowDialog();
            return exportImageViewModel.Status;
        }
    }
}
