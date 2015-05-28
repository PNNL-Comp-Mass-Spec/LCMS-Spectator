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

    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;

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
            this.LightModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };
            this.HeavyModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };

            this.LightModifications.AddRange(IcParameters.Instance.LightModifications.Select(mod => new SearchModificationViewModel(dialogService)
            {
                SelectedResidue = mod.TargetResidue,
                SelectedModification = mod.Modification,
                SelectedSequenceLocation = mod.Location,
                FixedSelection = "Fixed"
            }));

            this.HeavyModifications.AddRange(IcParameters.Instance.HeavyModifications.Select(mod => new SearchModificationViewModel(dialogService)
            {
                SelectedResidue = mod.TargetResidue,
                SelectedModification = mod.Modification,
                SelectedSequenceLocation = mod.Location,
                FixedSelection = "Fixed"
            }));

            var addLightModificationCommand = ReactiveCommand.Create();
            addLightModificationCommand.Subscribe(_ => this.LightModifications.Add(new SearchModificationViewModel(dialogService)));
            this.AddLightModificationCommand = addLightModificationCommand;

            var addHeavyModificationCommand = ReactiveCommand.Create();
            addHeavyModificationCommand.Subscribe(_ => this.HeavyModifications.Add(new SearchModificationViewModel(dialogService)));
            this.AddHeavyModificationCommand = addHeavyModificationCommand;

            this.LightModifications.ItemChanged.Where(x => x.PropertyName == "Remove")
                .Select(x => x.Sender).Where(sender => sender.Remove)
                .Subscribe(searchMod => this.LightModifications.Remove(searchMod));

            this.HeavyModifications.ItemChanged.Where(x => x.PropertyName == "Remove")
                .Select(x => x.Sender).Where(sender => sender.Remove)
                .Subscribe(searchMod => this.HeavyModifications.Remove(searchMod));
        }

        /// <summary>
        /// Gets a list of light modifications.
        /// </summary>
        public ReactiveList<SearchModificationViewModel> LightModifications { get; private set; }

        /// <summary>
        /// Gets a list of heavy modifications.
        /// </summary>
        public ReactiveList<SearchModificationViewModel> HeavyModifications { get; private set; }

        /// <summary>
        /// Gets a command that adds a new modification to the light modifications list.
        /// </summary>
        public IReactiveCommand AddLightModificationCommand { get; private set; }

        /// <summary>
        /// Gets a command that adds a new modification to the heavy modifications list.
        /// </summary>
        public IReactiveCommand AddHeavyModificationCommand { get; private set; }

        /// <summary>
        /// Save the selected heavy and light modifications to the settings.
        /// </summary>
        public void Save()
        {
            IcParameters.Instance.LightModifications = new ReactiveList<SearchModification>(this.LightModifications.Select(searchModVm => searchModVm.SearchModification));
            IcParameters.Instance.HeavyModifications = new ReactiveList<SearchModification>(this.HeavyModifications.Select(searchModVm => searchModVm.SearchModification));
        }
    }
}
