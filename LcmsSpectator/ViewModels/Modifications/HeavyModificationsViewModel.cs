// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HeavyModificationsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting heavy modifications for light and heavy peptides.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Modifications
{
    /// <summary>
    /// View model for selecting heavy modifications for light and heavy peptides.
    /// </summary>
    public class HeavyModificationsViewModel : ReactiveObject
    {
        /// <summary>
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public HeavyModificationsViewModel()
        {
        }

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

            AddLightModificationCommand = ReactiveCommand.Create(() => LightModifications.Add(new SearchModificationViewModel(dialogService)));
            AddHeavyModificationCommand = ReactiveCommand.Create(() => HeavyModifications.Add(new SearchModificationViewModel(dialogService)));

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
        public ReactiveCommand<Unit, Unit> AddLightModificationCommand { get; }

        /// <summary>
        /// Gets a command that adds a new modification to the heavy modifications list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddHeavyModificationCommand { get; }

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
