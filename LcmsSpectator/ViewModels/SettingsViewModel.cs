// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SettingsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Tracks settings and then publishes them to application settings when user clicks OK.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Linq;
    using System.Windows.Media;

    using InformedProteomics.Backend.Data.Spectrometry;
    using Config;
    using DialogServices;
    using Modifications;

    using OxyPlot.Wpf;

    using ReactiveUI;

    /// <summary>
    /// Tracks settings and then publishes them to application settings when user clicks OK.
    /// </summary>
    public class SettingsViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from a view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// A value indicating whether the ion types should be changed
        /// when the activation method of the currently selected MS/MS spectrum changes.
        /// </summary>
        private bool automaticallySelectIonTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from a view model.</param>
        public SettingsViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            ToleranceUnits = new ReactiveList<ToleranceUnit> { ToleranceUnit.Ppm, ToleranceUnit.Mz };
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

            FeatureColors = new ColorListViewModel();
            IdColors = new ColorListViewModel();

            var oxyScanCol = IcParameters.Instance.Ms2ScanColor;
            Ms2ScanColor = new Color { A = oxyScanCol.A, R = oxyScanCol.R, G = oxyScanCol.G, B = oxyScanCol.B };

            foreach (var color in IcParameters.Instance.FeatureColors)
            {
                FeatureColors.ColorViewModels.Add(new ColorViewModel { SelectedColor = new Color { A = color.A, R = color.R, B = color.B, G = color.G } });
            }

            foreach (var color in IcParameters.Instance.IdColors)
            {
                IdColors.ColorViewModels.Add(new ColorViewModel { SelectedColor = new Color { A = color.A, R = color.R, B = color.B, G = color.G } });
            }

            Modifications = new ReactiveList<SearchModificationViewModel>();
            foreach (var searchModification in IcParameters.Instance.SearchModifications)
            {
                var modificationVm = new SearchModificationViewModel(searchModification, this.dialogService);
                ////modificationVm.RemoveModificationCommand.Subscribe(_ => this.RemoveModification(modificationVm));
                Modifications.Add(modificationVm);
            }

            var addModificationCommand = ReactiveCommand.Create();
            addModificationCommand.Subscribe(_ => AddModificationImplementation());
            AddModificationCommand = addModificationCommand;

            var createNewModificationCommand = ReactiveCommand.Create();
            createNewModificationCommand.Subscribe(_ => CreateNewModificationImplementation());
            CreateNewModificationCommand = createNewModificationCommand;

            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => SaveImplementation());
            SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => CancelImplementation());
            CancelCommand = cancelCommand;

            HeavyModificationsViewModel = new HeavyModificationsViewModel(dialogService);

            Status = false;
        }

        /// <summary>
        /// Event that is triggered when this is ready to close (user has clicked "OK" or "Cancel")
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets a command that adds a search modification to the modification selector list.
        /// </summary>
        public IReactiveCommand AddModificationCommand { get; }

        /// <summary>
        /// Gets a command that creates and register a new modification.
        /// </summary>
        public IReactiveCommand CreateNewModificationCommand { get; }

        /// <summary>
        /// Gets a command that validates all settings and saves them.
        /// </summary>
        public IReactiveCommand SaveCommand { get; }

        /// <summary>
        /// Gets a command that closes settings without saving.
        /// </summary>
        public IReactiveCommand CancelCommand { get; }

        /// <summary>
        /// Gets the view Model for light/heavy modification selector
        /// </summary>
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; }

        /// <summary>
        /// Gets or sets a list of possible tolerance units.
        /// </summary>
        public ReactiveList<ToleranceUnit> ToleranceUnits { get; set; }

        /// <summary>
        /// Gets or sets the tolerance for precursor ions.
        /// </summary>
        public double PrecursorIonTolerance { get; set; }

        /// <summary>
        /// Gets or sets the unit for tolerance of precursor ions.
        /// </summary>
        public ToleranceUnit PrecursorIonToleranceUnit { get; set; }

        /// <summary>
        /// Gets or sets the tolerance for product ions.
        /// </summary>
        public double ProductIonTolerance { get; set; }

        /// <summary>
        /// Gets or sets the unit for tolerance of product ions.
        /// </summary>
        public ToleranceUnit ProductIonToleranceUnit { get; set; }

        /// <summary>
        /// Gets or sets the QValue threshold of identifications displayed.
        /// </summary>
        public double QValueThreshold { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of possible modification combinations per sequence.
        /// </summary>
        public int ModificationsPerSequence { get; set; }

        /// <summary>
        /// Gets or sets the minimum possible correlation of ions.
        /// </summary>
        public double IonCorrelationThreshold { get; set; }

        /// <summary>
        /// Gets or sets the default value for "Points To Smooth" slider.
        /// </summary>
        public int PointsToSmooth { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of window for spectrum filtering by intensity histogram.
        /// </summary>
        public double SpectrumFilterWindowSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum relative intensity of isotopes to display on precursor ion plot.
        /// </summary>
        public double PrecursorRelativeIntensityThreshold { get; set; }

        /// <summary>
        /// Gets a value indicating whether the instrument data (instrument reported mass, instrument reported charge) is
        /// displayed in the identifications data grid?
        /// </summary>
        public bool ShowInstrumentData { get; }

        /// <summary>
        /// Gets the modifications displayed in the application.
        /// </summary>
        public ReactiveList<SearchModificationViewModel> Modifications { get; }

        /// <summary>
        /// Gets a value indicating whether the settings should be saved.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets or sets the string containing list of base ion types for CID and HCD spectra
        /// separated by a space.
        /// </summary>
        public string CidHcdIonTypes { get; set; }

        /// <summary>
        /// Gets or sets the string containing list of base ion types for ETD spectra separated
        /// by a space.
        /// </summary>
        public string EtdIonTypes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the ion types should be changed
        /// when the activation method of the currently selected MS/MS spectrum changes?
        /// </summary>
        public bool AutomaticallySelectIonTypes
        {
            get => automaticallySelectIonTypes;
            set => this.RaiseAndSetIfChanged(ref automaticallySelectIonTypes, value);
        }

        /// <summary>
        /// Gets or sets the DPI resolution for image exporting.
        /// </summary>
        public int ExportImageDpi { get; set; }

        /// <summary>
        /// Gets the view model for the feature color list.
        /// </summary>
        public ColorListViewModel FeatureColors { get; }

        /// <summary>
        /// Gets the view model for the id color list.
        /// </summary>
        public ColorListViewModel IdColors { get; }

        /// <summary>
        /// Gets or sets the color for the MS/MS scans on the feature map.
        /// </summary>
        public Color Ms2ScanColor { get; set; }

        /// <summary>
        /// Implementation for the AddModificationCommand.
        /// Adds a search modification to the modification selector list.
        /// </summary>
        private void AddModificationImplementation()
        {
            var modVm = new SearchModificationViewModel(dialogService);
            ////modVm.RemoveModificationCommand.Subscribe(_ => this.RemoveModification(modVm));
            Modifications.Add(modVm);
        }

        /// <summary>
        /// Implementation for CreateNewModificationCommand.
        /// Creates and register a new modification.
        /// </summary>
        private void CreateNewModificationImplementation()
        {
            var customModVm = new CustomModificationViewModel(string.Empty, false, dialogService);
            if (dialogService.OpenCustomModification(customModVm))
            {
                IcParameters.Instance.RegisterModification(customModVm.ModificationName, customModVm.Composition);
            }
        }

        /// <summary>
        /// Implementation for SaveCommand.
        /// Validates all settings and saves them.
        /// </summary>
        private void SaveImplementation()
        {
            if (PointsToSmooth != 0 && (PointsToSmooth % 2 == 0 || PointsToSmooth < 3))
            {
                dialogService.MessageBox("Points To Smooth must be an odd number of at least 3. 0 = No smoothing.");
                return;
            }

            if (ExportImageDpi < 1)
            {
                dialogService.MessageBox("Export Image DPI must be at least 1.");
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

            IcParameters.Instance.FeatureColors = FeatureColors.GetOxyColors();
            IcParameters.Instance.IdColors = IdColors.GetOxyColors();
            IcParameters.Instance.Ms2ScanColor = Ms2ScanColor.ToOxyColor();

            var modificationList = Modifications.Select(searchModificationVm => searchModificationVm.SearchModification)
                                                .Where(searchModification => searchModification != null).ToList();
            IcParameters.Instance.SearchModifications = modificationList;

            HeavyModificationsViewModel.Save();

            IcParameters.Instance.Update();

            Status = true;
            ReadyToClose?.Invoke(this, null);
        }

        /// <summary>
        /// Implementation for CancelCommand.
        /// Closes settings without saving.
        /// </summary>
        private void CancelImplementation()
        {
            ReadyToClose?.Invoke(this, null);
        }

        /// <summary>
        /// Removes a modification from the list of modifications.
        /// </summary>
        /// <param name="modVm">The modification to remove.</param>
        private void RemoveModification(SearchModificationViewModel modVm)
        {
            if (modVm != null)
            {
                Modifications.Remove(modVm);
            }
        }
    }
}
