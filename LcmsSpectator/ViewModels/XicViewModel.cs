using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.PlotModels;
using LcmsSpectatorModels.Models;
using OxyPlot.Axes;

namespace LcmsSpectator.ViewModels
{
    public class XicViewModel: ViewModelBase
    {
        public XicPlotViewModel FragmentPlotViewModel { get; set; }
        public XicPlotViewModel HeavyFragmentPlotViewModel { get; set; }
        public XicPlotViewModel PrecursorPlotViewModel { get; set; }
        public XicPlotViewModel HeavyPrecursorPlotViewModel { get; set; }
        public ColorDictionary Colors { get; set; }
        public ILcMsRun Lcms { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        public RelayCommand OpenHeavyModificationsCommand { get; private set; }
        public class XicCloseRequest: NotificationMessage
        {
            public XicCloseRequest(object sender, string notification = "XicClosing") : base(sender, notification) {}
        }
        public XicViewModel(IMainDialogService dialogService=null)
        {
            IsLoading = true;
            if (dialogService == null) dialogService = new MainDialogService();
            _dialogService = dialogService;
            _xicXAxis = new LinearAxis(AxisPosition.Bottom, "Retention Time");
            FragmentPlotViewModel = new XicPlotViewModel(_dialogService, "Fragment XIC", XicXAxis, false, false);
            HeavyFragmentPlotViewModel = new XicPlotViewModel(_dialogService, "Heavy Fragment XIC", XicXAxis, true, false);
            PrecursorPlotViewModel = new XicPlotViewModel(_dialogService, "Precursor XIC", XicXAxis, false);
            HeavyPrecursorPlotViewModel = new XicPlotViewModel(_dialogService, "Heavy Precursor XIC", XicXAxis, true);
            _showScanMarkers = false;
            _showHeavy = false;
            _showFragmentXic = false;
            XicXAxis.AxisChanged += XAxisChanged;
            CloseCommand = new RelayCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), ""))
                    Messenger.Default.Send(new XicCloseRequest(this));
            });
            OpenHeavyModificationsCommand = new RelayCommand(OpenHeavyModifications);

            _fragmentLabels = new List<LabeledIonViewModel>();
            _precursorLabels = new List<LabeledIonViewModel>();

            Messenger.Default.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedPrecursorLabelsChanged);
            Messenger.Default.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedFragmentLabelsChanged);
            Messenger.Default.Register<PropertyChangedMessage<PrSm>>(this, SelectedPrSmChanged);
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);
        }

        /// <summary>
        /// Raw file name without path or extension. For displaying on tab header.
        /// </summary>
        public string RawFileName
        {
            get { return _rawFileName; }
            private set
            {
                _rawFileName = value;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Full path to the raw file including extension.
        /// </summary>
        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                _rawFilePath = value;
                RawFileName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(RawFilePath));
                IsLoading = true;
                // load raw file
                Lcms = PbfLcMsRun.GetLcMsRun(_rawFilePath, MassSpecDataType.XCaliburRun, 0, 0);
                FragmentPlotViewModel.Lcms = Lcms;
                HeavyFragmentPlotViewModel.Lcms = Lcms;
                PrecursorPlotViewModel.Lcms = Lcms;
                HeavyPrecursorPlotViewModel.Lcms = Lcms;
                // set bounds for shared x axis
                var maxRt = Math.Max(Lcms.GetElutionTime(Lcms.MaxLcScan), 1.0);
                _xicXAxis.Maximum = maxRt + 0.0001;
                _xicXAxis.Minimum = 0;
                _xicXAxis.AbsoluteMaximum = maxRt + 0.0001;
                _xicXAxis.AbsoluteMinimum = 0;
                _xicXAxis.Zoom(0, maxRt);
                UpdatePlots();  // update plots in case things were changed during loading
                IsLoading = false;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                RaisePropertyChanged();
            }
        }

        private void SettingsChanged(SettingsChangedNotification notification)
        {
            UpdatePlots();
        }

        private void SelectedPrSmChanged(PropertyChangedMessage<PrSm> message)
        {
            ClearCache();

            // calculate rt bounds
            var rt = Lcms.GetElutionTime(SelectedPrSmViewModel.Instance.Scan);
            var minLcmsRt = Lcms.GetElutionTime(Lcms.MinLcScan);
            var maxLcmsRt = Lcms.GetElutionTime(Lcms.MaxLcScan);
            var minRt = rt - 1;
            var maxRt = rt + 1;
            if (rt < 1) minRt = 0;
            minRt = Math.Max(minRt, minLcmsRt);
            if (rt.Equals(0)) maxRt = maxLcmsRt;
            if (rt > maxLcmsRt)
            {
                minRt = 0;
                maxRt = maxLcmsRt;
            }
            // zoom to rt
            XicXAxis.Minimum = minRt;
            XicXAxis.Maximum = maxRt;
            XicXAxis.Zoom(minRt, maxRt);
        }

        private async void SelectedFragmentLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "FragmentLabels") return;
            _fragmentLabels = message.NewValue;
            if (ShowHeavy)
            {
                if (ShowFragmentXic)
                {
                    FragmentPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetLightFragmentIons();
                    HeavyFragmentPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyFragmentIons();
                    UpdateFragmentAreaRatioLabels();
                }
            }
            else
            {
                if (ShowFragmentXic)
                {
                    FragmentPlotViewModel.Ions = _fragmentLabels;
                }
            }
        }

        private async void SelectedPrecursorLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "PrecursorLabels") return;
            _precursorLabels = message.NewValue;
            if (ShowHeavy)
            {
                PrecursorPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetLightPrecursorIons();
                HeavyPrecursorPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyPrecursorIons();
                UpdatePrecursorAreaRatioLabels();
            }
            else
            {
                PrecursorPlotViewModel.Ions = _precursorLabels;
            }
        }


        /// <summary>
        /// Shows and hides the point markers on the XIC plots.
        /// </summary>
        public bool ShowScanMarkers
        {
            get { return _showScanMarkers; }
            set
            {
                _showScanMarkers = value;
                FragmentPlotViewModel.ShowScanMarkers = _showScanMarkers;
                HeavyFragmentPlotViewModel.ShowScanMarkers = _showScanMarkers;
                PrecursorPlotViewModel.ShowScanMarkers = _showScanMarkers;
                HeavyPrecursorPlotViewModel.ShowScanMarkers = _showScanMarkers;
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// Toggle fragment XICs.
        /// </summary>
        public bool ShowFragmentXic
        {
            get { return _showFragmentXic; }
            set
            {
                _showFragmentXic = value;
                if (_showFragmentXic)
                {
                    EnableFragmentXic();
                }
                else
                {
                    FragmentPlotViewModel.Ions = new List<LabeledIonViewModel>();
                    if (_showHeavy)
                        HeavyFragmentPlotViewModel.Ions = new List<LabeledIonViewModel>();
                }
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Shows and hides the heavy XICs.
        /// If the heavy XICs are being toggled on, it updates them.
        /// </summary>
        public bool ShowHeavy
        {
            get { return _showHeavy; }
            set
            {
                _showHeavy = value;
                if (_showHeavy)
                {
                    EnableHeavyXic();
                }
                else
                {
                    HeavyPrecursorPlotViewModel.Ions = new List<LabeledIonViewModel>();
                    if (_showFragmentXic)
                        HeavyFragmentPlotViewModel.Ions = new List<LabeledIonViewModel>();
                }
                RaisePropertyChanged();
            }
        }

        public string FragmentAreaRatioLabel
        {
            get { return _fragmentAreaRatioLabel; }
            private set
            {
                _fragmentAreaRatioLabel = value;
                RaisePropertyChanged();
            }
        }

        public string PrecursorAreaRatioLabel
        {
            get { return _precursorAreaRatioLabel; }
            set
            {
                _precursorAreaRatioLabel = value;
                RaisePropertyChanged();
            }
        }

        public void ClearCache()
        {
            FragmentPlotViewModel.ClearCache();
            PrecursorPlotViewModel.ClearCache();
            HeavyFragmentPlotViewModel.ClearCache();
            HeavyPrecursorPlotViewModel.ClearCache();
        }

        /// <summary>
        /// Update and regenerate all plots
        /// </summary>
        public async void UpdatePlots()
        {
            ClearCache();

            if (ShowHeavy)
            {
                PrecursorPlotViewModel.Ions = _precursorLabels;
                HeavyPrecursorPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyPrecursorIons();
                UpdatePrecursorAreaRatioLabels();
                if (ShowFragmentXic)
                {
                    FragmentPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetLightFragmentIons();
                    HeavyFragmentPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyFragmentIons();
                    UpdateFragmentAreaRatioLabels();
                }
            }
            else
            {
                PrecursorPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.PrecursorLabelUpdate;
                if (ShowFragmentXic)
                {
                    FragmentPlotViewModel.Ions = _fragmentLabels;
                }
            }
        }

        private async void EnableHeavyXic()
        {
            HeavyPrecursorPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyPrecursorIons();
            UpdatePrecursorAreaRatioLabels();
            if (ShowFragmentXic)
            {
                HeavyFragmentPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyFragmentIons();
                UpdateFragmentAreaRatioLabels();
            }
        }

        private async void EnableFragmentXic()
        {
            if (ShowHeavy)
            {
                FragmentPlotViewModel.Ions = _fragmentLabels;
                HeavyFragmentPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyFragmentIons();
                UpdateFragmentAreaRatioLabels();
            }
            else
            {
                FragmentPlotViewModel.Ions = _fragmentLabels;
            }
        }

        /// <summary>
        /// Shared x axis for all plots. Sharing an X axis allows all plots to zoom and pan together.
        /// </summary>
        private LinearAxis XicXAxis
        {
            get
            {
                return _xicXAxis;
            }
        }

        /// <summary>
        /// Update the ratio labels for the fragment ion Xics.
        /// Ratios are (light / heavy)
        /// </summary>
        private async void UpdateFragmentAreaRatioLabels()
        {
            if (!ShowHeavy || !ShowFragmentXic) return;
            if (FragmentPlotViewModel == null || FragmentPlotViewModel.Plot == null) return;
            if (XicXAxis == null) return;
            var fragmentArea = await FragmentPlotViewModel.GetAreaTask();
            var heavyFragmentArea = await HeavyFragmentPlotViewModel.GetAreaTask();
            var ratio = fragmentArea / heavyFragmentArea;
            if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
            string formatted;
            if (ratio > 1000 || ratio < 0.001) formatted = String.Format("{0:0.###EE0}", ratio);
            else formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
            FragmentAreaRatioLabel = String.Format("Area ratio: {0}", formatted);
        }

        /// <summary>
        /// Update the ratio labels for the precursor ion Xics.
        /// Ratios are (light / heavy)
        /// </summary>
        private async void UpdatePrecursorAreaRatioLabels()
        {
            if (!ShowHeavy) return;
            if (PrecursorPlotViewModel == null || PrecursorPlotViewModel.Plot == null) return;
            if (XicXAxis == null) return;
            var precursorArea = await PrecursorPlotViewModel.GetAreaTask();
            var heavyPrecursorArea = await HeavyPrecursorPlotViewModel.GetAreaTask();
            var ratio = precursorArea / heavyPrecursorArea;
            if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
            string formatted;
            if (ratio > 1000 || ratio < 0.001) formatted = String.Format("{0:0.###EE0}", ratio);
            else formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
            PrecursorAreaRatioLabel = String.Format("Area ratio: {0}", formatted);
        }

        /// <summary>
        /// Event handler for XAxis changed to update area ratio labels when shared x axis is zoomed or panned.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            UpdateFragmentAreaRatioLabels();
            UpdatePrecursorAreaRatioLabels();
        }

        private void OpenHeavyModifications()
        {
            var heavyModificationsWindowVm = new HeavyModificationsWindowViewModel();
            _dialogService.OpenHeavyModifications(heavyModificationsWindowVm);
            if (heavyModificationsWindowVm.Status)
            {
                Messenger.Default.Send(new SettingsChangedNotification(this, "HeavyModificationsSettingsChanged"));
            }
        }

        private readonly IMainDialogService _dialogService;

        private readonly LinearAxis _xicXAxis;

        private List<LabeledIonViewModel> _fragmentLabels;
        private List<LabeledIonViewModel> _precursorLabels; 

        private bool _showScanMarkers;
        private bool _showHeavy;
        private string _rawFilePath;
        private bool _showFragmentXic;
        private string _rawFileName;
        private bool _isLoading;
        private string _fragmentAreaRatioLabel;
        private string _precursorAreaRatioLabel;
    }
}
