using System;
using System.Collections.Generic;
using System.Globalization;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
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
        public RelayCommand OpenHeavyModificationsCommand { get; private set; }
        public XicViewModel(IMainDialogService dialogService, ITaskService taskService, Messenger messenger)
        {
            MessengerInstance = messenger;
            //IsLoading = true;
            _dialogService = dialogService;
            _taskService = taskService;
            _xicXAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Retention Time"
            };
            _fragmentXAxis = new LinearAxis {Position = AxisPosition.Bottom, Title = "Retention Time"};
            _fragmentXAxis.AxisChanged += XAxisChanged;
            FragmentPlotViewModel = new XicPlotViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), messenger, "Fragment XIC", _fragmentXAxis, false, false);
            FragmentPlotViewModel.XicPlotChanged += (o, e) => UpdateFragmentAreaRatioLabels();
            _heavyFragmentXAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Retention Time" };
            _heavyFragmentXAxis.AxisChanged += XAxisChanged;
            HeavyFragmentPlotViewModel = new XicPlotViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), messenger, "Heavy Fragment XIC", _heavyFragmentXAxis, true, false);
            HeavyFragmentPlotViewModel.XicPlotChanged += (o, e) => UpdateFragmentAreaRatioLabels();
            _precursorXAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Retention Time" };
            _precursorXAxis.AxisChanged += XAxisChanged;
            PrecursorPlotViewModel = new XicPlotViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), messenger, "Precursor XIC", _precursorXAxis, false);
            PrecursorPlotViewModel.XicPlotChanged += (o, e) => UpdatePrecursorAreaRatioLabels();
            _heavyPrecursorXAxis = new LinearAxis { Position = AxisPosition.Bottom, Title = "Retention Time" };
            _heavyPrecursorXAxis.AxisChanged += XAxisChanged;
            HeavyPrecursorPlotViewModel = new XicPlotViewModel(_dialogService, TaskServiceFactory.GetTaskServiceLike(taskService), messenger, "Heavy Precursor XIC", _heavyPrecursorXAxis, true);
            HeavyPrecursorPlotViewModel.XicPlotChanged += (o, e) => UpdatePrecursorAreaRatioLabels();
            _showScanMarkers = false;
            _showHeavy = false;
            _showFragmentXic = false;
            XicXAxis.AxisChanged += XAxisChanged;
            OpenHeavyModificationsCommand = new RelayCommand(OpenHeavyModifications);

            _fragmentLabels = new List<LabeledIonViewModel>();
            _lightFragmentLabels = new List<LabeledIonViewModel>();
            _heavyFragmentLabels = new List<LabeledIonViewModel>();
            _precursorLabels = new List<LabeledIonViewModel>();
            _lightPrecursorLabels = new List<LabeledIonViewModel>();
            _heavyPrecursorLabels = new List<LabeledIonViewModel>();

            MessengerInstance.Register<ClearAllNotification>(this, ClearAllNotificationHandler);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedPrecursorLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, LightPrecursorLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, HeavyPrecursorLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, SelectedFragmentLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, LightFragmentLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<List<LabeledIonViewModel>>>(this, HeavyFragmentLabelsChanged);
            MessengerInstance.Register<PropertyChangedMessage<PrSm>>(this, SelectedPrSmChanged);
            Messenger.Default.Register<SettingsChangedNotification>(this, SettingsChanged);
        }

        public ILcMsRun Lcms
        {
            get { return _lcms; }
            set
            {
                _lcms = value;
                FragmentPlotViewModel.Lcms = _lcms;
                HeavyFragmentPlotViewModel.Lcms = _lcms;
                PrecursorPlotViewModel.Lcms = _lcms;
                HeavyPrecursorPlotViewModel.Lcms = _lcms;
                RaisePropertyChanged();
            }
        }

        private void SettingsChanged(SettingsChangedNotification notification)
        {
            //if (notification.Notification != "HeavyModificationsSettingsChanged") return;
            //UpdatePlots();
            ClearCache();
        }

        private void SelectedPrSmChanged(PropertyChangedMessage<PrSm> message)
        {
            if (message.NewValue == null) return;
            var prsm = message.NewValue;
            // calculate rt bounds
            var rt = Lcms.GetElutionTime(prsm.Scan);
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

        private void SelectedFragmentLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "FragmentLabels") return;
            _fragmentLabels = message.NewValue;
            if (!ShowHeavy && ShowFragmentXic)
            {
                FragmentPlotViewModel.Ions = _fragmentLabels;
            }
        }

        private void LightFragmentLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "LightFragmentLabels") return;
            _lightFragmentLabels = message.NewValue;
            if (ShowHeavy && ShowFragmentXic)
            {
                FragmentPlotViewModel.Ions = _lightFragmentLabels;
                UpdateFragmentAreaRatioLabels();
            }
        }

        private void HeavyFragmentLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "HeavyFragmentLabels") return;
            _heavyFragmentLabels = message.NewValue;
            if (ShowHeavy && ShowFragmentXic)
            {
                HeavyFragmentPlotViewModel.Ions = _heavyFragmentLabels;
                UpdateFragmentAreaRatioLabels();
            }
        }

        private void SelectedPrecursorLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "PrecursorLabels") return;
            _precursorLabels = message.NewValue;
            if (ShowHeavy)
            {
                //PrecursorPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetLightPrecursorIons();
                //HeavyPrecursorPlotViewModel.Ions = await SelectedPrSmViewModel.Instance.GetHeavyPrecursorIons();
                UpdatePrecursorAreaRatioLabels();
            }
            else
            {
                PrecursorPlotViewModel.Ions = _precursorLabels;
            }
        }

        private void LightPrecursorLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "LightPrecursorLabels") return;
            _lightPrecursorLabels = message.NewValue;
            if (ShowHeavy)
            {
                PrecursorPlotViewModel.Ions = _lightPrecursorLabels;
                UpdatePrecursorAreaRatioLabels();   
            }
        }

        private void HeavyPrecursorLabelsChanged(PropertyChangedMessage<List<LabeledIonViewModel>> message)
        {
            if (message.PropertyName != "HeavyPrecursorLabels") return;
            _heavyPrecursorLabels = message.NewValue;
            if (ShowHeavy)
            {
                HeavyPrecursorPlotViewModel.Ions = _heavyPrecursorLabels;
                UpdatePrecursorAreaRatioLabels();   
            }
        }


        private void ClearAllNotificationHandler(ClearAllNotification notification)
        {
            ClearCache();
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
                var oldValue = _showHeavy;
                _showHeavy = value;
                if (_showHeavy)
                {
                    EnableHeavyXic();
                }
                else
                {
                    PrecursorPlotViewModel.Ions = _precursorLabels;
                    HeavyPrecursorPlotViewModel.Ions = new List<LabeledIonViewModel>();
                    if (_showFragmentXic)
                    {
                        FragmentPlotViewModel.Ions = _fragmentLabels;
                        HeavyFragmentPlotViewModel.Ions = new List<LabeledIonViewModel>();   
                    }
                }
                RaisePropertyChanged("ShowHeavy", oldValue, _showHeavy, true);
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
        public void UpdatePlots()
        {
            ClearCache();

            if (ShowHeavy)
            {
                PrecursorPlotViewModel.Ions = _lightPrecursorLabels;
                HeavyPrecursorPlotViewModel.Ions = _heavyPrecursorLabels;
                UpdatePrecursorAreaRatioLabels();
                if (ShowFragmentXic)
                {
                    FragmentPlotViewModel.Ions = _lightFragmentLabels;
                    HeavyFragmentPlotViewModel.Ions = _heavyFragmentLabels;
                    UpdateFragmentAreaRatioLabels();
                }
            }
            else
            {
                PrecursorPlotViewModel.Ions = _precursorLabels;
                if (ShowFragmentXic)
                {
                    FragmentPlotViewModel.Ions = _fragmentLabels;
                }
            }
        }

        private void EnableHeavyXic()
        {
            PrecursorPlotViewModel.Ions = _lightPrecursorLabels;
            HeavyPrecursorPlotViewModel.Ions = _heavyPrecursorLabels;
            UpdatePrecursorAreaRatioLabels();
            if (ShowFragmentXic)
            {
                FragmentPlotViewModel.Ions = _lightFragmentLabels;
                HeavyFragmentPlotViewModel.Ions = _heavyFragmentLabels;
                UpdateFragmentAreaRatioLabels();
            }
        }

        private void EnableFragmentXic()
        {
            if (ShowHeavy)
            {
                FragmentPlotViewModel.Ions = _lightFragmentLabels;
                HeavyFragmentPlotViewModel.Ions = _heavyFragmentLabels;
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
        private void UpdateFragmentAreaRatioLabels()
        {
            _taskService.Enqueue(() =>
            {
                if (!ShowHeavy || !ShowFragmentXic) return;
                if (FragmentPlotViewModel == null || FragmentPlotViewModel.Plot == null) return;
                if (XicXAxis == null) return;
                var fragmentArea = FragmentPlotViewModel.GetCurrentArea();
                var heavyFragmentArea = HeavyFragmentPlotViewModel.GetCurrentArea();
                var ratio = fragmentArea / heavyFragmentArea;
                if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
                string formatted;
                if (ratio > 1000 || ratio < 0.001) formatted = String.Format("{0:0.###EE0}", ratio);
                else formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
                FragmentAreaRatioLabel = String.Format("Area ratio: {0}", formatted); 
            });
        }

        /// <summary>
        /// Update the ratio labels for the precursor ion Xics.
        /// Ratios are (light / heavy)
        /// </summary>
        private void UpdatePrecursorAreaRatioLabels()
        {
            _taskService.Enqueue(() =>
            {
                if (!ShowHeavy) return;
                if (PrecursorPlotViewModel == null || PrecursorPlotViewModel.Plot == null) return;
                if (XicXAxis == null) return;
                var precursorArea = PrecursorPlotViewModel.GetCurrentArea();
                var heavyPrecursorArea = HeavyPrecursorPlotViewModel.GetCurrentArea();
                var ratio = precursorArea / heavyPrecursorArea;
                if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
                string formatted;
                if (ratio > 1000 || ratio < 0.001) formatted = String.Format("{0:0.###EE0}", ratio);
                else formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
                PrecursorAreaRatioLabel = String.Format("Area ratio: {0}", formatted); 
            });
        }

        /// <summary>
        /// Event handler for XAxis changed to update area ratio labels when shared x axis is zoomed or panned.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (!_axisInternalChange)
            {
                _axisInternalChange = true;
                var axis = sender as LinearAxis;
                if (axis == null) return;
                if (sender != _fragmentXAxis)
                    _fragmentXAxis.Zoom(axis.ActualMinimum, axis.ActualMaximum);
                if (sender != _heavyFragmentXAxis)
                    _heavyFragmentXAxis.Zoom(axis.ActualMinimum, axis.ActualMaximum);
                if (sender != _precursorXAxis)
                    _precursorXAxis.Zoom(axis.ActualMinimum, axis.ActualMaximum);
                if (sender != _heavyPrecursorXAxis)
                    _heavyPrecursorXAxis.Zoom(axis.ActualMinimum, axis.ActualMaximum);
                UpdateFragmentAreaRatioLabels();
                UpdatePrecursorAreaRatioLabels();
                _axisInternalChange = false;
            }
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
        private readonly ITaskService _taskService;

        private readonly LinearAxis _xicXAxis;

        private List<LabeledIonViewModel> _fragmentLabels;
        private List<LabeledIonViewModel> _lightFragmentLabels;
        private List<LabeledIonViewModel> _precursorLabels; 

        private bool _showScanMarkers;
        private bool _showHeavy;
        private bool _showFragmentXic;
        private string _fragmentAreaRatioLabel;
        private string _precursorAreaRatioLabel;
        private List<LabeledIonViewModel> _heavyFragmentLabels;
        private List<LabeledIonViewModel> _lightPrecursorLabels;
        private List<LabeledIonViewModel> _heavyPrecursorLabels;

        private bool _axisInternalChange;
        private readonly LinearAxis _fragmentXAxis;
        private readonly LinearAxis _heavyFragmentXAxis;
        private readonly LinearAxis _precursorXAxis;
        private readonly LinearAxis _heavyPrecursorXAxis;
        private ILcMsRun _lcms;
    }
}
