// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseIonTypeViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for a BaseIonType model.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Data
{
    using InformedProteomics.Backend.Data.Spectrometry;
    using ReactiveUI;

    /// <summary>
    /// View model for a BaseIonType model.
    /// </summary>
    public class BaseIonTypeViewModel : ReactiveObject
    {
        /// <summary>
        /// The BaseIonType model for this view model.
        /// </summary>
        private BaseIonType baseIonType;

        /// <summary>
        /// A value indicating whether this is selected.
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// Gets or sets the BaseIonType model for this view model.
        /// </summary>
        public BaseIonType BaseIonType
        {
            get => baseIonType;
            set => this.RaiseAndSetIfChanged(ref baseIonType, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this is selected.
        /// </summary>
        public bool IsSelected
        {
            get => isSelected;
            set => this.RaiseAndSetIfChanged(ref isSelected, value);
        }
    }
}
