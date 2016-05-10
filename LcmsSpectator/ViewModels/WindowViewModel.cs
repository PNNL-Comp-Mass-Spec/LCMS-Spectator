// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WindowViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Base class for windows.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Reactive.Linq;
    using ReactiveUI;

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
            this.SuccessCommand = ReactiveCommand.Create();
            this.SuccessCommand.Select(_ => this.Validate()).Subscribe(
            status =>
            {
                this.Status = status;
                if (status && this.ReadyToClose != null)
                {
                    this.ReadyToClose(this, EventArgs.Empty);
                }
            });

            this.CancelCommand = ReactiveCommand.Create();
            this.CancelCommand.Subscribe(
            status =>
            {
                this.Status = false;
                if (this.ReadyToClose != null)
                {
                    this.ReadyToClose(this, EventArgs.Empty);
                }
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
            get { return this.status; }
            set { this.RaiseAndSetIfChanged(ref this.status, value); }
        }

        /// <summary>
        /// Gets a command for success.
        /// </summary>
        public ReactiveCommand<object> SuccessCommand { get; private set; }

        /// <summary>
        /// Gets a command for canceling the operation.
        /// </summary>
        public ReactiveCommand<object> CancelCommand { get; private set; }

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
