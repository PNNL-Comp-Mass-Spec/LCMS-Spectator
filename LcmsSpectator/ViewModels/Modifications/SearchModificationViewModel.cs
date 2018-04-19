// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchModificationViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for editing a SearchModification object.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Reactive.Linq;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Modifications
{
    /// <summary>
    /// View model for editing a SearchModification object.
    /// </summary>
    public class SearchModificationViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// List of all registered modifications.
        /// </summary>
        private ReactiveList<Modification> modifications;

        /// <summary>
        /// A value indicating whether this modification should be removed.
        /// </summary>
        private bool remove;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchModificationViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public SearchModificationViewModel(IDialogService dialogService)
        {
            this.dialogService = dialogService;
            Modifications = new ReactiveList<Modification>(Modification.CommonModifications);
            AminoAcidResidues = new ReactiveList<char>(AminoAcid.StandardAminoAcidCharacters) { '*' };
            SequenceLocations = new ReactiveList<SequenceLocation>
            {
                SequenceLocation.Everywhere, SequenceLocation.PeptideNTerm, SequenceLocation.PeptideCTerm,
                SequenceLocation.ProteinNTerm, SequenceLocation.ProteinCTerm
            };
            SelectedSequenceLocation = SequenceLocation.Everywhere;
            IsFixed = new ReactiveList<string> { "Fixed", "Optional" };
            FixedSelection = "Fixed";

            Modifications = IcParameters.Instance.RegisteredModifications;

            SelectedModification = Modifications[0];
            SelectedResidue = AminoAcidResidues[0];

            Remove = false;

            var removeModificationCommand = ReactiveCommand.Create();
            removeModificationCommand
                .Where(_ => this.dialogService.ConfirmationBox(
                    string.Format(
                        "Are you sure you would like to remove {0}[{1}]?",
                        SelectedResidue,
                        SelectedModification.Name),
                    "Remove Search Modification"))
                .Subscribe(_ => Remove = true);
            RemoveModificationCommand = removeModificationCommand;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchModificationViewModel"/> class.
        /// Create new ModificationViewModel from searchModification
        /// </summary>
        /// <param name="searchModification">Search modification to create the SelectModificationViewModel from.</param>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public SearchModificationViewModel(SearchModification searchModification, IDialogService dialogService)
            : this(dialogService)
        {
            SearchModification = searchModification;
        }

        /// <summary>
        /// Gets or sets a list of all registered modifications.
        /// </summary>
        public ReactiveList<Modification> Modifications
        {
            get => modifications;
            set => this.RaiseAndSetIfChanged(ref modifications, value);
        }

        /// <summary>
        /// Gets a list of all possible amino acid residues.
        /// </summary>
        public ReactiveList<char> AminoAcidResidues { get; }

        /// <summary>
        /// Gets a list of all possible sequence locations.
        /// </summary>
        public ReactiveList<SequenceLocation> SequenceLocations { get; }

        /// <summary>
        /// Gets a list of all possible fixed values.
        /// </summary>
        public ReactiveList<string> IsFixed { get; }

        /// <summary>
        /// Gets or sets the modification selected from modification list.
        /// </summary>
        public Modification SelectedModification { get; set; }

        /// <summary>
        /// Gets or sets the residue selected from residue list.
        /// </summary>
        public char SelectedResidue { get; set; }

        /// <summary>
        /// Gets or sets the sequence location selected from the sequence location list.
        /// </summary>
        public SequenceLocation SelectedSequenceLocation { get; set; }

        /// <summary>
        /// Gets or sets the fixed value selected from the IsFixed list.
        /// </summary>
        public string FixedSelection { get; set; }

        /// <summary>
        /// Gets a command that removes this modification from the list.
        /// </summary>
        public IReactiveCommand RemoveModificationCommand { get; }

        /// <summary>
        /// Gets a value indicating whether this modification should be removed.
        /// </summary>
        public bool Remove
        {
            get => remove;
            private set => this.RaiseAndSetIfChanged(ref remove, value);
        }

        /// <summary>
        /// Gets or sets a search modification edited with this view model.
        /// </summary>
        public SearchModification SearchModification
        {
            get
            {
                if (SelectedModification == null)
                {
                    return null;
                }

                if (
                    !AminoAcid.StandardAminoAcidCharacters.Contains(
                        SelectedResidue.ToString(CultureInfo.InvariantCulture)) && SelectedResidue != '*')
                {
                    return null;
                }

                var isFixed = FixedSelection == "Fixed";
                return new SearchModification(SelectedModification, SelectedResidue, SelectedSequenceLocation, isFixed);
            }

            set
            {
                SelectedModification = value.Modification;
                SelectedResidue = value.TargetResidue;
                SelectedSequenceLocation = value.Location;
                FixedSelection = value.IsFixedModification ? "Fixed" : "Optional";
            }
        }
    }
}
