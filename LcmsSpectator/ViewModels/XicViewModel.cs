using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using OxyPlot.Axes;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class XicViewModel: ReactiveObject
    {
        public XicViewModel(IMainDialogService dialogService, ILcMsRun lcms)
        {
            _dialogService = dialogService;
            _lcms = lcms;
            _fragmentXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom, Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan, 
                AbsoluteMaximum = lcms.MaxLcScan
            };
            _fragmentXAxis.AxisChanged += XAxisChanged;
            FragmentPlotViewModel = new XicPlotViewModel(_dialogService, lcms, "Fragment XIC", _fragmentXAxis, false);
            _heavyFragmentXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan
            };
            _heavyFragmentXAxis.AxisChanged += XAxisChanged;
            HeavyFragmentPlotViewModel = new XicPlotViewModel(_dialogService, lcms, "Heavy Fragment XIC", _heavyFragmentXAxis, false);
            _precursorXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan
            };
            _precursorXAxis.AxisChanged += XAxisChanged;
            PrecursorPlotViewModel = new XicPlotViewModel(_dialogService, lcms, "Precursor XIC", _precursorXAxis) {IsPlotVisible = true};
            _heavyPrecursorXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan
            };
            _heavyPrecursorXAxis.AxisChanged += XAxisChanged;
            HeavyPrecursorPlotViewModel = new XicPlotViewModel(_dialogService, lcms, "Heavy Precursor XIC", _heavyPrecursorXAxis);


            _showHeavy = false;
            _showFragmentXic = false;
            var openHeavyModificationsCommand = ReactiveCommand.Create();
            openHeavyModificationsCommand.Subscribe(_ => OpenHeavyModifications());
            OpenHeavyModificationsCommand = openHeavyModificationsCommand;

            PrecursorViewModes = new ReactiveList<PrecursorViewMode>(Enum.GetValues(typeof (PrecursorViewMode)).Cast<PrecursorViewMode>());

            // Update area ratios when the area of any of the plots changes
            this.WhenAny(x => x.FragmentPlotViewModel.Area, x => x.HeavyFragmentPlotViewModel.Area, (x, y) => x.Value / y.Value)
                .Select(FormatRatio)
                .ToProperty(this, x => x.FragmentAreaRatioLabel, out _fragmentAreaRatioLabel);
            this.WhenAny(x => x.PrecursorPlotViewModel.Area, x => x.HeavyPrecursorPlotViewModel.Area, (x, y) => x.Value / y.Value)
                .Select(FormatRatio)
                .ToProperty(this, x => x.PrecursorAreaRatioLabel, out _precursorAreaRatioLabel);
            this.WhenAnyValue(x => x.ShowFragmentXic, x => x.ShowHeavy)
                .Subscribe(x =>
                {
                    FragmentPlotViewModel.IsPlotVisible = x.Item1;
                    HeavyFragmentPlotViewModel.IsPlotVisible = x.Item1 && x.Item2;
                    HeavyPrecursorPlotViewModel.IsPlotVisible = x.Item2;
                });
        }

        #region Public Properties
        public XicPlotViewModel FragmentPlotViewModel { get; private set; }
        public XicPlotViewModel HeavyFragmentPlotViewModel { get; private set; }
        public XicPlotViewModel PrecursorPlotViewModel { get; private set; }
        public XicPlotViewModel HeavyPrecursorPlotViewModel { get; private set; }
        public IReactiveCommand OpenHeavyModificationsCommand { get; private set; }
        public ReactiveList<PrecursorViewMode> PrecursorViewModes { get; private set; }

        /// <summary>
        /// Toggle fragment XICs.
        /// </summary>
        public bool ShowFragmentXic
        {
            get { return _showFragmentXic; }
            set { this.RaiseAndSetIfChanged(ref _showFragmentXic, value); }
        }

        /// <summary>
        /// Shows and hides the heavy XICs.
        /// If the heavy XICs are being toggled on, it updates them.
        /// </summary>
        public bool ShowHeavy
        {
            get { return _showHeavy; }
            set { this.RaiseAndSetIfChanged(ref _showHeavy, value); }
        }

        private readonly ObservableAsPropertyHelper<string> _fragmentAreaRatioLabel;
        public string FragmentAreaRatioLabel
        {
            get { return _fragmentAreaRatioLabel.Value; }
        }

        private readonly ObservableAsPropertyHelper<string> _precursorAreaRatioLabel; 
        public string PrecursorAreaRatioLabel
        {
            get { return _precursorAreaRatioLabel.Value; }
        }

        public PrecursorViewMode PrecursorViewMode
        {
            get { return _precursorViewMode; }
            set { this.RaiseAndSetIfChanged(ref _precursorViewMode, value);  }
        }

        public void ClearCache()
        {
            FragmentPlotViewModel.ClearPlot();
            PrecursorPlotViewModel.ClearPlot();
            HeavyFragmentPlotViewModel.ClearPlot();
            HeavyPrecursorPlotViewModel.ClearPlot();
        }
        #endregion

        #region Private Methods
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
                _axisInternalChange = false;
            }
        }

        private void OpenHeavyModifications()
        {
            var heavyModificationsWindowVm = new HeavyModificationsWindowViewModel();
            _dialogService.OpenHeavyModifications(heavyModificationsWindowVm);
        }

        private string FormatRatio(double ratio)
        {
            if (ratio.Equals(Double.NaN) || ratio < 0) ratio = 0.0;
            string formatted;
            if (ratio > 1000 || ratio < 0.001) formatted = String.Format("{0:0.###EE0}", ratio);
            else formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
            return formatted;
        }
        #endregion

        #region Public Methods

        /// <summary>
        /// Zoom all XICs to a given scan number.
        /// </summary>
        /// <param name="scanNum">Scan number to zoom to.</param>
        public void ZoomToScan(int scanNum)
        {
            var rt = _lcms.GetElutionTime(scanNum);
            var min = Math.Max(rt - 1, 0);
            var max = Math.Min(rt + 1, _lcms.MaxLcScan);
            _precursorXAxis.Minimum = min;
            _precursorXAxis.Maximum = max;
            _precursorXAxis.Zoom(min, max);
        }
        #endregion

        #region Private Members
        private readonly IMainDialogService _dialogService;
        private readonly ILcMsRun _lcms;

        private bool _showHeavy;
        private bool _showFragmentXic;

        private bool _axisInternalChange;
        private readonly LinearAxis _fragmentXAxis;
        private readonly LinearAxis _heavyFragmentXAxis;
        private readonly LinearAxis _precursorXAxis;
        private readonly LinearAxis _heavyPrecursorXAxis;
        private PrecursorViewMode _precursorViewMode;
        #endregion
    }
}
