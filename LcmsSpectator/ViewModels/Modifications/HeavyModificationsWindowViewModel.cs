// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeavyModificationsWindowViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for a window for selecting heavy modifications for light and heavy peptides.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using LcmsSpectator.Models;

namespace LcmsSpectator.ViewModels.Modifications
{
    using System;

    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;

    using ReactiveUI;

    /// <summary>
    /// View model for a window for selecting heavy modifications for light and heavy peptides.
    /// </summary>
    public class HeavyModificationsWindowViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeavyModificationsWindowViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public HeavyModificationsWindowViewModel(IDialogService dialogService, ModificationSettings settings)
        {
            this.HeavyModificationsViewModel = new HeavyModificationsViewModel(
                SingletonProjectManager.Instance.ProjectInfo.RegisteredModifications,
                settings.LightModifications,
                settings.HeavyModifications,
                dialogService);

            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => this.SaveImplementation());
            this.SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            this.Status = false;
        }

        /// <summary>
        /// Event that is triggered when save or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets the HeavyModificationsViewModel for selecting heavy modifications.
        /// </summary>
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; private set; }

        /// <summary>
        /// Gets a command that sets status to true, 
        /// saves the selected heavy modifications, and triggers the ReadyToClose event.
        /// </summary>
        public IReactiveCommand SaveCommand { get; private set; }

        /// <summary>
        /// Gets a command that sets status to false and triggers the ReadyToClose event.
        /// </summary>
        public IReactiveCommand CancelCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the modifications should be saved.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Implementation of SaveCommand.
        /// Sets status to true, saves the selected heavy modifications, and triggers the ReadyToClose event.
        /// </summary>
        public void SaveImplementation()
        {
            //this.HeavyModificationsViewModel.Save();
            this.Status = true;
            //IcParameters.Instance.Update();
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation of CancelCommand.
        /// Sets status to false and triggers the ReadyToClose event.
        /// </summary>
        public void CancelImplementation()
        {
            this.Status = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }
    }
}
