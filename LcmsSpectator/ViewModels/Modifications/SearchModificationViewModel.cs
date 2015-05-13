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
    using System.Globalization;

    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;

    using ReactiveUI;

    /// <summary>
    /// View model for editing a SearchModification object.
    /// </summary>
    public class SearchModificationViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchModificationViewModel"/> class. 
        /// </summary>
        public SearchModificationViewModel()
        {
            this.Modifications = new ReactiveList<Modification>(Modification.CommonModifications);
            this.AminoAcidResidues = new ReactiveList<char>(AminoAcid.StandardAminoAcidCharacters) { '*' };
            this.SequenceLocations = new ReactiveList<SequenceLocation>
            {
                SequenceLocation.Everywhere, SequenceLocation.PeptideNTerm, SequenceLocation.PeptideCTerm
            };
            this.SelectedSequenceLocation = SequenceLocation.Everywhere;
            this.IsFixed = new ReactiveList<string> { "Fixed", "Optional" };
            this.FixedSelection = "Fixed";

            this.RemoveModificationCommand = ReactiveCommand.Create();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchModificationViewModel"/> class. 
        /// Create new ModificationViewModel from searchModification
        /// </summary>
        /// <param name="searchModification">Search modification to create the SelectModificationViewModel from.</param>
        public SearchModificationViewModel(SearchModification searchModification)
            : this()
        {
            this.SearchModification = searchModification;
        }

        /// <summary>
        /// Gets a list of all registered modifications.
        /// </summary>
        public ReactiveList<Modification> Modifications { get; private set; }

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
        public ReactiveCommand<object> RemoveModificationCommand { get; private set; }

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
                        this.SelectedResidue.ToString(CultureInfo.InvariantCulture)))
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
