// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Tracks settings and then publishes them to application settings when user clicks OK.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using LcmsSpectator.Models.Dataset;

namespace LcmsSpectator.ViewModels.Settings
{
    using System;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.ViewModels.Modifications;
    using ReactiveUI;
    
    /// <summary>
    /// Tracks settings and then publishes them to application settings when user clicks OK.
    /// </summary>
    public class SettingsViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class. 
        /// </summary>
        /// <param name="projectInfo">Project info to set settings for.</param>
        /// <param name="dialogService">Dialog service for opening dialogs from a view model.</param>
        public SettingsViewModel(ProjectInfo projectInfo, IMainDialogService dialogService = null)
        {
            dialogService = dialogService ?? new MainDialogService();
            this.Status = false;

            this.SaveCommand = ReactiveCommand.Create();
            this.SaveCommand.Subscribe(_ => this.SaveImplementation());

            this.CancelCommand = ReactiveCommand.Create();
            this.CancelCommand.Subscribe(_ => this.CancelImplementation());

            //this.HeavyModificationsViewModel = new HeavyModificationsViewModel(
            //    projectInfo.ModificationSettings.RegisteredModifications,
            //    projectInfo.ModificationSettings.LightModifications,
            //    projectInfo.ModificationSettings.HeavyModifications,
            //    dialogService);

            this.FeatureMapSettingsViewModel = new FeatureMapSettingsViewModel(projectInfo.Datasets[0].FeatureMapSettings, SingletonProjectManager.Instance.Datasets);
            this.ImageExportSettingsViewModel = new ImageExportSettingsViewModel(projectInfo.Datasets[0].ImageExportSettings, SingletonProjectManager.Instance.Datasets);
            this.IonTypeSettingsViewModel = new IonTypeSettingsViewModel(projectInfo.Datasets[0].IonTypeSettings, SingletonProjectManager.Instance.Datasets);
            this.ToleranceSettingsViewModel = new ToleranceSettingsViewModel(projectInfo.Datasets[0].ToleranceSettings, SingletonProjectManager.Instance.Datasets);
        }

        /// <summary>
        /// Event that is triggered when this is ready to close (user has clicked "OK" or "Cancel")
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a value indicating whether the settings should be saved.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets a command that validates all settings and saves them.
        /// </summary>
        public ReactiveCommand<object> SaveCommand { get; private set; }

        /// <summary>
        /// Gets a command that closes settings without saving.
        /// </summary>
        public ReactiveCommand<object> CancelCommand { get; private set; }

        /// <summary>
        /// Gets the view Model for light/heavy modification selector
        /// </summary>
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; private set; }

        /// <summary>
        /// Gets the view model for tolerance settings.
        /// </summary>
        public FeatureMapSettingsViewModel FeatureMapSettingsViewModel { get; private set; }

        /// <summary>
        /// Gets the view model for image export settings.
        /// </summary>
        public ImageExportSettingsViewModel ImageExportSettingsViewModel { get; private set; }

        /// <summary>
        /// Gets the view model for the ion type settings.
        /// </summary>
        public IonTypeSettingsViewModel IonTypeSettingsViewModel { get; private set; }

        /// <summary>
        /// Gets the view model for modification settings.
        /// </summary>
        public ModificationSettingsViewModel ModificationSettingsViewModel { get; private set; }

        /// <summary>
        /// Gets the view model for tolerance settings.
        /// </summary>
        public ToleranceSettingsViewModel ToleranceSettingsViewModel { get; private set; }

        /// <summary>
        /// Implementation for SaveCommand.
        /// Validates all settings and saves them.
        /// </summary>
        private void SaveImplementation()
        {
            this.Status = true;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, null);
            }
        }

        /// <summary>
        /// Implementation for CancelCommand.
        /// Closes settings without saving.
        /// </summary>
        private void CancelImplementation()
        {
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, null);
            }
        }
    }
}
