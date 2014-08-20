using System;
using System.Collections.ObjectModel;
using System.Globalization;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.ViewModels
{
    public class ModificationViewModel: ViewModelBase
    {
        public ObservableCollection<Modification> Modifications { get; private set; }
        public ObservableCollection<char> AminoAcidResidues { get; private set; }
        public ObservableCollection<SequenceLocation> SequenceLocations { get; private set; }
        public ObservableCollection<string> IsFixed { get; private set; } 

        public Modification SelectedModification { get; set; }
        public char SelectedResidue { get; set; }
        public SequenceLocation SelectedSequenceLocation { get; set; }
        public string FixedSelection { get; set; }

        public DelegateCommand RemoveModificationCommand { get; private set; }

        public event EventHandler RequestModificationRemoval;

        /// <summary>
        /// Create new ModificationViewModel with default values for all properties.
        /// </summary>
        public ModificationViewModel()
        {
            Modifications = new ObservableCollection<Modification>(Modification.CommonModifications);
            AminoAcidResidues = new ObservableCollection<char>(AminoAcid.StandardAminoAcidCharacters) { '*' };
            SequenceLocations = new ObservableCollection<SequenceLocation>
            {
                SequenceLocation.Everywhere, SequenceLocation.PeptideNTerm, SequenceLocation.PeptideCTerm
            };
            SelectedSequenceLocation = SequenceLocation.Everywhere;
            IsFixed = new ObservableCollection<string> { "Fixed", "Optional" };
            FixedSelection = "Fixed";

            RemoveModificationCommand = new DelegateCommand(() =>
            {
                if (RequestModificationRemoval != null) RequestModificationRemoval(this, EventArgs.Empty);
            });
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
