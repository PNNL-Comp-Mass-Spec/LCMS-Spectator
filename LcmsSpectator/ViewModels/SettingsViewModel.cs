using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using InformedProteomics.Backend.Data.Sequence;
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
        public double SpectrumFilterSlope { get; set; }
        public double PrecursorRelativeIntensityThreshold { get; set; }
        public PrecursorViewMode PrecursorViewMode { get; set; }

        public ObservableCollection<PrecursorViewMode> PrecursorViewModes { get; private set; } 
        public ObservableCollection<ModificationViewModel> Modifications { get; private set; }
        public RelayCommand AddModificationCommand { get; set; }

        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public HeavyModificationsViewModel HeavyModificationsViewModel { get; private set; }

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
            SpectrumFilterSlope = IcParameters.Instance.SpectrumFilterSlope;
            PrecursorRelativeIntensityThreshold = IcParameters.Instance.PrecursorRelativeIntensityThreshold;
            PrecursorViewMode = IcParameters.Instance.PrecursorViewMode;

            PrecursorViewModes = new ObservableCollection<PrecursorViewMode>
            {
                PrecursorViewMode.Isotopes,
                PrecursorViewMode.Charges
            };

            Modifications = new ObservableCollection<ModificationViewModel>();
            foreach (var searchModification in IcParameters.Instance.SearchModifications)
            {
                var modificationVm = new ModificationViewModel(searchModification);
                modificationVm.RequestModificationRemoval += RemoveModification;
                Modifications.Add(modificationVm);
            }
            AddModificationCommand = new RelayCommand(AddModification);

            HeavyModificationsViewModel = new HeavyModificationsViewModel();

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            Status = false;
        }

        private void AddModification()
        {
            var modVm = new ModificationViewModel();
            modVm.RequestModificationRemoval += RemoveModification;
            Modifications.Add(modVm);
        }

        private void Save()
        {
            if (PointsToSmooth != 0 && (PointsToSmooth % 2 == 0 || PointsToSmooth < 3))
            {
                _dialogService.MessageBox("Points To Smooth must be an odd number of at least 3. 0 = No smoothing.");
                return;
            }

            IcParameters.Instance.PrecursorTolerancePpm = new Tolerance(PrecursorIonTolerance, PrecursorIonToleranceUnit);
            IcParameters.Instance.ProductIonTolerancePpm = new Tolerance(ProductIonTolerance, ProductIonToleranceUnit);
            IcParameters.Instance.QValueThreshold = QValueThreshold;
            IcParameters.Instance.MaxDynamicModificationsPerSequence = ModificationsPerSequence;
            IcParameters.Instance.IonCorrelationThreshold = IonCorrelationThreshold;
            IcParameters.Instance.PointsToSmooth = PointsToSmooth;
            IcParameters.Instance.SpectrumFilterSlope = SpectrumFilterSlope;
            IcParameters.Instance.PrecursorRelativeIntensityThreshold = PrecursorRelativeIntensityThreshold;
            IcParameters.Instance.PrecursorViewMode = PrecursorViewMode;

            var modificationList = new List<SearchModification>();
            foreach (var searchModificationVm in Modifications)
            {
                var searchModification = searchModificationVm.SearchModification;
                if (searchModification != null) modificationList.Add(searchModification);
            }
            IcParameters.Instance.SearchModifications = modificationList;

            HeavyModificationsViewModel.Save();

            IcParameters.Instance.Update();

            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, null);
        }

        private void LoadLayoutFile()
        {
            
        }

        private void Cancel()
        {
            if (ReadyToClose != null) ReadyToClose(this, null);
        }

        private void RemoveModification(object sender, EventArgs e)
        {
            var modVm = sender as ModificationViewModel;
            if (modVm != null) Modifications.Remove(modVm);
        }

        private readonly IDialogService _dialogService;
    }
}
