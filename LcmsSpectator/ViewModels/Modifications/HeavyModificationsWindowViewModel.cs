// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeavyModificationsWindowViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for a window for selecting heavy modifications for light and heavy peptides.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reactive;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Modifications
{
    /// <summary>
    /// View model for a window for selecting heavy modifications for light and heavy peptides.
    /// </summary>
    public class HeavyModificationsWindowViewModel : ReactiveObject
    {
        /// <summary>
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public HeavyModificationsWindowViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HeavyModificationsWindowViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public HeavyModificationsWindowViewModel(IDialogService dialogService)
        {
            HeavyModificationsViewModel = new HeavyModificationsViewModel(dialogService);

            SaveCommand = ReactiveCommand.Create(SaveImplementation);
            CancelCommand = ReactiveCommand.Create(CancelImplementation);

            Status = false;
        }

        /// <summary>
        /// Event that is triggered when save or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets the HeavyModificationsViewModel for selecting heavy modifications.
        /// </summary>
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; }

        /// <summary>
        /// Gets a command that sets status to true,
        /// saves the selected heavy modifications, and triggers the ReadyToClose event.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SaveCommand { get; }

        /// <summary>
        /// Gets a command that sets status to false and triggers the ReadyToClose event.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

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
            HeavyModificationsViewModel.Save();
            Status = true;
            IcParameters.Instance.Update();
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementation of CancelCommand.
        /// Sets status to false and triggers the ReadyToClose event.
        /// </summary>
        public void CancelImplementation()
        {
            Status = false;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
