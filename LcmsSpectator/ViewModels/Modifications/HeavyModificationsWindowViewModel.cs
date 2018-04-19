// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeavyModificationsWindowViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for a window for selecting heavy modifications for light and heavy peptides.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Modifications
{
    using System;

    using Config;
    using DialogServices;

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
        public HeavyModificationsWindowViewModel(IDialogService dialogService)
        {
            HeavyModificationsViewModel = new HeavyModificationsViewModel(dialogService);

            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => SaveImplementation());
            SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => CancelImplementation());
            CancelCommand = cancelCommand;

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
        public IReactiveCommand SaveCommand { get; }

        /// <summary>
        /// Gets a command that sets status to false and triggers the ReadyToClose event.
        /// </summary>
        public IReactiveCommand CancelCommand { get; }

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
