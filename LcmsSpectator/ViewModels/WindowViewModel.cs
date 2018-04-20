// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Base class for windows.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Reactive;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    /// <summary>
    /// Base class for windows.
    /// </summary>
    public abstract class WindowViewModel : ReactiveObject
    {
        /// <summary>
        /// A value indicating whether the data in the window is valid.
        /// </summary>
        private bool status;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowViewModel"/> class.
        /// </summary>
        protected WindowViewModel()
        {
            SuccessCommand = ReactiveCommand.Create(() =>
            {
                Status = Validate();
                if (Status)
                {
                    ReadyToClose?.Invoke(this, EventArgs.Empty);
                }
            });

            CancelCommand = ReactiveCommand.Create(() =>
            {
                Status = false;
                ReadyToClose?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Event that is triggered when save or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets or sets a value indicating whether the data in the window is valid.
        /// </summary>
        public bool Status
        {
            get => status;
            set => this.RaiseAndSetIfChanged(ref status, value);
        }

        /// <summary>
        /// Gets a command for success.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SuccessCommand { get; }

        /// <summary>
        /// Gets a command for canceling the operation.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        /// <summary>
        /// Gets an observable that determines whether or not  the Success command is executable.
        /// </summary>
        protected abstract IObservable<bool> CanSucceed { get; }

        /// <summary>
        /// Function that checks whether or not the input to this window is valid.
        /// </summary>
        /// <returns>A value indicating whether the input to this window is valid.</returns>
        protected abstract bool Validate();
    }
}
