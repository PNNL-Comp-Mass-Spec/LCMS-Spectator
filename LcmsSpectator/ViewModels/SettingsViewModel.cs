using System.Collections.Generic;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectatorModels.Config;

namespace LcmsSpectator.ViewModels
{
    public class SettingsViewModel: ViewModelBase
    {
        public List<ToleranceUnit> ToleranceUnits { get; set; }
        public double PrecursorIonTolerance { get; set; }
        public ToleranceUnit PrecursorIonToleranceUnit { get; set; }
        public double ProductIonTolerance { get; set; }
        public ToleranceUnit ProductIonToleranceUnit { get; set; }
        public double QValueThreshold { get; set; }
        public int ModificationsPerSequence { get; set; }
        public double IonCorrelationThreshold { get; set; }

        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }

        public SettingsViewModel()
        {
            ToleranceUnits = new List<ToleranceUnit> {ToleranceUnit.Ppm, ToleranceUnit.Th};

            PrecursorIonTolerance = IcParameters.Instance.PrecursorTolerancePpm.GetValue();
            PrecursorIonToleranceUnit = IcParameters.Instance.PrecursorTolerancePpm.GetUnit();

            ProductIonTolerance = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            ProductIonToleranceUnit = IcParameters.Instance.ProductIonTolerancePpm.GetUnit();

            QValueThreshold = IcParameters.Instance.QValueThreshold;

            IonCorrelationThreshold = IcParameters.Instance.IonCorrelationThreshold;

            ModificationsPerSequence = IcParameters.Instance.MaxDynamicModificationsPerSequence;

            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);
        }

        private void Save()
        {
            IcParameters.Instance.PrecursorTolerancePpm = new Tolerance(PrecursorIonTolerance, PrecursorIonToleranceUnit);
            IcParameters.Instance.ProductIonTolerancePpm = new Tolerance(ProductIonTolerance, ProductIonToleranceUnit);
            IcParameters.Instance.QValueThreshold = QValueThreshold;
            IcParameters.Instance.MaxDynamicModificationsPerSequence = ModificationsPerSequence;
            IcParameters.Instance.IonCorrelationThreshold = IonCorrelationThreshold;
        }

        private void Cancel()
        {
            
        }
    }
}
