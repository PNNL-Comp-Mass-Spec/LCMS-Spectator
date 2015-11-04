// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueFilterViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model for selecting and validating a filter value.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Filters
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using LcmsSpectator.DialogServices;
    using ReactiveUI;

    /// <summary>
    /// A view model for selecting and validating a filter value.
    /// </summary>
    public class MultiValueFilterViewModel : ReactiveObject, IFilter
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The function that filters a set of data.
        /// </summary>
        private readonly FilterFunction filter;

        /// <summary>
        /// The function that determines if the filter value is valid.
        /// </summary>
        private readonly Validate validator;

        /// <summary>
        /// A value indicating whether this item is selected 
        /// </summary>
        private bool selected;

        /// <summary>
        /// The selected value for the filter.
        /// </summary>
        private string value;

        /// <summary>
        /// The value selected from the Values list.
        /// </summary>
        private string selectedValue;

        /// <summary>
        /// The deliimeter to use for parsing a string containing a list.
        /// 0 = no delimiter.
        /// </summary>
        private char delimiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiValueFilterViewModel"/> class. 
        /// Initializes instance of the Filter
        /// </summary>
        /// <param name="name">The name of the filter.</param>
        /// <param name="title">The title of the filter.</param>
        /// <param name="description">The description text for the filter.</param>
        /// <param name="filter">The function that filters a set of data.</param>
        /// <param name="validator">Function that determines if the filter value is valid.</param>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        /// <param name="values">The default possible values to filter by.</param>
        /// <param name="delimiter">
        /// The deliimeter to use for parsing a string containing a list.
        /// 0 = no delimiter.
        /// </param>
        /// <param name="defaultValue">The default value to filter by.</param>
        public MultiValueFilterViewModel(
                                         string name,
                                         string title,
                                         string description,
                                         FilterFunction filter,
                                         Validate validator,
                                         IDialogService dialogService,
                                         IEnumerable<string> values = null,
                                         char delimiter = '\0',
                                         string defaultValue = "")
        {
            if (values == null)
            {
                values = new ReactiveList<string>();
            }

            this.Name = name;
            this.Title = title;
            this.Description = description;
            this.DefaultValues = values;
            this.filter = filter;
            this.validator = validator;

            var filterCommand = ReactiveCommand.Create();
            filterCommand.Subscribe(_ => this.FilterImplementation());
            this.FilterCommand = filterCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => this.CancelImplementation());
            this.CancelCommand = cancelCommand;

            var selectValueCommand = ReactiveCommand.Create(
                                         this.WhenAnyValue(x => x.Value)
                                             .Select(value => this.ParseValues(value))
                                             .Select(vals => vals.Any()));

            selectValueCommand.Subscribe(_ => this.SelectValueImplementation());
            this.SelectValueCommand = selectValueCommand;

            var removeValueCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.SelectedValue).Select(v => v != null));
            removeValueCommand.Subscribe(_ => this.RemoveValueImplementation());
            this.RemoveValueCommand = removeValueCommand;

            this.Value = defaultValue;
            this.Values = new ReactiveList<string>();
            this.delimiter = delimiter;
            this.dialogService = dialogService;
            this.Status = false;
        }

        /// <summary>
        /// Delegate defining interface of function that filters a set of data.
        /// </summary>
        /// <param name="data">The data to filter.</param>
        /// <param name="value">The value of the filter.</param>
        /// <returns>The filtered data.</returns>
        public delegate IEnumerable<object> FilterFunction(IEnumerable<object> data, IEnumerable<string> value); 

        /// <summary>
        /// Delegate defining interface of function that validates a filter value.
        /// </summary>
        /// <param name="value">Value of filter to validate.</param>
        /// <returns>A value indicating whether the filter is valid.</returns>
        public delegate bool Validate(object value);

        /// <summary>
        /// Event that is triggered when filter or cancel are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets the name of the filter.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the title text of the filter.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Gets the description text for the filter
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this item is selected.
        /// </summary>
        public bool Selected
        {
            get { return this.selected; }
            set { this.RaiseAndSetIfChanged(ref this.selected, value); }
        }

        /// <summary>
        /// Gets the default possible values to filter by.
        /// </summary>
        public IEnumerable<object> DefaultValues { get; private set; }

        /// <summary>
        /// Gets or sets the selected value for the filter.
        /// </summary>
        public string Value
        {
            get { return this.value; }
            set { this.RaiseAndSetIfChanged(ref this.value, value); }
        }

        /// <summary>
        /// Gets or sets the value selected from the Values list.
        /// </summary>
        public string SelectedValue
        {
            get { return this.selectedValue; }
            set { this.RaiseAndSetIfChanged(ref this.selectedValue, value); }
        }

        /// <summary>
        /// Gets the selected values for the filter.
        /// </summary>
        public ReactiveList<string> Values { get; private set; }

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
        /// Gets a command that inserts the selected value into the selected values list.
        /// </summary>
        public IReactiveCommand SelectValueCommand { get; private set; }

        /// <summary>
        /// Gets a command that removes the selected value into the selected values list.
        /// </summary>
        public IReactiveCommand RemoveValueCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a valid filter has been selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Filter a collection of data.
        /// </summary>
        /// <param name="data">The data to filter.</param>
        /// <returns>The filtered data.</returns>
        public IEnumerable<object> Filter(IEnumerable<object> data)
        {
            return this.filter(data, this.Values);
        }

        /// <summary>
        /// Reset the status of this filter.
        /// </summary>
        public void ResetStatus()
        {
            this.Status = false;
        }

        /// <summary>
        /// Implementation of FilterCommand.
        /// Sets status to true if a valid filter has been selected
        /// and triggers the ReadyToClose event.
        /// </summary>
        private void FilterImplementation()
        {
            if (this.Values.Any(v => !this.validator(v)))
            {
                this.dialogService.MessageBox("Invalid filter value.");
            }
            else
            {
                this.Status = true;
                if (this.ReadyToClose != null)
                {
                    this.ReadyToClose(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Implementation of CancelCommand.
        /// Sets status to false and triggers the ReadyToClose event.
        /// </summary>
        private void CancelImplementation()
        {
            this.Status = false;
            this.Selected = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation of SelectValueCommand.
        /// Gets a command that inserts the selected value into the selected values list.
        /// </summary>
        private void SelectValueImplementation()
        {
            var values = this.ParseValues(this.Value);
            values = values.Where(val => this.validator(val)).Where(val => !this.Values.Contains(val));
            if (value.Any())
            {
                foreach (var v in values)
                {
                    this.Values.Add(v);
                }
            }
            else
            {
                this.dialogService.MessageBox("No valid filter values selected.");
            }
        }

        /// <summary>
        /// Implementation of RemoveValueCommand.
        /// Removes the selected value into the selected values list.
        /// </summary>
        private void RemoveValueImplementation()
        {
            if (this.Values.Contains(this.SelectedValue))
            {
                this.Values.Remove(this.SelectedValue);
            }
        }

        /// <summary>
        /// Splits the value list on the delimiter if the delimiter isn't 0.
        /// </summary>
        /// <param name="valueList">The string to split.</param>
        /// <returns>List of split values.</returns>
        private IEnumerable<string> ParseValues(string valueList)
        {
            List<string> values;

            if (this.delimiter != '\0')
            {
                values = valueList.Split(this.delimiter).ToList();
            }
            else
            {
                values = new List<string> { valueList };
            }

            // Trim white space
            for (int i = 0; i < values.Count; i++)
            {
                values[i] = values[i].Trim();
            }

            // Get only values that are not in value list.
            values = values.Where(val => !string.IsNullOrWhiteSpace(val))
                           .Where(val => !this.Values.Contains(val))
                           .ToList();

            return values;
        }
    }
}
