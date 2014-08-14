using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.DialogServices;
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
        public int PointsToSmooth { get; set; }

        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }

        public event EventHandler ReadyToClose;

        public bool Status { get; private set; }

        public SettingsViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            ToleranceUnits = new List<ToleranceUnit> {ToleranceUnit.Ppm, ToleranceUnit.Th};
            PrecursorIonTolerance = IcParameters.Instance.PrecursorTolerancePpm.GetValue();
            PrecursorIonToleranceUnit = IcParameters.Instance.PrecursorTolerancePpm.GetUnit();
            ProductIonTolerance = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            ProductIonToleranceUnit = IcParameters.Instance.ProductIonTolerancePpm.GetUnit();
            QValueThreshold = IcParameters.Instance.QValueThreshold;
            IonCorrelationThreshold = IcParameters.Instance.IonCorrelationThreshold;
            ModificationsPerSequence = IcParameters.Instance.MaxDynamicModificationsPerSequence;
            PointsToSmooth = IcParameters.Instance.PointsToSmooth;

            SaveCommand = new DelegateCommand(Save);
            CancelCommand = new DelegateCommand(Cancel);

            Status = false;
        }

        private void Save()
        {
            if (PointsToSmooth % 2 == 0 || PointsToSmooth < 3)
            {
                _dialogService.MessageBox("Points To Smooth must be an odd number greater than 1.");
                return;
            }

            IcParameters.Instance.PrecursorTolerancePpm = new Tolerance(PrecursorIonTolerance, PrecursorIonToleranceUnit);
            IcParameters.Instance.ProductIonTolerancePpm = new Tolerance(ProductIonTolerance, ProductIonToleranceUnit);
            IcParameters.Instance.QValueThreshold = QValueThreshold;
            IcParameters.Instance.MaxDynamicModificationsPerSequence = ModificationsPerSequence;
            IcParameters.Instance.IonCorrelationThreshold = IonCorrelationThreshold;
            IcParameters.Instance.PointsToSmooth = PointsToSmooth;

            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, null);
        }

        private void Cancel()
        {
            if (ReadyToClose != null) ReadyToClose(this, null);
        }

        private readonly IDialogService _dialogService;
    }
}
