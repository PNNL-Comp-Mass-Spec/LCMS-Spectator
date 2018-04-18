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
    using System.Linq;
    using System.Reactive.Linq;
    using InformedProteomics.Backend.MassSpecData;

    using Config;
    using DialogServices;
    using Models;
    using Data;
    using Modifications;
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
            fragmentXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom, Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            fragmentXAxis.AxisChanged += XAxisChanged;
            FragmentPlotViewModel = new XicPlotViewModel(
                                        this.dialogService,
                                        new FragmentationSequenceViewModel { AddPrecursorIons = false },
                                        lcms,
                                        "Fragment XIC",
                                        fragmentXAxis,
                                        false);
            heavyFragmentXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            heavyFragmentXAxis.AxisChanged += XAxisChanged;
            HeavyFragmentPlotViewModel = new XicPlotViewModel(
                this.dialogService,
                new FragmentationSequenceViewModel { AddPrecursorIons = false },
                lcms,
                "Heavy Fragment XIC",
                heavyFragmentXAxis,
                false,
                AxisPosition.Right);
            precursorXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            precursorXAxis.AxisChanged += XAxisChanged;
            PrecursorPlotViewModel = new XicPlotViewModel(this.dialogService, new PrecursorSequenceIonViewModel(), lcms, "Precursor XIC", precursorXAxis)
            {
                IsPlotUpdating = true,
            };

            heavyPrecursorXAxis = new LinearAxis
            {
                StringFormat = "0.###",
                Position = AxisPosition.Bottom,
                Title = "Retention Time",
                AbsoluteMinimum = lcms.MinLcScan,
                AbsoluteMaximum = lcms.MaxLcScan + 1
            };
            heavyPrecursorXAxis.AxisChanged += XAxisChanged;
            HeavyPrecursorPlotViewModel = new XicPlotViewModel(
                this.dialogService,
                new PrecursorSequenceIonViewModel(),
                lcms,
                "Heavy Precursor XIC",
                heavyPrecursorXAxis,
                vertAxes: AxisPosition.Right);

            showHeavy = false;
            showFragmentXic = false;
            var openHeavyModificationsCommand = ReactiveCommand.Create();
            openHeavyModificationsCommand.Subscribe(_ => OpenHeavyModificationsImplentation());
            OpenHeavyModificationsCommand = openHeavyModificationsCommand;

            PrecursorPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.LightModifications.ToArray();
            FragmentPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.LightModifications.ToArray();
            HeavyPrecursorPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.HeavyModifications.ToArray();
            HeavyFragmentPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.HeavyModifications.ToArray();

            linkedAxes = new Dictionary<LinearAxis, List<LinearAxis>>
            {
                { precursorXAxis, new List<LinearAxis>() },
                { fragmentXAxis, new List<LinearAxis>() },
                { heavyPrecursorXAxis, new List<LinearAxis>() },
                { heavyFragmentXAxis, new List<LinearAxis>() }
            };

            plotLinkageTracker = new HashSet<Tuple<LinearAxis, LinearAxis>>();
            TogglePlotLinks(fragmentXAxis, heavyFragmentXAxis);
            TogglePlotLinks(precursorXAxis, heavyPrecursorXAxis);
            TogglePlotLinks(precursorXAxis, fragmentXAxis);
            TogglePlotLinks(heavyPrecursorXAxis, heavyFragmentXAxis);

            // Update area ratios when the area of any of the plots changes
            this.WhenAny(x => x.FragmentPlotViewModel.Area, x => x.HeavyFragmentPlotViewModel.Area, (x, y) => x.Value / y.Value)
                .Select(FormatRatio)
                .ToProperty(this, x => x.FragmentAreaRatioLabel, out fragmentAreaRatioLabel);
            this.WhenAny(x => x.PrecursorPlotViewModel.Area, x => x.HeavyPrecursorPlotViewModel.Area, (x, y) => x.Value / y.Value)
                .Select(FormatRatio)
                .ToProperty(this, x => x.PrecursorAreaRatioLabel, out precursorAreaRatioLabel);
            this.WhenAnyValue(x => x.ShowFragmentXic, x => x.ShowHeavy)
                .Subscribe(x =>
                {
                    FragmentPlotViewModel.IsPlotUpdating = x.Item1;
                    HeavyFragmentPlotViewModel.IsPlotUpdating = x.Item1 && x.Item2;
                    HeavyPrecursorPlotViewModel.IsPlotUpdating = x.Item2;
                });
            this.WhenAnyValue(x => x.FragmentationSequence)
                .Where(fragSeq => fragSeq != null)
                .Subscribe(fragSeq =>
                {
                    FragmentPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    HeavyFragmentPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    PrecursorPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                    HeavyPrecursorPlotViewModel.FragmentationSequenceViewModel.FragmentationSequence = fragSeq;
                });

            PrecursorToFragmentLinkLabel = "L";
            LightToHeavyLinkLabel = "L";

            LinkLightToHeavyCommand = ReactiveCommand.Create();
            LinkLightToHeavyCommand.Subscribe(
                _ =>
                    {
                        TogglePlotLinks(fragmentXAxis, heavyFragmentXAxis);
                        TogglePlotLinks(precursorXAxis, heavyPrecursorXAxis);
                        LightToHeavyLinkLabel = LightToHeavyLinkLabel == "L" ? "U" : "L";
                    });

            LinkPrecursorToFragmentCommand = ReactiveCommand.Create();
            LinkPrecursorToFragmentCommand.Subscribe(
                _ =>
                    {
                        TogglePlotLinks(precursorXAxis, fragmentXAxis);
                        TogglePlotLinks(heavyPrecursorXAxis, heavyFragmentXAxis);
                        PrecursorToFragmentLinkLabel = PrecursorToFragmentLinkLabel == "L" ? "U" : "L";
                    });
        }

        /// <summary>
        /// Gets view model for fragment XIC plot.
        /// </summary>
        public XicPlotViewModel FragmentPlotViewModel { get; }

        /// <summary>
        /// Gets view model for heavy fragment XIC plot.
        /// </summary>
        public XicPlotViewModel HeavyFragmentPlotViewModel { get; }

        /// <summary>
        /// Gets view model for precursor XIC plot.
        /// </summary>
        public XicPlotViewModel PrecursorPlotViewModel { get; }

        /// <summary>
        /// Gets view model for heavy precursor XIC plot.
        /// </summary>
        public XicPlotViewModel HeavyPrecursorPlotViewModel { get; }

        /// <summary>
        /// Gets command that opens window for selecting heavy modifications for light and heavy peptides.
        /// </summary>
        public ReactiveCommand<object> OpenHeavyModificationsCommand { get; }

        /// <summary>
        /// Gets a command that links the Precursor XIC axes to the Fragment XIC axes.
        /// </summary>
        public ReactiveCommand<object> LinkLightToHeavyCommand { get; }

        /// <summary>
        /// Gets a command that links the precursor XIC axes to the Heavy Precursor XIC axes.
        /// </summary>
        public ReactiveCommand<object> LinkPrecursorToFragmentCommand { get; }

        private string lightToHeavyLinkLabel;

        public string LightToHeavyLinkLabel
        {
            get => lightToHeavyLinkLabel;
            private set => this.RaiseAndSetIfChanged(ref lightToHeavyLinkLabel, value);
        }

        private string precursorToFragmentLinkLabel;

        public string PrecursorToFragmentLinkLabel
        {
            get => precursorToFragmentLinkLabel;
            private set => this.RaiseAndSetIfChanged(ref precursorToFragmentLinkLabel, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the fragment XICs are shown.
        /// </summary>
        public bool ShowFragmentXic
        {
            get => showFragmentXic;
            set => this.RaiseAndSetIfChanged(ref showFragmentXic, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the heavy XICs are shown.
        /// If the heavy XICs are being toggled on, it updates them.
        /// </summary>
        public bool ShowHeavy
        {
            get => showHeavy;
            set => this.RaiseAndSetIfChanged(ref showHeavy, value);
        }

        /// <summary>
        /// Gets the ratio of area under the curve for fragment XICs (light / heavy).
        /// </summary>
        public string FragmentAreaRatioLabel => fragmentAreaRatioLabel.Value;

        /// <summary>
        /// Gets the ratio of area under the curve for precursor XICs (light / heavy).
        /// </summary>
        public string PrecursorAreaRatioLabel => precursorAreaRatioLabel.Value;

        /// <summary>
        /// Gets or sets the fragmentation sequence (fragment/precursor ion generator)
        /// </summary>
        public FragmentationSequence FragmentationSequence
        {
            get => fragmentationSequence;
            set => this.RaiseAndSetIfChanged(ref fragmentationSequence, value);
        }

        /// <summary>
        /// Clear all XIC plots.
        /// </summary>
        public void ClearAll()
        {
            FragmentPlotViewModel.ClearPlot();
            PrecursorPlotViewModel.ClearPlot();
            HeavyFragmentPlotViewModel.ClearPlot();
            HeavyPrecursorPlotViewModel.ClearPlot();
        }

        /// <summary>
        /// Zoom all XICs to a given scan number.
        /// </summary>
        /// <param name="scanNum">Scan number to zoom to.</param>
        public void ZoomToScan(int scanNum)
        {
            var rt = lcms.GetElutionTime(scanNum);
            var range = lcms.GetElutionTime(lcms.MaxLcScan) - lcms.GetElutionTime(lcms.MinLcScan);
            var offset = range * 0.03;
            var min = Math.Max(rt - offset, 0);
            var max = Math.Min(rt + offset, lcms.MaxLcScan);
            precursorXAxis.Minimum = min;
            precursorXAxis.Maximum = max;
            precursorXAxis.Zoom(min, max);
        }

        /// <summary>
        /// Set the selected scan marker on all XIC plots
        /// </summary>
        /// <param name="scan">Scan number to put marker at</param>
        public void SetSelectedScan(int scan)
        {
            FragmentPlotViewModel.SelectedScan = scan;
            PrecursorPlotViewModel.SelectedScan = scan;
            HeavyFragmentPlotViewModel.SelectedScan = scan;
            HeavyPrecursorPlotViewModel.SelectedScan = scan;
        }

        /// <summary>
        /// Get an observable that is triggered when the selected scan of any XIC plot is changed
        /// </summary>
        /// <returns>IObservable of integer (selected scan #)</returns>
        public IObservable<int> SelectedScanUpdated()
        {
            return Observable.Merge(
                 FragmentPlotViewModel.WhenAnyValue(x => x.SelectedScan),
                 PrecursorPlotViewModel.WhenAnyValue(x => x.SelectedScan),
                 HeavyFragmentPlotViewModel.WhenAnyValue(x => x.SelectedScan),
                 HeavyPrecursorPlotViewModel.WhenAnyValue(x => x.SelectedScan));
        }

        /// <summary>
        /// Event handler for XAxis changed to update area ratio labels when shared x axis is zoomed or panned.
        /// </summary>
        /// <param name="sender">The x Axis that sent the event</param>
        /// <param name="e">The event arguments</param>
        private void XAxisChanged(object sender, AxisChangedEventArgs e)
        {
            if (!axisInternalChange)
            {
                axisInternalChange = true;
                axisInternalChange = true;
                if (!(sender is LinearAxis axis))
                {
                    return;
                }

                UpdateAxesRecursive(axis, new HashSet<LinearAxis>());
                axisInternalChange = false;
            }
        }

        private void UpdateAxesRecursive(LinearAxis sender, ISet<LinearAxis> alreadyUpdated)
        {
            if (alreadyUpdated.Contains(sender))
            {
                return;
            }

            alreadyUpdated.Add(sender);
            if (linkedAxes.ContainsKey(sender))
            {
                var links = linkedAxes[sender];
                foreach (var link in links)
                {
                    link.Zoom(sender.ActualMinimum, sender.ActualMaximum);
                    UpdateAxesRecursive(link, alreadyUpdated);
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
            if (plotLinkageTracker.Contains(key))
            {
                RemoveLinks(axis1, axis2);
                plotLinkageTracker.Remove(key);
            }
            else
            {
                AddLinks(axis1, axis2);
                plotLinkageTracker.Add(key);
            }
        }

        /// <summary>
        /// Link two axes together so they scroll together.
        /// </summary>
        /// <param name="axis1">First axis to link.</param>
        /// <param name="axis2">Second axis to link.</param>
        private void AddLinks(LinearAxis axis1, LinearAxis axis2)
        {
            AddLink(axis1, axis2);
            AddLink(axis2, axis1);
        }

        /// <summary>
        /// Link the second axis to the first so the second one scrolls when the first does.
        /// </summary>
        /// <param name="axis1">The first axis.</param>
        /// <param name="axis2">The second axis.</param>
        private void AddLink(LinearAxis axis1, LinearAxis axis2)
        {
            if (linkedAxes.ContainsKey(axis1))
            {
                var links = linkedAxes[axis1];
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
            RemoveLink(axis1, axis2);
            RemoveLink(axis2, axis1);
        }

        /// <summary>
        /// Unlink the second axis to the first so the second one does not scroll when the first does.
        /// </summary>
        /// <param name="axis1">The first axis.</param>
        /// <param name="axis2">The second axis.</param>
        private void RemoveLink(LinearAxis axis1, LinearAxis axis2)
        {
            if (linkedAxes.ContainsKey(axis1))
            {
                var links = linkedAxes[axis1];
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
            var heavyModificationsWindowVm = new HeavyModificationsWindowViewModel(dialogService);
            dialogService.OpenHeavyModifications(heavyModificationsWindowVm);
            PrecursorPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.LightModifications.ToArray();
            FragmentPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.LightModifications.ToArray();
            HeavyPrecursorPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.HeavyModifications.ToArray();
            HeavyFragmentPlotViewModel.FragmentationSequenceViewModel.HeavyModifications = IcParameters.Instance.HeavyModifications.ToArray();
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
