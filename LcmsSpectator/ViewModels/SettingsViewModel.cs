using System;
using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class SettingsViewModel: ReactiveObject
    {
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; private set; }
        public event EventHandler ReadyToClose;

        #region Commands
        public IReactiveCommand AddModificationCommand { get; private set; }
        public IReactiveCommand CreateNewModificationCommand { get; private set; }
        public IReactiveCommand SaveCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }
        #endregion

        public SettingsViewModel(IMainDialogService dialogService)
        {
            _dialogService = dialogService;
            ToleranceUnits = new List<ToleranceUnit> {ToleranceUnit.Ppm, ToleranceUnit.Th};
            PrecursorIonTolerance = IcParameters.Instance.PrecursorTolerancePpm.GetValue();
            PrecursorIonToleranceUnit = IcParameters.Instance.PrecursorTolerancePpm.GetUnit();
            ProductIonTolerance = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            ProductIonToleranceUnit = IcParameters.Instance.ProductIonTolerancePpm.GetUnit();
            IonCorrelationThreshold = IcParameters.Instance.IonCorrelationThreshold;
            PointsToSmooth = IcParameters.Instance.PointsToSmooth;
            SpectrumFilterWindowSize = IcParameters.Instance.SpectrumFilterWindowSize;
            PrecursorRelativeIntensityThreshold = IcParameters.Instance.PrecursorRelativeIntensityThreshold;
            ShowInstrumentData = IcParameters.Instance.ShowInstrumentData;
            AutomaticallySelectIonTypes = IcParameters.Instance.AutomaticallySelectIonTypes;
            CidHcdIonTypes = IcParameters.Instance.GetCidHcdIonTypes();
            EtdIonTypes = IcParameters.Instance.GetEtdIonTypes();

            PrecursorViewModes = new ReactiveList<PrecursorViewMode>
            {
                PrecursorViewMode.Isotopes,
                PrecursorViewMode.Charges
            };

            Modifications = new ReactiveList<ModificationViewModel>();
            foreach (var searchModification in IcParameters.Instance.SearchModifications)
            {
                var modificationVm = new ModificationViewModel(searchModification);
                modificationVm.RemoveModificationCommand.Subscribe(_ => RemoveModification(modificationVm));
                Modifications.Add(modificationVm);
            }
            var addModificationCommand = ReactiveCommand.Create();
            addModificationCommand.Subscribe(_ => AddModification());
            AddModificationCommand = addModificationCommand;

            var createNewModificationCommand = ReactiveCommand.Create();
            createNewModificationCommand.Subscribe(_ => CreateNewModification());
            CreateNewModificationCommand = createNewModificationCommand;

            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => Save());
            SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => Cancel());
            CancelCommand = cancelCommand;

            HeavyModificationsViewModel = new HeavyModificationsViewModel();

            Status = false;
        }

        #region Public properties
        public List<ToleranceUnit> ToleranceUnits { get; set; }
        public double PrecursorIonTolerance { get; set; }
        public ToleranceUnit PrecursorIonToleranceUnit { get; set; }
        public double ProductIonTolerance { get; set; }
        public ToleranceUnit ProductIonToleranceUnit { get; set; }
        public double QValueThreshold { get; set; }
        public int ModificationsPerSequence { get; set; }
        public double IonCorrelationThreshold { get; set; }
        public int PointsToSmooth { get; set; }
        public double SpectrumFilterWindowSize { get; set; }
        public double PrecursorRelativeIntensityThreshold { get; set; }
        public bool ShowInstrumentData { get; private set; }
        public ReactiveList<PrecursorViewMode> PrecursorViewModes { get; private set; }
        public ReactiveList<ModificationViewModel> Modifications { get; private set; }
        public bool Status { get; private set; }

        public string CidHcdIonTypes { get; set; }
        public string EtdIonTypes { get; set; }
        private bool _automaticallySelectIonTypes;
        public bool AutomaticallySelectIonTypes 
        {
            get { return _automaticallySelectIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _automaticallySelectIonTypes, value); }
        }
        #endregion

        #region Private methods
        private void AddModification()
        {
            var modVm = new ModificationViewModel();
            modVm.RemoveModificationCommand.Subscribe(_ => RemoveModification(modVm));
            Modifications.Add(modVm);
        }

        private void CreateNewModification()
        {
            var customModVm = new CustomModificationViewModel("", false);
            if (_dialogService.OpenCustomModification(customModVm))
            {
                Modification.RegisterAndGetModification(customModVm.ModificationName, customModVm.Composition);
            }
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
            IcParameters.Instance.IonCorrelationThreshold = IonCorrelationThreshold;
            IcParameters.Instance.PointsToSmooth = PointsToSmooth;
            IcParameters.Instance.SpectrumFilterWindowSize = SpectrumFilterWindowSize;
            IcParameters.Instance.PrecursorRelativeIntensityThreshold = PrecursorRelativeIntensityThreshold;
            IcParameters.Instance.ShowInstrumentData = ShowInstrumentData;
            IcParameters.Instance.AutomaticallySelectIonTypes = AutomaticallySelectIonTypes;
            IcParameters.Instance.CidHcdIonTypes = IcParameters.IonTypeStringParse(CidHcdIonTypes);
            IcParameters.Instance.EtdIonTypes = IcParameters.IonTypeStringParse(EtdIonTypes);


            var modificationList = Modifications.Select(searchModificationVm => searchModificationVm.SearchModification)
                                                .Where(searchModification => searchModification != null).ToList();
            IcParameters.Instance.SearchModifications = modificationList;

            HeavyModificationsViewModel.Save();

            IcParameters.Instance.Update();

            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, null);
        }

        private void Cancel()
        {
            if (ReadyToClose != null) ReadyToClose(this, null);
        }

        private void RemoveModification(ModificationViewModel modVm)
        {
            if (modVm != null) Modifications.Remove(modVm);
        }
        #endregion

        #region Private fields
        private readonly IMainDialogService _dialogService;
        #endregion
    }
}
