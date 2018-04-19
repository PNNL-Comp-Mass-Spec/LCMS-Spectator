// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModificationViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model representing a modification that can be selected or unselected.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using InformedProteomics.Backend.Data.Sequence;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Modifications
{
    /// <summary>
    /// A view model representing a modification that can be selected or unselected.
    /// </summary>
    public class ModificationViewModel : ReactiveObject
    {
        /// <summary>
        /// The modification that this view model represents.
        /// </summary>
        private Modification modification;

        /// <summary>
        /// A value indicating whether this modification has been selected.
        /// </summary>
        private bool selected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModificationViewModel"/> class.
        /// </summary>
        /// <param name="modification">The modification that this view model represents.</param>
        public ModificationViewModel(Modification modification)
        {
            Modification = modification;
        }

        /// <summary>
        /// Gets or sets the modification that this view model represents.
        /// </summary>
        public Modification Modification
        {
            get => modification;
            set => this.RaiseAndSetIfChanged(ref modification, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this modification has been selected.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set => this.RaiseAndSetIfChanged(ref selected, value);
        }
    }
}
