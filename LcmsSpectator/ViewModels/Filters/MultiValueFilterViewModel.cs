// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MultiValueFilterViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model for selecting and validating a filter value.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Filters
{
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
        /// The delimiter to use for parsing a string containing a list.
        /// 0 = no delimiter.
        /// </summary>
        private readonly char delimiter;

        /// <summary>
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public MultiValueFilterViewModel()
        {
        }

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
        /// The delimiter to use for parsing a string containing a list.
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

            Name = name;
            Title = title;
            Description = description;
            DefaultValues = values;
            this.filter = filter;
            this.validator = validator;

            FilterCommand = ReactiveCommand.Create(FilterImplementation);
            CancelCommand = ReactiveCommand.Create(CancelImplementation);

            SelectValueCommand = ReactiveCommand.Create(SelectValueImplementation,
                                         this.WhenAnyValue(x => x.Value)
                                             .Select(ParseValues)
                                             .Select(vals => vals.Any()));

            RemoveValueCommand = ReactiveCommand.Create(RemoveValueImplementation, this.WhenAnyValue(x => x.SelectedValue).Select(v => v != null));

            Value = defaultValue;
            Values = new ReactiveList<string>();
            this.delimiter = delimiter;
            this.dialogService = dialogService;
            Status = false;
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
        public string Name { get; }

        /// <summary>
        /// Gets the title text of the filter.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the description text for the filter
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this item is selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set => this.RaiseAndSetIfChanged(ref selected, value);
        }

        /// <summary>
        /// Gets the default possible values to filter by.
        /// </summary>
        public IEnumerable<object> DefaultValues { get; }

        /// <summary>
        /// Gets or sets the selected value for the filter.
        /// </summary>
        public string Value
        {
            get => value;
            set => this.RaiseAndSetIfChanged(ref this.value, value);
        }

        /// <summary>
        /// Gets or sets the value selected from the Values list.
        /// </summary>
        public string SelectedValue
        {
            get => selectedValue;
            set => this.RaiseAndSetIfChanged(ref selectedValue, value);
        }

        /// <summary>
        /// Gets the selected values for the filter.
        /// </summary>
        public ReactiveList<string> Values { get; }

        /// <summary>
        /// Gets a command that sets status to true if a valid filter has been selected
        /// and triggers the ReadyToClose event.
        /// </summary>
        public ReactiveCommand<Unit, Unit> FilterCommand { get; }

        /// <summary>
        /// Gets a command that sets status to false and triggers the ReadyToClose event.
        /// </summary>
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        /// <summary>
        /// Gets a command that inserts the selected value into the selected values list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> SelectValueCommand { get; }

        /// <summary>
        /// Gets a command that removes the selected value into the selected values list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> RemoveValueCommand { get; }

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
            return filter(data, Values);
        }

        /// <summary>
        /// Reset the status of this filter.
        /// </summary>
        public void ResetStatus()
        {
            Status = false;
        }

        /// <summary>
        /// Implementation of FilterCommand.
        /// Sets status to true if a valid filter has been selected
        /// and triggers the ReadyToClose event.
        /// </summary>
        private void FilterImplementation()
        {
            if (Values.Any(v => !validator(v)))
            {
                dialogService.MessageBox("Invalid filter value.");
            }
            else
            {
                Status = true;
                ReadyToClose?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation of CancelCommand.
        /// Sets status to false and triggers the ReadyToClose event.
        /// </summary>
        private void CancelImplementation()
        {
            Status = false;
            Selected = false;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementation of SelectValueCommand.
        /// Gets a command that inserts the selected value into the selected values list.
        /// </summary>
        private void SelectValueImplementation()
        {
            var values = ParseValues(Value);
            values = values.Where(val => validator(val)).Where(val => !Values.Contains(val));
            if (value.Length > 0)
            {
                Values.AddRange(values);
            }
            else
            {
                dialogService.MessageBox("No valid filter values selected.");
            }
        }

        /// <summary>
        /// Implementation of RemoveValueCommand.
        /// Removes the selected value into the selected values list.
        /// </summary>
        private void RemoveValueImplementation()
        {
            if (Values.Contains(SelectedValue))
            {
                Values.Remove(SelectedValue);
            }
        }

        /// <summary>
        /// Splits the value list on the delimiter if the delimiter isn't 0.
        /// </summary>
        /// <param name="valueList">The string to split.</param>
        /// <returns>List of split values.</returns>
        private IEnumerable<string> ParseValues(string valueList)
        {
            IEnumerable<string> parsedValues;

            if (delimiter != '\0')
            {
                parsedValues = valueList.Split(delimiter).Select(val => val.Trim());
            }
            else
            {
                var parsed = new List<string>();
                parsedValues = parsed;
                if (!string.IsNullOrWhiteSpace(valueList))
                {
                    parsed.Add(valueList.Trim());
                }
            }

            // Get only values that are not in value list.
            parsedValues = parsedValues.Where(val => !string.IsNullOrWhiteSpace(val))
                                       .Where(val => !Values.Contains(val));

            return parsedValues;
        }
    }
}
