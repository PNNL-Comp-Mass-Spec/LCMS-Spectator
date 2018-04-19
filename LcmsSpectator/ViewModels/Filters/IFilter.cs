// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFilter.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   An interface for selecting and validating a filter value.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Filters
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// An interface for selecting and validating a filter value.
    /// </summary>
    public interface IFilter
    {
        /// <summary>
        /// Event that is triggered when filter or cancel are executed.
        /// </summary>
        event EventHandler ReadyToClose;

        /// <summary>
        /// Gets the name of the filter.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the title text of the filter.
        /// </summary>
        string Title { get; }

        /// <summary>
        /// Gets the description text for the filter
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this item is selected
        /// </summary>
        bool Selected { get; set; }

        /// <summary>
        /// Gets the default possible values to filter by.
        /// </summary>
        IEnumerable<object> DefaultValues { get; }

        /// <summary>
        /// Gets a value indicating whether a valid filter has been selected.
        /// </summary>
        bool Status { get; }

        /// <summary>
        /// Filter a collection of data.
        /// </summary>
        /// <param name="data">The data to filter.</param>
        /// <returns>The filtered data.</returns>
        IEnumerable<object> Filter(IEnumerable<object> data);

        /// <summary>
        /// Reset the status of this filter.
        /// </summary>
        void ResetStatus();
    }
}
