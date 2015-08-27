namespace LcmsSpectator.ViewModels.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.Data.Spectrometry;
    using LcmsSpectator.Config;
    using ReactiveUI;
    
    public class IonTypeSettingsViewModel : ReactiveObject
    {
        /// <summary>
        /// The Cid/Hcd ion types as a space-separated string.
        /// </summary>
        private string cidHcdIonTypeText;

        /// <summary>
        /// The ETD ion types as a space-separated string.
        /// </summary>
        private string etdIonTypeText;

        public IonTypeSettingsViewModel(IonTypeSettings ionTypeSettings)
        {
            this.CidHcdIonTypes = new ReactiveList<BaseIonType>(ionTypeSettings.CidHcdIonTypes);
            this.CidHcdIonTypeText = this.CidHcdIonTypes.Aggregate(string.Empty, (current, ionType) => current + ionType.Symbol + " ");

            this.EtdIonTypes = new ReactiveList<BaseIonType>(ionTypeSettings.EtdIonTypes);
            this.EtdIonTypeText = this.EtdIonTypes.Aggregate(string.Empty, (current, ionType) => current + ionType.Symbol + " ");

            this.WhenAnyValue(x => x.CidHcdIonTypeText)
                .Subscribe(t =>
                {
                    this.CidHcdIonTypes.Clear();
                    this.CidHcdIonTypes.AddRange(this.IonTypeStringParse(t));
                });

            this.WhenAnyValue(x => x.EtdIonTypeText)
                .Subscribe(t =>
                {
                    this.EtdIonTypes.Clear();
                    this.EtdIonTypes.AddRange(this.IonTypeStringParse(t));
                });
        }

        /// <summary>
        /// Gets a value that indicates whether ion types should automatically be selected base on the activation method
        /// of the currently select MS/MS spectrum.
        /// </summary>
        public bool AutomaticallySelectIonTypes { get; private set; }

        /// <summary>
        /// Gets the ion types that are automatically selected when the activation method of the 
        /// selected MS/MS spectrum is CID or HCD.
        /// </summary>
        public ReactiveList<BaseIonType> CidHcdIonTypes { get; private set; }

        /// <summary>
        /// Gets or sets the Cid/Hcd ion types as a space separated string.
        /// </summary>
        public string CidHcdIonTypeText
        {
            get { return this.cidHcdIonTypeText; }
            set { this.RaiseAndSetIfChanged(ref this.cidHcdIonTypeText, value); }
        }
        
        /// <summary>
        /// Gets the ion types that are automatically selected when the activation method of the 
        /// selected MS/MS spectrum is ETD.
        /// </summary>
        public ReactiveList<BaseIonType> EtdIonTypes { get; set; }

        /// <summary>
        /// Gets or sets the ETD ion types as a space separated string.
        /// </summary>
        public string EtdIonTypeText
        {
            get { return this.etdIonTypeText; }
            set { this.RaiseAndSetIfChanged(ref this.etdIonTypeText, value); }
        }

        /// <summary>
        /// Parse a list of base ion type symbols separated by spaces.
        /// </summary>
        /// <param name="str">String containing ion type symbols separated by spaces.</param>
        /// <returns>List of BaseIonTypes representing each ion type in the string.</returns>
        private IEnumerable<BaseIonType> IonTypeStringParse(string str)
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

            return (from part in parts where ionNames.ContainsKey(part) select ionNames[part]);
        }
    }
}
