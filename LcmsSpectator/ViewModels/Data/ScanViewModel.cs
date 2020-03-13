// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScanViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maintains a filterable list of Protein-Spectrum-Match identifications.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.Utils;
using LcmsSpectator.ViewModels.Filters;
using LcmsSpectator.Writers.Exporters;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Data
{
    /// <summary>
    /// This class maintains a filterable list of Protein-Spectrum-Match identifications.
    /// </summary>
    public class ScanViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from the view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// The full set of unfiltered data.
        /// </summary>
        private ReactiveList<PrSm> data;

        /// <summary>
        /// The filtered data.
        /// </summary>
        private PrSm[] filteredData;

        /// <summary>
        /// A list of IDs grouped by protein name.
        /// </summary>
        private ReactiveList<ProteinId> filteredProteins;

        /// <summary>
        /// The selected Protein-Spectrum-Match identification.
        /// </summary>
        private PrSm selectedPrSm;

        /// <summary>
        /// Gets or sets the object selected in TreeView. Uses weak typing because each level TreeView is a different data type.
        /// </summary>
        private object treeViewSelectedItem;

        private bool showNativeId = false;
        private bool showDriftTime = false;

        /// <summary>
        /// The hierarchical tree of identifications.
        /// </summary>
        private IdentificationTree idTree;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from the view model.</param>
        /// <param name="ids">The Protein-Spectrum-Match identifications to display.</param>
        public ScanViewModel(IMainDialogService dialogService, IEnumerable<PrSm> ids)
        {
            ClearFiltersCommand = ReactiveCommand.Create(ClearFilters);
            this.dialogService = dialogService;

            FilteredData = new PrSm[0];
            FilteredProteins = new ReactiveList<ProteinId>();

            Filters = new ReactiveList<IFilter> { ChangeTrackingEnabled = true };

            Data = new ReactiveList<PrSm> { ChangeTrackingEnabled = true };
            InitializeDefaultFilters();
            Data.AddRange(ids);

            IdTree = new IdentificationTree();

            // When a filter is selected/unselected, request a filter value if selected, then filter data
            Filters.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Select(x => x.Sender)
                .Where(sender => !sender.Selected || sender.Name == "Hide Unidentified Scans" || this.dialogService.FilterBox(sender))
                .SelectMany(async _ => await FilterDataAsync(Data))
                .Subscribe(fd => FilteredData = fd);

            // Data changes when items are added or removed
            Data.CountChanged.Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                .SelectMany(async _ => await FilterDataAsync(Data))
                .Subscribe(fd => FilteredData = fd);

            // When data is filtered, group it by protein name
            this.WhenAnyValue(x => x.FilteredData).ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(async filteredData =>
            {
                IdTree.ClearIds();
                await IdTree.BuildIdTree(filteredData);
                FilteredProteins.Clear();
                foreach (var protein in IdTree.ProteinIds)
                {
                    FilteredProteins.Add(protein);
                }
            });

            // When data is filtered and a PRSM has not been selected yet, select the first PRSM
            this.WhenAnyValue(x => x.FilteredData)
                .Where(fd => fd.Length > 0)
                .Where(_ => SelectedPrSm == null)
                .Subscribe(fd => SelectedPrSm = fd[0]);

            // When a tree object is selected and it is a PRSM, set the selected PRSM
            this.WhenAnyValue(x => x.TreeViewSelectedItem)
                .Select(x => x as PrSm)
                .Where(p => p != null)
                .Subscribe(p => SelectedPrSm = p);

            // When a tree object is selected and it is an IIdData, set the selected PRSM
            this.WhenAnyValue(x => x.TreeViewSelectedItem)
                .Select(x => x as IIdData)
                .Where(p => p != null)
                .Subscribe(p => SelectedPrSm = p.GetHighestScoringPrSm());

            // When a PrSm's sequence changes, update its score.
            Data.ItemChanged.Where(x => x.PropertyName == "Sequence")
                .Select(x => x.Sender)
                .Where(sender => sender.Sequence.Count > 0)
                .Subscribe(UpdatePrSmScore);

            ExportSpectraCommand = ReactiveCommand.CreateFromTask(ExportSpectraImplementation);
            ExportPeaksCommand = ReactiveCommand.CreateFromTask(ExportPeaksImplementation);
            ExportProteinTreeCommand = ReactiveCommand.Create(ExportProteinTreeImplementation);

            ExportProteinTreeAsTsvCommand = ReactiveCommand.Create(ExportProteinTreeAsTsvImplementation);

        }

        /// <summary>
        /// Gets a command that clears all the filters.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ClearFiltersCommand { get; }

        /// <summary>
        /// Gets a command that exports the spectra plots for the selected identifications.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExportSpectraCommand { get; }

        /// <summary>
        /// Gets a command that exports the spectra peaks to TSV for the selected identifications.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExportPeaksCommand { get; }

        /// <summary>
        /// Gets a command that exports the protein tree as a hierarchy.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExportProteinTreeCommand { get; }

        /// <summary>
        /// Gets a command that exports the protein tree as a tab separated value file.
        /// </summary>
        public ReactiveCommand<Unit, Unit> ExportProteinTreeAsTsvCommand { get; }

        /// <summary>
        /// Gets the list of possible filters.
        /// </summary>
        public ReactiveList<IFilter> Filters { get; }

        /// <summary>
        /// Gets all unfiltered data.
        /// </summary>
        public ReactiveList<PrSm> Data
        {
            get => data;
            private set => this.RaiseAndSetIfChanged(ref data, value);
        }

        /// <summary>
        /// Gets the filtered data.
        /// </summary>
        public PrSm[] FilteredData
        {
            get => filteredData;
            private set => this.RaiseAndSetIfChanged(ref filteredData, value);
        }

        /// <summary>
        /// Gets a list of IDs grouped by protein name.
        /// </summary>
        public ReactiveList<ProteinId> FilteredProteins
        {
            get => filteredProteins;
            private set => this.RaiseAndSetIfChanged(ref filteredProteins, value);
        }

        /// <summary>
        /// Gets the hierarchical tree of identifications.
        /// </summary>
        public IdentificationTree IdTree
        {
            get => idTree;
            private set => this.RaiseAndSetIfChanged(ref idTree, value);
        }

        /// <summary>
        /// Gets or sets the selected Protein-Spectrum-Match identification.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get => selectedPrSm;
            set => this.RaiseAndSetIfChanged(ref selectedPrSm, value);
        }

        /// <summary>
        /// Gets or sets the object selected in TreeView. Uses weak typing because each level TreeView is a different data type.
        /// </summary>
        public object TreeViewSelectedItem
        {
            get => treeViewSelectedItem;
            set => this.RaiseAndSetIfChanged(ref treeViewSelectedItem, value);
        }

        /// <summary>
        /// If the NativeID column should be visible
        /// </summary>
        public bool ShowNativeId
        {
            get => showNativeId;
            set => this.RaiseAndSetIfChanged(ref showNativeId, value);
        }

        /// <summary>
        /// If the DriftTime column should be visible
        /// </summary>
        public bool ShowDriftTime
        {
            get => showDriftTime;
            set => this.RaiseAndSetIfChanged(ref showDriftTime, value);
        }

        /// <summary>
        /// Gets or sets the scorer factory used to score PRSMs on-the-fly.
        /// </summary>
        public ScorerFactory ScorerFactory { get; set; }

        /// <summary>
        /// Update the filters to only show proteins with this scan number
        /// Set to 0 to remove the filter
        /// </summary>
        /// <param name="scanNumber"></param>
        public async void ApplyScanFilter(int scanNumber)
        {

            foreach (var filter in Filters)
            {
                if (filter.Name != "Scan" || !(filter is MultiValueFilterViewModel scanFilter))
                    continue;

                if (scanNumber <= 0)
                {
                    if (scanFilter.Selected)
                    {
                        // Setting Selected to False will auto-update the proteins to remove the filter
                        scanFilter.Selected = false;
                    }
                    return;
                }

                var scanNumberText = scanNumber.ToString();

                if (scanFilter.Values.Count > 1)
                {
                    scanFilter.Values.Clear();
                }
                else if (scanFilter.Values.Count == 1)
                {
                    if (!scanFilter.Values.First().Equals(scanNumberText))
                    {
                        scanFilter.Values[0] = scanNumberText;
                    }
                }
                else
                {
                    scanFilter.Values.Add(scanNumberText);
                }

                scanFilter.Value = scanNumberText;

                if (!scanFilter.Selected)
                {
                    // Setting Selected to True auto-updates the proteins to apply the filter
                    scanFilter.Selected = true;
                    return;
                }

                break;
            }

            // Update the filtered data
            FilteredData = await FilterDataAsync(Data);
        }

        /// <summary>
        /// Clear all filters and filter the data.
        /// </summary>
        public void ClearFilters()
        {
            foreach (var filter in Filters)
            {
                filter.Selected = false;
            }
        }

        /// <summary>
        /// Remove Protein-Spectrum-Matches from IDTree that are associated with raw file
        /// </summary>
        /// <param name="rawFileName">Name of raw file</param>
        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newData = Data.Where(prsm => prsm.RawFileName != rawFileName).ToList();
            if (!newData.Contains(SelectedPrSm))
            {
                SelectedPrSm = null;
            }

            Data.Clear();
            Data.AddRange(newData);
        }

        /// <summary>
        /// Shows or hides instrument Precursor m/z and mass from the instrument
        /// Reads data from LCMSRun if necessary
        /// </summary>
        /// <param name="value">Should the instrument data be shown?</param>
        /// <param name="lcmsRun">LCMSRun for this data set.</param>
        /// <returns>Asynchronous task.</returns>
        public async Task ToggleShowInstrumentDataAsync(bool value, ILcMsRun lcmsRun)
        {
            if (value)
            {
                if (lcmsRun != null)
                {
                    var scans = Data;
                    foreach (var scan in scans.Where(scan => scan.Sequence.Count == 0))
                    {
                        var scan1 = scan;
                        var isolationWindow = await Task.Run(() => lcmsRun.GetIsolationWindow(scan1.Scan));
                        scan.PrecursorMz = isolationWindow.MonoisotopicMz ?? double.NaN;
                        scan.Charge = isolationWindow.Charge ?? 0;
                        scan.Mass = isolationWindow.MonoisotopicMass ?? double.NaN;
                    }
                }
            }
            else
            {
                var scans = Data;
                foreach (var scan in scans.Where(scan => scan.Sequence.Count == 0))
                {
                    scan.PrecursorMz = double.NaN;
                    scan.Charge = 0;
                    scan.Mass = double.NaN;
                }
            }
        }

        /// <summary>
        /// Clear all data.
        /// </summary>
        public void ClearIds()
        {
            Data.Clear();
        }

        /// <summary>
        /// Filter data by filter values asynchronously.
        /// </summary>
        /// <param name="da">The data to filtered.</param>
        /// <returns>The filtered data.</returns>
        private Task<PrSm[]> FilterDataAsync(IEnumerable<PrSm> da)
        {
            return Task.Run(() => FilterData(da));
        }

        /// <summary>
        /// Filter data by filter values.
        /// </summary>
        /// <param name="da">The data to filtered.</param>
        /// <returns>The filtered data.</returns>
        private PrSm[] FilterData(IEnumerable<PrSm> da)
        {
            IEnumerable<object> filtered = new List<PrSm>(da);
            var selectedFilters = Filters.Where(f => f.Selected);
            filtered = selectedFilters.Aggregate(filtered, (current, filter) => filter.Filter(current));
            var filteredPrSms = filtered.Cast<PrSm>();

            var allPrSmsByScan = new Dictionary<int, List<PrSm>>();

            // Ensure that all scan numbers for the data set are unique.
            foreach (var prsm in filteredPrSms)
            {
                if (!allPrSmsByScan.ContainsKey(prsm.Scan))
                {
                    allPrSmsByScan.Add(prsm.Scan, new List<PrSm> { prsm });
                }
                else if (allPrSmsByScan[prsm.Scan][0].Sequence.Count == 0)
                {
                    allPrSmsByScan[prsm.Scan] = new List<PrSm> { prsm };
                }
                else
                {
                    allPrSmsByScan[prsm.Scan].Add(prsm);
                }
            }

            var uniqueFilteredPrSms = allPrSmsByScan.Values.SelectMany(prsm => prsm).ToArray();
            Array.Sort(uniqueFilteredPrSms, new PrSm.PrSmScoreComparer());
            return uniqueFilteredPrSms;
        }

        /// <summary>
        /// Initialize possible default filters.
        /// </summary>
        private void InitializeDefaultFilters()
        {
            // Filter by scan #
            Filters.Add(new MultiValueFilterViewModel(
                "Scan",
                "Filter by Scan number",
                "Enter scan numbers to filter by separated by a comma",
                (d, v) => d.Where(p => v.Any(val => ((PrSm)p).Scan == Convert.ToInt32(val))),
                o =>
                {
                    var str = o as string;
                    return int.TryParse(str, out _);
                },
                dialogService,
                null,
                ','));

            // Filter by subsequence
            Filters.Add(new MultiValueFilterViewModel(
                    "Sequence",
                    "Filter by Sequence",
                    "Enter Sequence to filter by:",
                    (d, v) => d.Where(p => v.Any(val => ((PrSm)p).SequenceText.Contains(val))),
                    o => true,
                    dialogService,
                    (from prsm in Data where prsm.SequenceText.Length > 0 select prsm.SequenceText).Distinct()));

            // Filter by protein name
            Filters.Add(new MultiValueFilterViewModel(
                    "Protein Name",
                    "Filter by Protein Name",
                    "Enter protein name to filter by:",
                    (d, v) => d.Where(p => v.Any(val => ((PrSm)p).ProteinName.Contains(val))),
                    o => true,
                    dialogService,
                    (from prsm in Data where prsm.ProteinName.Length > 0 select prsm.ProteinName).Distinct()));

            // Filter by mass
            Filters.Add(new FilterViewModel(
                    "Mass",
                    "Filter by Mass",
                    "Enter minimum Mass to display:",
                    (d, v) => d.Where(datum => ((PrSm)datum).Mass >= Convert.ToDouble(v)),
                    o =>
                        {
                            var str = o as string;
                            return double.TryParse(str, out _);
                        },
                    dialogService));

            // Filter by most abundant isotope m/z
            Filters.Add(new FilterViewModel(
                    "Most Abundant Isotope m/z",
                    "Filter by Most Abundant Isotope M/Z",
                    "Enter minimum M/Z to display:",
                    (d, v) => d.Where(datum => ((PrSm)datum).PrecursorMz >= Convert.ToDouble(v)),
                    o =>
                        {
                            var str = o as string;
                            return double.TryParse(str, out _);
                        },
                    dialogService));

            // Filter by charge state
            Filters.Add(new MultiValueFilterViewModel(
                    "Charge",
                    "Filter by Charge",
                    "Enter Charge to filter by:",
                    (d, v) => d.Where(p => v.Any(val => ((PrSm)p).Charge == Convert.ToInt32(val))),
                    o =>
                        {
                            var str = o as string;
                            return int.TryParse(str, out _);
                        },
                    dialogService,
                    (from prsm in Data where prsm.Charge > 0 select prsm.Charge.ToString(CultureInfo.InvariantCulture)).Distinct()));

            // Filter by score
            Filters.Add(new FilterViewModel(
                    "Score",
                    "Filter by Score",
                    "Enter minimum score to display:",
                    (d, v) =>
                       {
                            var score = Convert.ToDouble(v);
                            return d.Where(
                                datum =>
                                    {
                                        var prsm = (PrSm)datum;
                                        return (prsm.UseGolfScoring && prsm.Score <= score)
                                               || (!prsm.UseGolfScoring && prsm.Score >= score);
                                    });
                        },
                    o =>
                        {
                            var str = o as string;
                            return double.TryParse(str, out _);
                        },
                    dialogService));

            // Filter by QValue
            Filters.Add(new FilterViewModel(
                    "QValue",
                    "Filter by QValue",
                    "Enter minimum QValue to display:",
                    (d, v) => d.Where(datum => ((PrSm)datum).QValue <= Convert.ToDouble(v)),
                    o =>
                        {
                            var str = o as string;
                            return double.TryParse(str, out _);
                        },
                    dialogService,
                    null,
                    "0.01") { Selected = true });

            // Remove unidentified scans
            Filters.Add(new FilterViewModel(
                    "Hide Unidentified Scans",
                    string.Empty,
                    string.Empty,
                    (d, v) => d.Where(datum => ((PrSm)datum).Sequence.Count > 0),
                    o => true,
                    dialogService));

            // Filter by raw file name
            Filters.Add(new FilterViewModel(
                    "Raw File Name",
                    "Filter by data set name",
                    "Enter data set to filter by:",
                    (d, v) => d.Where(datum => ((PrSm)datum).RawFileName.Contains((string)v)),
                    o => true,
                    dialogService,
                    (from prsm in Data where prsm.RawFileName.Length > 0 select prsm.RawFileName).Distinct()));
        }

        /// <summary>
        /// Score a <see cref="PrSm" /> based on its sequence and MS/MS spectrum.
        /// </summary>
        /// <param name="prsm">The <see cref="PrSm" /> to score.</param>
        private void UpdatePrSmScore(PrSm prsm)
        {
            if (ScorerFactory == null)
            {
                return;
            }

            var ms2Spectrum = prsm.Ms2Spectrum;
            if (ms2Spectrum == null)
            {
                return;
            }

            var scorer = ScorerFactory.GetScorer(prsm.Ms2Spectrum);
            prsm.Score = IonUtils.ScoreSequence(scorer, prsm.Sequence);
        }

        /// <summary>
        /// Gets a command that exports the spectra plots for the selected identifications.
        /// </summary>
        /// <returns>Task that asynchronously exports plots.</returns>
        private async Task ExportSpectraImplementation()
        {
            var folderPath = dialogService.OpenFolder();
            if (!string.IsNullOrEmpty(folderPath))
            {
                var exporter = new SpectrumPlotExporter(folderPath, null, IcParameters.Instance.ExportImageDpi);
                await exporter.ExportAsync(FilteredData);
            }
        }

        /// <summary>
        /// Gets a command that exports the spectra peaks to TSV for the selected identifications.
        /// </summary>
        /// <returns>Task that asynchronously exports plots.</returns>
        private async Task ExportPeaksImplementation()
        {
            var folderPath = dialogService.OpenFolder();
            if (!string.IsNullOrEmpty(folderPath))
            {
                var exporter = new SpectrumPeakExporter(folderPath);
                await exporter.ExportAsync(FilteredData);
            }
        }

        private void ExportProteinTreeImplementation()
        {
            var path = dialogService.SaveFile(".txt", "Text Files|*.txt");
            if (!string.IsNullOrEmpty(path))
            {
                using (var writer = new StreamWriter(path))
                {
                    foreach (var protein in IdTree.ProteinIds)
                    {
                        writer.WriteLine(protein.ProteinName);
                        foreach (var proteoform in protein.Proteoforms)
                        {
                            writer.WriteLine("\t{0}", proteoform.Value.Annotation);
                            foreach (var charge in proteoform.Value.ChargeStates)
                            {
                                writer.WriteLine("\t\t{0}+", charge.Key);
                                foreach (var prsm in charge.Value.PrSms)
                                {
                                    writer.WriteLine("\t\t\t{0} (Score: {1})", prsm.Value.Scan, prsm.Value.Score);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ExportProteinTreeAsTsvImplementation()
        {
            var path = dialogService.SaveFile(".tsv", "Tab-separated values|*.tsv");
            if (!string.IsNullOrEmpty(path))
            {
                using (var writer = new StreamWriter(path))
                {
                    writer.WriteLine("Scan\tProtein\tProteoform\tCharge\tScore");
                    foreach (var protein in IdTree.Proteins)
                    {
                        foreach (var proteoform in protein.Value.Proteoforms)
                        {
                            foreach (var charge in proteoform.Value.ChargeStates)
                            {
                                foreach (var scan in charge.Value.PrSms)
                                {
                                    writer.WriteLine("{0}\t{1}\t{2}\t{3}\t{4}",
                                                     scan.Value.Scan, protein.Key, proteoform.Value.Annotation, charge.Key, scan.Value.Score);
                                }
                            }
                        }
                    }
                }
            }
        }

    }
}
