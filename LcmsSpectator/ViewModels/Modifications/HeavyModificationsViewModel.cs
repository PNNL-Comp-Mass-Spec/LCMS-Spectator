// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeavyModificationsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting heavy modifications for light and heavy peptides.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Modifications
{
    using System.Collections;

    using InformedProteomics.Backend.Data.Sequence;

    using LcmsSpectator.Config;

    using ReactiveUI;

    /// <summary>
    /// View model for selecting heavy modifications for light and heavy peptides.
    /// </summary>
    public class HeavyModificationsViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeavyModificationsViewModel"/> class.
        /// </summary>
        public HeavyModificationsViewModel()
        {
            this.Modifications = new ReactiveList<Modification>
            {
                Modification.LysToHeavyLys,
                Modification.ArgToHeavyArg
            };

            this.SelectedLightModifications = IcParameters.Instance.LightModifications;
            this.SelectedHeavyModifications = IcParameters.Instance.HeavyModifications;
        }

        /// <summary>
        /// Gets a list of possible heavy modifications.
        /// </summary>
        public ReactiveList<Modification> Modifications { get; private set; }

        /// <summary>
        /// Gets or sets the list of heavy modifications that are selected for light peptides.
        /// </summary>
        public IList SelectedLightModifications { get; set; }

        /// <summary>
        /// Gets or sets the list of heavy modifications that are selected for heavy peptides.
        /// </summary>
        public IList SelectedHeavyModifications { get; set; }

        /// <summary>
        /// Save the selected heavy and light modifications to the settings.
        /// </summary>
        public void Save()
        {
            ////IcParameters.Instance.LightModifications = new ReactiveList<ModificationViewModel>(SelectedLightModifications.Cast<ModificationViewModel>().ToList());
            ////IcParameters.Instance.HeavyModifications = new ReactiveList<ModificationViewModel>(SelectedHeavyModifications.Cast<ModificationViewModel>().ToList());
        }
    }
}
