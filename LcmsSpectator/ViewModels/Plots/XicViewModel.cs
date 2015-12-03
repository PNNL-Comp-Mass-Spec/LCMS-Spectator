// --------------------------------------------------------------------------------------------------------------------
// <copyright file="XicViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for fragment, precursor, and heavy XIC plots.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.Plots
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reactive.Linq;
    using InformedProteomics.Backend.MassSpecData;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.ViewModels.Data;
    using LcmsSpectator.ViewModels.Modifications;
    using OxyPlot.Axes;
    using ReactiveUI;

    /// <summary>
    /// View model for fragment, precursor, and heavy XIC plots.
    /// </summary>
    public class XicViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// LCMSRun for the data set that this XIC plot is part of.
        /// </summary>
        private readonly ILcMsRun lcms;

        /// <summary>
        /// The ratio of area under the curve for fragment XICs (light / heavy).
        /// </summary>
        private readonly ObservableAsPropertyHelper<string> fragmentAreaRatioLabel;

        /// <summary>
        /// The ratio of area under the curve for precursor XICs (light / heavy).
        /// </summary>
        private readonly ObservableAsPropertyHelper<string> precursorAreaRatioLabel;

        /// <summary>
        /// X Axis for the fragment XIC plot.
        /// </summary>
        private readonly LinearAxis fragmentXAxis;

        /// <summary>
        /// X Axis for heavy fragment XIC plot.
        /// </summary>
        private readonly LinearAxis heavyFragmentXAxis;

        /// <summary>
        /// X Axis for precursor XIC plot.
        /// </summary>
        private readonly LinearAxis precursorXAxis;

        /// <summary>
        /// X Axis for heavy precursor XIC plot.
        /// </summary>
        private readonly LinearAxis heavyPrecursorXAxis;

        /// <summary>
        /// A value indicating whether the fragment XICs are shown.
        /// </summary>
        private bool showFragmentXic;

        /// <summary>
        /// A value indicating whether the heavy XICs are shown.
        /// If the heavy XICs are being toggled on, it updates them.
        /// </summary>
        private bool showHeavy;

        /// <summary>
        /// A value indicating whether or not the change to an axis was caused
        /// by synchronizing axes.
        /// </summary>
        private bool axisInternalChange;

        /// <summary>
        /// The fragmentation sequence (fragment/precursor ion generator)
        /// </summary>
        private FragmentationSequence fragmentationSequence;

        /// <summary>
        /// Tracks which axes each axis is linked to.
        /// </summary>
        private readonly Dictionary<LinearAxis, List<LinearAxis>> linkedAxes;

        /// <summary>
        /// Tacks whether or not a link between two axes exists.
        /// </summary>
        private readonly HashSet<Tuple<LinearAxis, LinearAxis>> plotLinkageTracker;

        /// <summary>
        /// Initializes a new instance of the <see cref="XicViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">A dialog service for opening dialogs from the view model</param>
        /// <param name="lcms">the LCMSRun representing the raw file for this dataset.</param>
        public XicViewModel(IMainDialogService dialogService, ILcMsRun lcms)
        {
            this.dialogService = dialogService;
            this.lcms = lcms;
            this.fragmentXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom, Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            this.fragmentXAxis.AxisChanged += this.XAxisChanged;
            this.FragmentPlotViewModel = new XicPlotViewModel(
                                        this.dialogService,
                                        new FragmentationSequenceViewModel { AddPrecursorIons = false },
                                        lcms,
                                        "Fragment XIC",
                                        this.fragmentXAxis,
                                        false);
            this.heavyFragmentXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            this.heavyFragmentXAxis.AxisChanged += this.XAxisChanged;
            this.HeavyFragmentPlotViewModel = new XicPlotViewModel(
                this.dialogService,
                new FragmentationSequenceViewModel { AddPrecursorIons = false },
                lcms,
                "Heavy Fragment XIC",
                this.heavyFragmentXAxis,
                false,
                AxisPosition.Right);
            this.precursorXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            this.precursorXAxis.AxisChanged += this.XAxisChanged;
            this.PrecursorPlotViewModel = new XicPlotViewModel(this.dialogService, new PrecursorSequenceIonViewModel(), lcms, "Precursor XIC", this.precursorXAxis)
            {
                IsPlotUpdating = true,
            };

            this.heavyPrecursorXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            this.heavyPrecursorXAxis.AxisChanged += this.XAxisChanged;
            this.HeavyPrecursorPlotViewModel = new XicPlotViewModel(
                this.dialogService,
                new PrecursorSequenceIonViewModel(),
                lcms,
                "Heavy Precursor XIC",
                this.heavyPrecursorXAxis,
                vertAxes: AxisPosition.Right);

            this.showHeavy = false;
            this.showFragmentXic = false;
            var openHeavyModificationsCommand = ReactiveCommand.Create();
            openHeavyModificationsCommand.Subscribe(_ => this.OpenHeavyModificationsImplentation());
            this.OpenHeavyModificationsCommand = openHeavyModificationsCommand;

            this.linkedAxes = new Dictionary<LinearAxis, List<LinearAxis>>
            {
                { this.precursorXAxis, new List<LinearAxis>() },
                { this.fragmentXAxis, new List<LinearAxis>() },
                { this.heavyPrecursorXAxis, new List<LinearAxis>() },
                { this.heavyFragmentXAxis, new List<LinearAxis>() }
            };

            this.plotLinkageTracker = new HashSet<Tuple<LinearAxis, LinearAxis>>();
            this.TogglePlotLinks(this.fragmentXAxis, this.heavyFragmentXAxis);
            this.TogglePlotLinks(this.precursorXAxis, this.heavyPrecursorXAxis);
            this.TogglePlotLinks(this.precursorXAxis, this.fragmentXAxis);
            this.TogglePlotLinks(this.heavyPrecursorXAxis, this.heavyFragmentXAxis);

            // Update area ratios when the area of any of the plots changes
            this.WhenAny(x => x.FragmentPlotViewModel.Area, x => x.HeavyFragmentPlotViewModel.Area, (x, y) => x.Value / y.Value)
                .Select(this.FormatRatio)
                .ToProperty(this, x => x.FragmentAreaRatioLabel, out this.fragmentAreaRatioLabel);
            this.WhenAny(x => x.PrecursorPlotViewModel.Area, x => x.HeavyPrecursorPlotViewModel.Area, (x, y) => x.Value / y.Value)
                .Select(this.FormatRatio)
                .ToProperty(this, x => x.PrecursorAreaRatioLabel, out this.precursorAreaRatioLabel);
            this.WhenAnyValue(x => x.ShowFragmentXic, x => x.ShowHeavy)
                .Subscribe(x =>
                {
                    this.FragmentPlotViewModel.IsPlotUpdating = x.Item1;
                    this.HeavyFragmentPlotViewModel.IsPlotUpdating = x.Item1 && x.Item2;
                    this.HeavyPrecursorPlotViewModel.IsPlotUpdating = x.Item2;
                });
            this.WhenAnyValue(x => x.FragmentationSequence)
                .Where(fragSeq => fragSeq != null)
                .Subscribe(fragSeq =>
                {
                    this.FragmentPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    this.HeavyFragmentPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    this.PrecursorPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    this.HeavyPrecursorPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                });

            this.PrecursorToFragmentLinkLabel = "L";
            this.LightToHeavyLinkLabel = "L";

            this.LinkLightToHeavyCommand = ReactiveCommand.Create();
            this.LinkLightToHeavyCommand.Subscribe(
                _ =>
                    {
                        this.TogglePlotLinks(this.fragmentXAxis, this.heavyFragmentXAxis);
                        this.TogglePlotLinks(this.precursorXAxis, this.heavyPrecursorXAxis);
                        this.LightToHeavyLinkLabel = this.LightToHeavyLinkLabel == "L" ? "U" : "L";
                    });

            this.LinkPrecursorToFragmentCommand = ReactiveCommand.Create();
            this.LinkPrecursorToFragmentCommand.Subscribe(
                _ =>
                    {
                        this.TogglePlotLinks(this.precursorXAxis, this.fragmentXAxis);
                        this.TogglePlotLinks(this.heavyPrecursorXAxis, this.heavyFragmentXAxis);
                        this.PrecursorToFragmentLinkLabel = this.PrecursorToFragmentLinkLabel == "L" ? "U" : "L";
                    });
        }

        /// <summary>
        /// Gets view model for fragment XIC plot.
        /// </summary>
        public XicPlotViewModel FragmentPlotViewModel { get; private set; }

        /// <summary>
        /// Gets view model for heavy fragment XIC plot.
        /// </summary>
        public XicPlotViewModel HeavyFragmentPlotViewModel { get; private set; }

        /// <summary>
        /// Gets view model for precursor XIC plot.
        /// </summary>
        public XicPlotViewModel PrecursorPlotViewModel { get; private set; }

        /// <summary>
        /// Gets view model for heavy precursor XIC plot.
        /// </summary>
        public XicPlotViewModel HeavyPrecursorPlotViewModel { get; private set; }

        /// <summary>
        /// Gets command that opens window for selecting heavy modifications for light and heavy peptides.
        /// </summary>
        public ReactiveCommand<object> OpenHeavyModificationsCommand { get; private set; }

        /// <summary>
        /// Gets a command that links the Precursor XIC axes to the Fragment XIC axes.
        /// </summary>
        public ReactiveCommand<object> LinkLightToHeavyCommand { get; private set; }

        /// <summary>
        /// Gets a command that links the precursor XIC axes to the Heavy Precursor XIC axes.
        /// </summary>
        public ReactiveCommand<object> LinkPrecursorToFragmentCommand { get; private set; }

        private string lightToHeavyLinkLabel;

        public string LightToHeavyLinkLabel
        {
            get { return this.lightToHeavyLinkLabel; }
            private set { this.RaiseAndSetIfChanged(ref this.lightToHeavyLinkLabel, value); }
        }

        private string precursorToFragmentLinkLabel;

        public string PrecursorToFragmentLinkLabel
        {
            get { return this.precursorToFragmentLinkLabel; }
            private set { this.RaiseAndSetIfChanged(ref this.precursorToFragmentLinkLabel, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the fragment XICs are shown.
        /// </summary>
        public bool ShowFragmentXic
        {
            get { return this.showFragmentXic; }
            set { this.RaiseAndSetIfChanged(ref this.showFragmentXic, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the heavy XICs are shown.
        /// If the heavy XICs are being toggled on, it updates them.
        /// </summary>
        public bool ShowHeavy
        {
            get { return this.showHeavy; }
            set { this.RaiseAndSetIfChanged(ref this.showHeavy, value); }
        }

        /// <summary>
        /// Gets the ratio of area under the curve for fragment XICs (light / heavy).
        /// </summary>
        public string FragmentAreaRatioLabel
        {
            get { return this.fragmentAreaRatioLabel.Value; }
        }

        /// <summary>
        /// Gets the ratio of area under the curve for precursor XICs (light / heavy).
        /// </summary>
        public string PrecursorAreaRatioLabel
        {
            get { return this.precursorAreaRatioLabel.Value; }
        }

        /// <summary>
        /// Gets or sets the fragmentation sequence (fragment/precursor ion generator)
        /// </summary>
        public FragmentationSequence FragmentationSequence
        {
            get { return this.fragmentationSequence; }
            set { this.RaiseAndSetIfChanged(ref this.fragmentationSequence, value); }
        }

        /// <summary>
        /// Clear all XIC plots.
        /// </summary>
        public void ClearAll()
        {
            this.FragmentPlotViewModel.ClearPlot();
            this.PrecursorPlotViewModel.ClearPlot();
            this.HeavyFragmentPlotViewModel.ClearPlot();
            this.HeavyPrecursorPlotViewModel.ClearPlot();
        }

        /// <summary>
        /// Zoom all XICs to a given scan number.
        /// </summary>
        /// <param name="scanNum">Scan number to zoom to.</param>
        public void ZoomToScan(int scanNum)
        {
            var rt = this.lcms.GetElutionTime(scanNum);
            var range = this.lcms.GetElutionTime(this.lcms.MaxLcScan) - this.lcms.GetElutionTime(this.lcms.MinLcScan);
            var offset = range * 0.03;
            var min = Math.Max(rt - offset, 0);
            var max = Math.Min(rt + offset, this.lcms.MaxLcScan);
            this.precursorXAxis.Minimum = min;
            this.precursorXAxis.Maximum = max;
            this.precursorXAxis.Zoom(min, max);
        }

        /// <summary>
        /// Set the selected scan marker on all XIC plots
        /// </summary>
        /// <param name="scan">Scan number to put marker at</param>
        public void SetSelectedScan(int scan)
        {
            this.FragmentPlotViewModel.SelectedScan = scan;
            this.PrecursorPlotViewModel.SelectedScan = scan;
            this.HeavyFragmentPlotViewModel.SelectedScan = scan;
            this.HeavyPrecursorPlotViewModel.SelectedScan = scan;
        }

        /// <summary>
        /// Get an observable that is triggered when the selected scan of any XIC plot is changed
        /// </summary>
        /// <returns>IObservable of integer (selected scan #)</returns>
        public IObservable<int> SelectedScanUpdated()
        {
            return Observable.Merge(
                 this.FragmentPlotViewModel.WhenAnyValue(x => x.SelectedScan),
                 this.PrecursorPlotViewModel.WhenAnyValue(x => x.SelectedScan),
                 this.HeavyFragmentPlotViewModel.WhenAnyValue(x => x.SelectedScan),
                 this.HeavyPrecursorPlotViewModel.WhenAnyValue(x => x.SelectedScan));
        }

        /// <summary>
        /// Event handler for XAxis changed to update area ratio labels when shared x axis is zoomed or panned.
        /// </summary>
        /// <param name="sender">The x Axis that sent the event</param>
        /// <param name="e">The event arguments</param>
        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (!this.axisInternalChange)
            {
                this.axisInternalChange = true;
                this.axisInternalChange = true;
                var axis = sender as LinearAxis;
                if (axis == null)
                {
                    return;
                }

                this.UpdateAxesRecursive(axis, new HashSet<LinearAxis>());
                this.axisInternalChange = false;
            }
        }

        private void UpdateAxesRecursive(LinearAxis sender, HashSet<LinearAxis> alreadyUpdated)
        {
            if (alreadyUpdated.Contains(sender))
            {
                return;
            }

            alreadyUpdated.Add(sender);
            if (this.linkedAxes.ContainsKey(sender))
            {
                var links = this.linkedAxes[sender];
                foreach (var link in links)
                {
                    link.Zoom(sender.ActualMinimum, sender.ActualMaximum);
                    this.UpdateAxesRecursive(link, alreadyUpdated);
                }
            }
        }

        /// <summary>
        /// Toggle the linkage between two plot's axes.
        /// </summary>
        /// <param name="axis1">First axis.</param>
        /// <param name="axis2">Second axis.</param>
        private void TogglePlotLinks(LinearAxis axis1, LinearAxis axis2)
        {
            var key = new Tuple<LinearAxis, LinearAxis>(axis1, axis2);
            if (this.plotLinkageTracker.Contains(key))
            {
                this.RemoveLinks(axis1, axis2);
                this.plotLinkageTracker.Remove(key);
            }
            else
            {
                this.AddLinks(axis1, axis2);
                this.plotLinkageTracker.Add(key);
            }
        }

        /// <summary>
        /// Link two axes together so they scroll together.
        /// </summary>
        /// <param name="axis1">First axis to link.</param>
        /// <param name="axis2">Second axis to link.</param>
        private void AddLinks(LinearAxis axis1, LinearAxis axis2)
        {
            this.AddLink(axis1, axis2);
            this.AddLink(axis2, axis1);
        }

        /// <summary>
        /// Link the second axis to the first so the second one scrolls when the first does.
        /// </summary>
        /// <param name="axis1">The first axis.</param>
        /// <param name="axis2">The second axis.</param>
        private void AddLink(LinearAxis axis1, LinearAxis axis2)
        {
            if (this.linkedAxes.ContainsKey(axis1))
            {
                var links = this.linkedAxes[axis1];
                if (!links.Contains(axis2))
                {
                    links.Add(axis2);
                }
            }
        }

        /// <summary>
        /// Unlink two axes together so they do not scroll together.
        /// </summary>
        /// <param name="axis1">First axis to unlink.</param>
        /// <param name="axis2">Second axis to unlink.</param>
        private void RemoveLinks(LinearAxis axis1, LinearAxis axis2)
        {
            this.RemoveLink(axis1, axis2);
            this.RemoveLink(axis2, axis1);
        }

        /// <summary>
        /// Unlink the second axis to the first so the second one does not scroll when the first does.
        /// </summary>
        /// <param name="axis1">The first axis.</param>
        /// <param name="axis2">The second axis.</param>
        private void RemoveLink(LinearAxis axis1, LinearAxis axis2)
        {
            if (this.linkedAxes.ContainsKey(axis1))
            {
                var links = this.linkedAxes[axis1];
                if (links.Contains(axis2))
                {
                    links.Remove(axis2);
                }
            }
        }

        /// <summary>
        /// Implementation for OpenHeavyModificationsCommand.
        /// Open window for selecting heavy modifications for light and heavy peptides.
        /// </summary>
        private void OpenHeavyModificationsImplentation()
        {
            var heavyModificationsWindowVm = new HeavyModificationsWindowViewModel(this.dialogService);
            this.dialogService.OpenHeavyModifications(heavyModificationsWindowVm);
        }

        /// <summary>
        /// Format the area under the curve ratio as a string.
        /// </summary>
        /// <param name="ratio">Area under the curve ratio.</param>
        /// <returns>Area under the curve ratio formatted as a string.</returns>
        private string FormatRatio(double ratio)
        {
            if (ratio.Equals(double.NaN) || ratio < 0)
            {
                ratio = 0.0;
            }

            string formatted;
            if (ratio > 1000 || ratio < 0.001)
            {
                formatted = string.Format("{0:0.###EE0}", ratio);
            }
            else
            {
                formatted = Math.Round(ratio, 3).ToString(CultureInfo.InvariantCulture);
            }

            return formatted;
        }
    }
}
