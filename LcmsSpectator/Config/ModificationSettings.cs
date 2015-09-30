using System.Collections.Generic;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.Config
{
    public class ModificationSettings
    {
        public ModificationSettings()
        {
            this.SearchModifications = new List<SearchModification>
            {
                new SearchModification(Modification.Carbamidomethylation, 'C', SequenceLocation.Everywhere, true)
            };

            this.LightModifications = new List<SearchModification>();
            this.HeavyModifications = new List<SearchModification>
            {
                new SearchModification(Modification.LysToHeavyLys, 'K', SequenceLocation.PeptideCTerm, true), 
                new SearchModification(Modification.ArgToHeavyArg, 'R', SequenceLocation.PeptideCTerm, true)
            };
        }

        /// <summary>
        /// Gets or sets the list of static modifications used by the application.
        /// </summary>
        public List<SearchModification> SearchModifications { get; set; }

        /// <summary>
        /// Gets or sets the list of modifications that are applied to the
        /// light sequence when ShowHeavy is selected.
        /// </summary>
        public List<SearchModification> LightModifications { get; set; }

        /// <summary>
        /// Gets or sets the list of modifications that are applied to the
        /// heavy sequence when ShowHeavy is selected.
        /// </summary>
        public List<SearchModification> HeavyModifications { get; set; }
    }
}
