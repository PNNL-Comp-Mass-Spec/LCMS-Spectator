// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FilterViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model for selecting and validating a filter value.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using LcmsSpectator.DialogServices;
    using ReactiveUI;
    
    /// <summary>
    /// A view model for selecting and validating a filter value.
    /// </summary>
    public class FilterViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterViewModel"/> class. 
        /// Initializes instance of the Filter
        /// </summary>
        /// <param name="title">The title of the filter.</param>
        /// <param name="description">The description text for the filter.</param>
        /// <param name="defaultValue">The default value to filter by.</param>
        /// <param name="values">The default possible values to filter by.</param>
        /// <param name="validator">Function that determines if the filter value is valid.</param>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public FilterViewModel(string title, string description, string defaultValue, List<string> values, Validate validator, IDialogService dialogService)
        {
            this.Title = title;
            this.Description = description;
            this.Values = values;
            this.Validator = validator;
            var filterCommand = ReactiveCommand.Create();
            filterCommand.Subscribe(_ => this.FilterImplementation());
            this.FilterCommand = filterCommand;
            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;
            this.SelectedValue = defaultValue;
            this.dialogService = dialogService;
            this.Status = false;
        }

        /// <summary>
        /// Delegate defining interface of function that validates a filter.
        /// </summary>
        /// <param name="value">Value of filter to validate.</param>
        /// <returns>A value indicating whether the filter is valid.</returns>
        public delegate bool Validate(object value);

        /// <summary>
        /// Event that is triggered when filter or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets the title of the filter.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the description text for the filter
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets the default possible values to filter by.
        /// </summary>
        public List<string> Values { get; private set; }

        /// <summary>
        /// Gets or sets the selected value for the filter.
        /// </summary>
        public string SelectedValue { get; set; }

        /// <summary>
        /// Gets a command that sets status to true if a valid filter has been selected
        /// and triggers the ReadyToClose event.
        /// </summary>
        public IReactiveCommand FilterCommand { get; private set; }

        /// <summary>
        /// Gets a command that sets status to false and triggers the ReadyToClose event.
        /// </summary>
        public IReactiveCommand CancelCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a valid filter has been selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets the function that determines if the filter value is valid.
        /// </summary>
        public Validate Validator { get; private set; }

        /// <summary>
        /// Implementation of FilterCommand.
        /// Sets status to true if a valid filter has been selected
        /// and triggers the ReadyToClose event.
        /// </summary>
        private void FilterImplementation()
        {
            if (this.Validator(this.SelectedValue))
            {
                this.Status = true;
                if (this.ReadyToClose != null)
                {
                    this.ReadyToClose(this, EventArgs.Empty);
                }
            }
            else
            {
                this.dialogService.MessageBox("Invalid filter value.");
            }
        }

        /// <summary>
        /// Implementation of CancelCommand.
        /// Sets status to false and triggers the ReadyToClose event.
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
