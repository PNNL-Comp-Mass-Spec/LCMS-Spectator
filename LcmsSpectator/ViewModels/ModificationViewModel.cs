using System;
using System.Globalization;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class ModificationViewModel: ReactiveObject
    {
        public ReactiveList<Modification> Modifications { get; private set; }
        public ReactiveList<char> AminoAcidResidues { get; private set; }
        public ReactiveList<SequenceLocation> SequenceLocations { get; private set; }
        public ReactiveList<string> IsFixed { get; private set; } 

        public Modification SelectedModification { get; set; }
        public char SelectedResidue { get; set; }
        public SequenceLocation SelectedSequenceLocation { get; set; }
        public string FixedSelection { get; set; }

        public ReactiveCommand<Object> RemoveModificationCommand { get; private set; }

        /// <summary>
        /// Create new ModificationViewModel with default values for all properties.
        /// </summary>
        public ModificationViewModel()
        {
            Modifications = new ReactiveList<Modification>(Modification.CommonModifications);
            AminoAcidResidues = new ReactiveList<char>(AminoAcid.StandardAminoAcidCharacters) { '*' };
            SequenceLocations = new ReactiveList<SequenceLocation>
            {
                SequenceLocation.Everywhere, SequenceLocation.PeptideNTerm, SequenceLocation.PeptideCTerm
            };
            SelectedSequenceLocation = SequenceLocation.Everywhere;
            IsFixed = new ReactiveList<string> { "Fixed", "Optional" };
            FixedSelection = "Fixed";

            RemoveModificationCommand = ReactiveCommand.Create();
        }

        /// <summary>
        /// Create new ModificationViewModel from searchModification
        /// </summary>
        /// <param name="searchModification"></param>
        public ModificationViewModel(SearchModification searchModification): this()
        {
            SearchModification = searchModification;
        }

        public SearchModification SearchModification
        {
            get
            {
                if (SelectedModification == null) return null;
                if (!AminoAcid.StandardAminoAcidCharacters.Contains(SelectedResidue.ToString(CultureInfo.InvariantCulture))) return null;
                var isFixed = (FixedSelection == "Fixed");
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
