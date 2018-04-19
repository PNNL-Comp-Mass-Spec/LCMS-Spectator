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
    using System;
    using System.Linq;
    using System.Reactive.Linq;

    using InformedProteomics.Backend.Data.Sequence;

    using Config;
    using DialogServices;

    using ReactiveUI;

    /// <summary>
    /// View model for selecting heavy modifications for light and heavy peptides.
    /// </summary>
    public class HeavyModificationsViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeavyModificationsViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public HeavyModificationsViewModel(IDialogService dialogService)
        {
            LightModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };
            HeavyModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };

            LightModifications.AddRange(IcParameters.Instance.LightModifications.Select(mod => new SearchModificationViewModel(dialogService)
            {
                SelectedResidue = mod.TargetResidue,
                SelectedModification = mod.Modification,
                SelectedSequenceLocation = mod.Location,
                FixedSelection = "Fixed"
            }));

            HeavyModifications.AddRange(IcParameters.Instance.HeavyModifications.Select(mod => new SearchModificationViewModel(dialogService)
            {
                SelectedResidue = mod.TargetResidue,
                SelectedModification = mod.Modification,
                SelectedSequenceLocation = mod.Location,
                FixedSelection = "Fixed"
            }));

            var addLightModificationCommand = ReactiveCommand.Create();
            addLightModificationCommand.Subscribe(_ => LightModifications.Add(new SearchModificationViewModel(dialogService)));
            AddLightModificationCommand = addLightModificationCommand;

            var addHeavyModificationCommand = ReactiveCommand.Create();
            addHeavyModificationCommand.Subscribe(_ => HeavyModifications.Add(new SearchModificationViewModel(dialogService)));
            AddHeavyModificationCommand = addHeavyModificationCommand;

            LightModifications.ItemChanged.Where(x => x.PropertyName == "Remove")
                .Select(x => x.Sender).Where(sender => sender.Remove)
                .Subscribe(searchMod => LightModifications.Remove(searchMod));

            HeavyModifications.ItemChanged.Where(x => x.PropertyName == "Remove")
                .Select(x => x.Sender).Where(sender => sender.Remove)
                .Subscribe(searchMod => HeavyModifications.Remove(searchMod));
        }

        /// <summary>
        /// Gets a list of light modifications.
        /// </summary>
        public ReactiveList<SearchModificationViewModel> LightModifications { get; }

        /// <summary>
        /// Gets a list of heavy modifications.
        /// </summary>
        public ReactiveList<SearchModificationViewModel> HeavyModifications { get; }

        /// <summary>
        /// Gets a command that adds a new modification to the light modifications list.
        /// </summary>
        public IReactiveCommand AddLightModificationCommand { get; }

        /// <summary>
        /// Gets a command that adds a new modification to the heavy modifications list.
        /// </summary>
        public IReactiveCommand AddHeavyModificationCommand { get; }

        /// <summary>
        /// Save the selected heavy and light modifications to the settings.
        /// </summary>
        public void Save()
        {
            IcParameters.Instance.LightModifications = new ReactiveList<SearchModification>(LightModifications.Select(searchModVm => searchModVm.SearchModification));
            IcParameters.Instance.HeavyModifications = new ReactiveList<SearchModification>(HeavyModifications.Select(searchModVm => searchModVm.SearchModification));
        }
    }
}
