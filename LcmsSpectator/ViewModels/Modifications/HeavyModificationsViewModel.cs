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
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;

    using InformedProteomics.Backend.Data.Sequence;

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
        /// <param name="modifications">The registered modifications to pull from.</param>
        /// <param name="lightModifications">The modifications for light sequences</param>
        /// <param name="heavyModifications">The modifications for heavy sequences.</param>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public HeavyModificationsViewModel(IEnumerable<Modification> modifications, 
                                           IEnumerable<SearchModification> lightModifications,
                                           IEnumerable<SearchModification> heavyModifications,
                                           IDialogService dialogService = null)
        {
            dialogService = dialogService ?? new DialogService();
            this.LightModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };
            this.HeavyModifications = new ReactiveList<SearchModificationViewModel> { ChangeTrackingEnabled = true };

            this.LightModifications.AddRange(lightModifications.Select(mod => new SearchModificationViewModel(modifications, dialogService)
            {
                SelectedResidue = mod.TargetResidue,
                SelectedModification = mod.Modification,
                SelectedSequenceLocation = mod.Location,
                FixedSelection = "Fixed"
            }));

            this.HeavyModifications.AddRange(heavyModifications.Select(mod => new SearchModificationViewModel(modifications, dialogService)
            {
                SelectedResidue = mod.TargetResidue,
                SelectedModification = mod.Modification,
                SelectedSequenceLocation = mod.Location,
                FixedSelection = "Fixed"
            }));

            this.AddLightModificationCommand = ReactiveCommand.Create();
            this.AddLightModificationCommand.Subscribe(_ => this.LightModifications.Add(new SearchModificationViewModel(modifications, dialogService)));

            this.AddHeavyModificationCommand = ReactiveCommand.Create();
            this.AddHeavyModificationCommand.Subscribe(_ => this.HeavyModifications.Add(new SearchModificationViewModel(modifications, dialogService)));

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
        public ReactiveCommand<object> AddLightModificationCommand { get; private set; }

        /// <summary>
        /// Gets a command that adds a new modification to the heavy modifications list.
        /// </summary>
        public ReactiveCommand<object> AddHeavyModificationCommand { get; private set; }
    }
}
