using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectator.Config
{
    public class IonTypeSettings
    {
        public IonTypeSettings()
        {
            this.AutomaticallySelectIonTypes = true;
            this.CidHcdIonTypes = new List<BaseIonType> { BaseIonType.B, BaseIonType.Y };
            this.EtdIonTypes = new List<BaseIonType> { BaseIonType.C, BaseIonType.Z };
        }

        /// <summary>
        /// Gets a value that indicates whether ion types should automatically be selected base on the activation method
        /// of the currently select MS/MS spectrum.
        /// </summary>
        public bool AutomaticallySelectIonTypes { get; set; }

        /// <summary>
        /// Gets the ion types that are automatically selected when the activation method of the 
        /// selected MS/MS spectrum is CID or HCD.
        /// </summary>
        public List<BaseIonType> CidHcdIonTypes { get; set; }

        /// <summary>
        /// Gets the ion types that are automatically selected when the activation method of the 
        /// selected MS/MS spectrum is ETD.
        /// </summary>
        public List<BaseIonType> EtdIonTypes { get; set; }

        /// <summary>
        /// Set CidHcd ion types from space-separated string.
        /// </summary>
        /// <param name="ionTypeString">Space separated string.</param>
        public void SetCidHcdIonTypes(string ionTypeString)
        {
            this.CidHcdIonTypes = this.IonTypeStringParse(ionTypeString);
        }

        /// <summary>
        /// Set ETD ion types from space-separated string.
        /// </summary>
        /// <param name="ionTypeString">Space separated string.</param>
        public void SetEtdHcdIonTypes(string ionTypeString)
        {
            this.EtdIonTypes = this.IonTypeStringParse(ionTypeString);
        }

        /// <summary>
        /// Get string containing list of ion types to be highlighted for CID and HCD spectra.
        /// </summary>
        /// <returns>String containing ion type symbols separated by spaces.</returns>
        public string GetCidHcdIonTypes()
        {
            return this.CidHcdIonTypes.Aggregate(string.Empty, (current, ionType) => current + ionType.Symbol + " ");
        }

        /// <summary>
        /// Get string containing list of ion types to be highlighted for ETD spectra.
        /// </summary>
        /// <returns>String containing ion type symbols separated by spaces.</returns>
        public string GetEtdIonTypes()
        {
            return this.EtdIonTypes.Aggregate(string.Empty, (current, ionType) => current + ionType.Symbol + " ");
        }

        /// <summary>
        /// Parse a list of base ion type symbols separated by spaces.
        /// </summary>
        /// <param name="str">String containing ion type symbols separated by spaces.</param>
        /// <returns>List of BaseIonTypes representing each ion type in the string.</returns>
        private List<BaseIonType> IonTypeStringParse(string str)
        {
            var parts = str.Split(' ');
            var ionNames = new Dictionary<string, BaseIonType>
            {
                { BaseIonType.A.Symbol, BaseIonType.A },
                { BaseIonType.B.Symbol, BaseIonType.B },
                { BaseIonType.C.Symbol, BaseIonType.C },
                { BaseIonType.X.Symbol, BaseIonType.X },
                { BaseIonType.Y.Symbol, BaseIonType.Y },
                { BaseIonType.Z.Symbol, BaseIonType.Z }
            };

            return (from part in parts where ionNames.ContainsKey(part) select ionNames[part]).ToList();
        }
    }
}
