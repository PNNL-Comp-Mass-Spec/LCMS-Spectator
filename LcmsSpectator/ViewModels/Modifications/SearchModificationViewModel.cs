// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SearchModificationViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for editing a SearchModification object.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Modifications
{
    using System;
    using System.Globalization;
    using System.Reactive.Linq;

    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;

    using ReactiveUI;

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
            this.Modifications = new ReactiveList<Modification>(Modification.CommonModifications);
            this.AminoAcidResidues = new ReactiveList<char>(AminoAcid.StandardAminoAcidCharacters) { '*' };
            this.SequenceLocations = new ReactiveList<SequenceLocation>
            {
                SequenceLocation.Everywhere, SequenceLocation.PeptideNTerm, SequenceLocation.PeptideCTerm,
                SequenceLocation.ProteinNTerm, SequenceLocation.ProteinCTerm
            };
            this.SelectedSequenceLocation = SequenceLocation.Everywhere;
            this.IsFixed = new ReactiveList<string> { "Fixed", "Optional" };
            this.FixedSelection = "Fixed";

            this.Modifications = IcParameters.Instance.RegisteredModifications;

            this.SelectedModification = this.Modifications[0];
            this.SelectedResidue = this.AminoAcidResidues[0];

            this.Remove = false;

            var removeModificationCommand = ReactiveCommand.Create();
            removeModificationCommand
                .Where(_ => this.dialogService.ConfirmationBox(
                    string.Format(
                        "Are you sure you would like to remove {0}[{1}]?",
                        this.SelectedResidue,
                        this.SelectedModification.Name),
                    "Remove Search Modification"))
                .Subscribe(_ => this.Remove = true);
            this.RemoveModificationCommand = removeModificationCommand;
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
            this.SearchModification = searchModification;
        }

        /// <summary>
        /// Gets or sets a list of all registered modifications.
        /// </summary>
        public ReactiveList<Modification> Modifications
        {
            get { return this.modifications; }
            set { this.RaiseAndSetIfChanged(ref this.modifications, value); }
        }

        /// <summary>
        /// Gets a list of all possible amino acid residues.
        /// </summary>
        public ReactiveList<char> AminoAcidResidues { get; private set; }

        /// <summary>
        /// Gets a list of all possible sequence locations.
        /// </summary>
        public ReactiveList<SequenceLocation> SequenceLocations { get; private set; }

        /// <summary>
        /// Gets a list of all possible fixed values.
        /// </summary>
        public ReactiveList<string> IsFixed { get; private set; }

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
        public IReactiveCommand RemoveModificationCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this modification should be removed.
        /// </summary>
        public bool Remove
        {
            get { return this.remove; }
            private set { this.RaiseAndSetIfChanged(ref this.remove, value); }
        }

        /// <summary>
        /// Gets or sets a search modification edited with this view model.
        /// </summary>
        public SearchModification SearchModification
        {
            get
            {
                if (this.SelectedModification == null)
                {
                    return null;
                }

                if (
                    !AminoAcid.StandardAminoAcidCharacters.Contains(
                        this.SelectedResidue.ToString(CultureInfo.InvariantCulture)) && this.SelectedResidue != '*')
                {
                    return null;
                }

                var isFixed = this.FixedSelection == "Fixed";
                return new SearchModification(this.SelectedModification, this.SelectedResidue, this.SelectedSequenceLocation, isFixed);
            }

            set
            {
                this.SelectedModification = value.Modification;
                this.SelectedResidue = value.TargetResidue;
                this.SelectedSequenceLocation = value.Location;
                this.FixedSelection = value.IsFixedModification ? "Fixed" : "Optional";
            }
        }
    }
}
