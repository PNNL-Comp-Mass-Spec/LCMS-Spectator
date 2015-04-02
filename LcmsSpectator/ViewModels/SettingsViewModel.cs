using System;
using System.Linq;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class SettingsViewModel: ReactiveObject
    {
        /// <summary>
        /// Tracks settings and then publishes them to application settings when user clicks OK.
        /// </summary>
        /// <param name="dialogService">Dialog service for mvvm-friendly dialogs.</param>
        public SettingsViewModel(IMainDialogService dialogService)
        {
            _dialogService = dialogService;
            ToleranceUnits = new ReactiveList<ToleranceUnit> {ToleranceUnit.Ppm, ToleranceUnit.Th};
            PrecursorIonTolerance = IcParameters.Instance.PrecursorTolerancePpm.GetValue();
            PrecursorIonToleranceUnit = IcParameters.Instance.PrecursorTolerancePpm.GetUnit();
            ProductIonTolerance = IcParameters.Instance.ProductIonTolerancePpm.GetValue();
            ProductIonToleranceUnit = IcParameters.Instance.ProductIonTolerancePpm.GetUnit();
            IonCorrelationThreshold = IcParameters.Instance.IonCorrelationThreshold;
            PointsToSmooth = IcParameters.Instance.PointsToSmooth;
            PrecursorRelativeIntensityThreshold = IcParameters.Instance.PrecursorRelativeIntensityThreshold;
            ShowInstrumentData = IcParameters.Instance.ShowInstrumentData;
            AutomaticallySelectIonTypes = IcParameters.Instance.AutomaticallySelectIonTypes;
            CidHcdIonTypes = IcParameters.Instance.GetCidHcdIonTypes();
            EtdIonTypes = IcParameters.Instance.GetEtdIonTypes();
            ExportImageDpi = IcParameters.Instance.ExportImageDpi;

            Modifications = new ReactiveList<SelectModificationViewModel>();
            foreach (var searchModification in IcParameters.Instance.SearchModifications)
            {
                var modificationVm = new SelectModificationViewModel(searchModification);
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

        #region Commands
        public IReactiveCommand AddModificationCommand { get; private set; }
        public IReactiveCommand CreateNewModificationCommand { get; private set; }
        public IReactiveCommand SaveCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }
        #endregion

        #region Public properties
        /// <summary>
        /// View Model for light/heavy modification selector
        /// </summary>
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; private set; }

        /// <summary>
        /// Event that is triggered when this is ready to close (user has clicked "OK" or "Cancel")
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// List of possible tolerance units.
        /// </summary>
        public ReactiveList<ToleranceUnit> ToleranceUnits { get; set; }

        /// <summary>
        /// Tolerance for precursor ions.
        /// </summary>
        public double PrecursorIonTolerance { get; set; }

        /// <summary>
        /// Unit for tolerance of precursor ions.
        /// </summary>
        public ToleranceUnit PrecursorIonToleranceUnit { get; set; }

        /// <summary>
        /// Tolerance for product ions.
        /// </summary>
        public double ProductIonTolerance { get; set; }

        /// <summary>
        /// Unit for tolerance of product ions.
        /// </summary>
        public ToleranceUnit ProductIonToleranceUnit { get; set; }

        /// <summary>
        /// QValue threshold of identifications displayed.
        /// </summary>
        public double QValueThreshold { get; set; }

        /// <summary>
        /// Maximum number of possible modification combinations per sequence.
        /// </summary>
        public int ModificationsPerSequence { get; set; }

        /// <summary>
        /// Minimum possible correlation of ions.
        /// </summary>
        public double IonCorrelationThreshold { get; set; }

        /// <summary>
        /// Default value for "Points To Smooth" slider.
        /// </summary>
        public int PointsToSmooth { get; set; }

        /// <summary>
        /// Maximum size of window for spectrum filtering by intensity histogram.
        /// </summary>
        public double SpectrumFilterWindowSize { get; set; }

        /// <summary>
        /// Maximum relative intensity of isotopes to display on precursor ion plot.
        /// </summary>
        public double PrecursorRelativeIntensityThreshold { get; set; }

        /// <summary>
        /// Should instrument data (instrument reported mass, instrument reported charge) be
        /// displayed in the identifications data grid?
        /// </summary>
        public bool ShowInstrumentData { get; private set; }

        /// <summary>
        /// Modifications displayed in the application.
        /// </summary>
        public ReactiveList<SelectModificationViewModel> Modifications { get; private set; }

        /// <summary>
        /// Should the settings be saved?
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// String containing list of base ion types for CID and HCD spectra
        /// separated by a space.
        /// </summary>
        public string CidHcdIonTypes { get; set; }

        /// <summary>
        /// String containing list of base ion types for ETD spectra separated
        /// by a space.
        /// </summary>
        public string EtdIonTypes { get; set; }

        private bool _automaticallySelectIonTypes;
        /// <summary>
        /// Should ion types be changed when the activation method of the currently selected ms2 spectrum changes?
        /// </summary>
        public bool AutomaticallySelectIonTypes 
        {
            get { return _automaticallySelectIonTypes; }
            set { this.RaiseAndSetIfChanged(ref _automaticallySelectIonTypes, value); }
        }

        public int ExportImageDpi { get; set; }
        #endregion

        #region Private methods
        private void AddModification()
        {
            var modVm = new SelectModificationViewModel();
            modVm.RemoveModificationCommand.Subscribe(_ => RemoveModification(modVm));
            Modifications.Add(modVm);
        }

        private void CreateNewModification()
        {
            var customModVm = new CustomModificationViewModel("", false, _dialogService);
            if (_dialogService.OpenCustomModification(customModVm))
            {
                IcParameters.Instance.RegisterModification(customModVm.ModificationName, customModVm.Composition);
            }
        }

        private void Save()
        {
            if (PointsToSmooth != 0 && (PointsToSmooth % 2 == 0 || PointsToSmooth < 3))
            {
                _dialogService.MessageBox("Points To Smooth must be an odd number of at least 3. 0 = No smoothing.");
                return;
            }

            if (ExportImageDpi < 1)
            {
                _dialogService.MessageBox("Export Image DPI must be at least 1.");
                return;
            }

            IcParameters.Instance.PrecursorTolerancePpm = new Tolerance(PrecursorIonTolerance, PrecursorIonToleranceUnit);
            IcParameters.Instance.ProductIonTolerancePpm = new Tolerance(ProductIonTolerance, ProductIonToleranceUnit);
            IcParameters.Instance.IonCorrelationThreshold = IonCorrelationThreshold;
            IcParameters.Instance.PointsToSmooth = PointsToSmooth;
            IcParameters.Instance.PrecursorRelativeIntensityThreshold = PrecursorRelativeIntensityThreshold;
            IcParameters.Instance.ShowInstrumentData = ShowInstrumentData;
            IcParameters.Instance.AutomaticallySelectIonTypes = AutomaticallySelectIonTypes;
            IcParameters.Instance.CidHcdIonTypes = IcParameters.IonTypeStringParse(CidHcdIonTypes);
            IcParameters.Instance.EtdIonTypes = IcParameters.IonTypeStringParse(EtdIonTypes);
            IcParameters.Instance.ExportImageDpi = ExportImageDpi;


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

        private void RemoveModification(SelectModificationViewModel modVm)
        {
            if (modVm != null) Modifications.Remove(modVm);
        }
        #endregion

        #region Private fields
        private readonly IMainDialogService _dialogService;
        #endregion
    }
}
