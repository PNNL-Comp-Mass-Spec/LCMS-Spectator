// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchSettingsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for configuration settings for running an MSPathFinder Database search.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.ViewModels.Modifications;
    using ReactiveUI;

    /// <summary>
    /// View model for configuration settings for running an MSPathFinder Database search.
    /// </summary>
    public class SearchSettingsViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// A value indicating whether application modifications were updated.
        /// </summary>
        private bool modificationsUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchSettingsViewModel"/> class. 
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public SearchSettingsViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;

            var manageModificationsCommand = ReactiveCommand.Create();
            manageModificationsCommand.Subscribe(_ => this.ManageModificationsImplementation());
            this.ManageModificationsCommand = manageModificationsCommand;

            var runCommand = ReactiveCommand.Create();
            runCommand.Subscribe(_ => this.RunImplementation());
            this.RunCommand = runCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            this.ModificationsUpdated = false;
        }

        /// <summary>
        /// Event that is triggered when save or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a value indicating whether a valid modification has been selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets a command that manages the modification registered with the application.
        /// </summary>
        public IReactiveCommand ManageModificationsCommand { get; private set; }

        /// <summary>
        /// Gets a command that validates search settings and closes the window.
        /// </summary>
        public IReactiveCommand RunCommand { get; private set; }

        /// <summary>
        /// Gets a command that closes the window.
        /// </summary>
        public IReactiveCommand CancelCommand { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether application modifications were updated.
        /// </summary>
        public bool ModificationsUpdated
        {
            get { return this.modificationsUpdated; }
            set { this.RaiseAndSetIfChanged(ref this.modificationsUpdated, value); }
        }

        /// <summary>
        /// Implementation for ManageModificationsCommand.
        /// Gets or sets a command that manages the modification registered with the application.
        /// </summary>
        private void ManageModificationsImplementation()
        {
            var manageModificationsViewModel = new ManageModificationsViewModel(this.dialogService);
            manageModificationsViewModel.Modifications.AddRange(IcParameters.Instance.RegisteredModifications);
            this.dialogService.OpenManageModifications(manageModificationsViewModel);

            this.ModificationsUpdated = true;
        }

        /// <summary>
        /// Implementation for <see cref="RunCommand"/>.
        /// Gets a command that validates search settings and closes the window.
        /// </summary>
        private void RunImplementation()
        {
            this.Status = true;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation for <see cref="CancelCommand"/>.
        /// Gets a command that closes the window.
        /// </summary>
        private void CancelImplementation()
        {
            this.Status = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }
    }
}
