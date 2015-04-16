// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScanViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>>
// <summary>
//   This class maintains a filterable list of Protein-Spectrum-Match identifications.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Threading.Tasks;
    using InformedProteomics.Backend.Data.Spectrometry;
    using InformedProteomics.Backend.MassSpecData;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using ReactiveUI;
    
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
        private IEnumerable<ProteinId> filteredProteins;

        /// <summary>
        /// The selected Protein-Spectrum-Match identification.
        /// </summary>
        private PrSm selectedPrSm;

        /// <summary>
        /// Gets or sets the object selected in TreeView. Uses weak typing because each level TreeView is a different data type.
        /// </summary>
        private object treeViewSelectedItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScanViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from the view model.</param>
        /// <param name="ids">The Protein-Spectrum-Match identifications to display.</param>
        public ScanViewModel(IMainDialogService dialogService, IEnumerable<PrSm> ids)
        {
            var clearFiltersCommand = ReactiveCommand.Create();
            clearFiltersCommand.Subscribe(_ => this.ClearFilters());
            this.ClearFiltersCommand = clearFiltersCommand;
            this.dialogService = dialogService;

            this.FilteredData = new PrSm[0];

            this.Filters = new ReactiveList<FilterViewModel> { ChangeTrackingEnabled = true };

            this.Data = new ReactiveList<PrSm> { ChangeTrackingEnabled = true };
            this.InitializeDefaultFilters();
            this.Data.AddRange(ids);

            // When a filter is selected/uselected, request a filter value if selected, then filter data
            this.Filters.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Select(x => x.Sender)
                .Where(sender => !sender.Selected || sender.Name == "Hide Unidentified Scans" || this.dialogService.FilterBox(sender))
                .SelectMany(async _ => await this.FilterDataAsync(this.Data))
                .Subscribe(fd => this.FilteredData = fd);

            // Data changes when items are added or removed
            var dataChanged = this.Data.BeforeItemsAdded.Merge(this.Data.BeforeItemsRemoved)
                                  .Where(x => x != null)
                                  .Throttle(TimeSpan.FromMilliseconds(500), RxApp.TaskpoolScheduler)
                                  .SelectMany(async _ => await this.FilterDataAsync(this.Data));

            // When data changes, update filtered data
            dataChanged.Subscribe(fd => this.FilteredData = fd);

            // When data is filtered, group it by protein name
            this.WhenAnyValue(x => x.FilteredData)
                .Select(fd => new IdentificationTree(fd))
                .Subscribe(idTree => this.FilteredProteins = idTree.ProteinIds);

            // When data is filtered and a PRSM has not been selected yet, select the first PRSM
            this.WhenAnyValue(x => x.FilteredData)
                .Where(fd => fd.Length > 0)
                .Where(_ => this.SelectedPrSm == null)
                .Subscribe(fd => this.SelectedPrSm = fd[0]);

            // When a tree object is selected and it is a PRSM, set the selected PRSM
            this.WhenAnyValue(x => x.TreeViewSelectedItem)
                .Select(x => x as PrSm)
                .Where(p => p != null)
                .Subscribe(p => this.SelectedPrSm = p);

            // When a tree object is selected and it is an IIdData, set the selected PRSM
            this.WhenAnyValue(x => x.TreeViewSelectedItem)
                .Select(x => x as IIdData)
                .Where(p => p != null)
                .Subscribe(p => this.SelectedPrSm = p.GetHighestScoringPrSm());
        }

        /// <summary>
        /// Gets a command that clears all the filters.
        /// </summary>
        public IReactiveCommand ClearFiltersCommand { get; private set; }

        /// <summary>
        /// Gets the list of possible filters.
        /// </summary>
        public ReactiveList<FilterViewModel> Filters { get; private set; } 

        /// <summary>
        /// Gets all unfiltered data.
        /// </summary>
        public ReactiveList<PrSm> Data
        {
            get { return this.data; }
            private set { this.RaiseAndSetIfChanged(ref this.data, value); }
        }

        /// <summary>
        /// Gets the filtered data.
        /// </summary>
        public PrSm[] FilteredData
        {
            get { return this.filteredData; }
            private set { this.RaiseAndSetIfChanged(ref this.filteredData, value); }
        }

        /// <summary>
        /// Gets a list of IDs grouped by protein name.
        /// </summary>
        public IEnumerable<ProteinId> FilteredProteins
        {
            get { return this.filteredProteins; }
            private set { this.RaiseAndSetIfChanged(ref this.filteredProteins, value); }
        }

        /// <summary>
        /// Gets or sets the selected Protein-Spectrum-Match identification.
        /// </summary>
        public PrSm SelectedPrSm
        {
            get { return this.selectedPrSm; }
            set { this.RaiseAndSetIfChanged(ref this.selectedPrSm, value); }
        }

        /// <summary>
        /// Gets or sets the object selected in TreeView. Uses weak typing because each level TreeView is a different data type.
        /// </summary>
        public object TreeViewSelectedItem
        {
            get { return this.treeViewSelectedItem; }
            set { this.RaiseAndSetIfChanged(ref this.treeViewSelectedItem, value); }
        }

        /// <summary>
        /// Clear all filters and filter the data.
        /// </summary>
        public void ClearFilters()
        {
            foreach (var filter in this.Filters)
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
            var newData = this.Data.Where(prsm => prsm.RawFileName != rawFileName).ToList();
            if (!newData.Contains(this.SelectedPrSm))
            {
                this.SelectedPrSm = null;
            }

            this.Data.Clear();
            this.Data.AddRange(newData);
        }

        /// <summary>
        /// Shows or hides instrument Precursor m/z and mass from the instrument
        /// Reads data from LCMSRun if necessary
        /// </summary>
        /// <param name="value">Should the instrument data be shown?</param>
        /// <param name="pbfLcmsRun">LCMSRun for this data set.</param>
        /// <returns>Asynchronous task.</returns>
        public async Task ToggleShowInstrumentDataAsync(bool value, PbfLcMsRun pbfLcmsRun)
        {
            if (value)
            {
                if (pbfLcmsRun != null)
                {
                    var scans = this.Data;
                    foreach (var scan in scans.Where(scan => scan.Sequence.Count == 0))
                    {
                        PrSm scan1 = scan;
                        IsolationWindow isolationWindow = await Task.Run(() => pbfLcmsRun.GetIsolationWindow(scan1.Scan));
                        scan.PrecursorMz = isolationWindow.MonoisotopicMz ?? double.NaN;
                        scan.Charge = isolationWindow.Charge ?? 0;
                        scan.Mass = isolationWindow.MonoisotopicMass ?? double.NaN;
                    }
                }
            }
            else
            {
                var scans = this.Data;
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
            this.Data.Clear();
        }

        /// <summary>
        /// Filter data by filter values asynchronously.
        /// </summary>
        /// <param name="da">The data to filtered.</param>
        /// <returns>The filtered data.</returns>
        private Task<PrSm[]> FilterDataAsync(IEnumerable<PrSm> da)
        {
            return Task.Run(() => this.FilterData(da));
        }

        /// <summary>
        /// Filter data by filter values.
        /// </summary>
        /// <param name="da">The data to filtered.</param>
        /// <returns>The filtered data.</returns>
        private PrSm[] FilterData(IEnumerable<PrSm> da)
        {
            IEnumerable<object> filtered = new List<PrSm>(da);
            var selectedFilters = this.Filters.Where(f => f.Selected);
            filtered = selectedFilters.Aggregate(filtered, (current, filter) => filter.Filter(current));
            var filteredPrSms = filtered.Cast<PrSm>();

            var allPrSmsByScan = new Dictionary<int, PrSm>();

            // ensure that all scan numbers for the data set are unique.
            foreach (var prsm in filteredPrSms)
            {
                if (!allPrSmsByScan.ContainsKey(prsm.Scan))
                {
                    allPrSmsByScan.Add(prsm.Scan, prsm);
                }
                else if (allPrSmsByScan[prsm.Scan].Sequence.Count == 0)
                {
                    allPrSmsByScan[prsm.Scan] = prsm;
                }
            }

            var uniqueFilteredPrSms = allPrSmsByScan.Values.ToArray();
            Array.Sort(uniqueFilteredPrSms, new PrSm.PrSmScoreComparer());
            return uniqueFilteredPrSms;
        }

        /// <summary>
        /// Initialize possible default filters.
        /// </summary>
        private void InitializeDefaultFilters()
        {
            // Filter by subsequence
            this.Filters.Add(new FilterViewModel(
                    "Sequence", 
                    "Filter by Sequence", 
                    "Enter Sequence to filter by:", 
                    (d, v) => d.Where(datum => ((PrSm)datum).SequenceText.Contains(v)), 
                    o => true,
                    this.dialogService,
                    (from prsm in this.Data where prsm.SequenceText.Length > 0 select prsm.SequenceText).Distinct()));

            // Filter by protein name
            this.Filters.Add(new FilterViewModel(
                    "Protein Name", 
                    "Filter by Protein Name", 
                    "Enter protein name to filter by:", 
                    (d, v) => d.Where(datum => ((PrSm)datum).ProteinName.Contains(v)), 
                    o => true, 
                    this.dialogService,
                    (from prsm in this.Data where prsm.ProteinName.Length > 0 select prsm.ProteinName).Distinct()));

            // Filter by mass
            this.Filters.Add(new FilterViewModel(
                    "Mass", 
                    "Filter by Mass", 
                    "Enter minimum Mass to display:", 
                    (d, v) => d.Where(datum => ((PrSm)datum).Mass >= Convert.ToDouble(v)), 
                    o =>
                        {
                            double conv;
                            var str = o as string;
                            return double.TryParse(str, out conv);
                        }, 
                    this.dialogService));

            // Filter by most abundant isotope m/z
            this.Filters.Add(new FilterViewModel(
                    "Most Abundant Isotope m/z", 
                    "Filter by Most Abundant Isotope M/Z", 
                    "Enter minimum M/Z to display:", 
                    (d, v) => d.Where(datum => ((PrSm)datum).PrecursorMz >= Convert.ToDouble(v)), 
                    o =>
                        {
                            double conv;
                            var str = o as string;
                            return double.TryParse(str, out conv);
                        }, 
                    this.dialogService));

            // Filter by charge state
            this.Filters.Add(new FilterViewModel(
                    "Charge", 
                    "Filter by Charge", 
                    "Enter Charge to filter by:", 
                    (d, v) => d.Where(datum => ((PrSm)datum).Charge == Convert.ToInt32(v)), 
                    o =>
                        {
                            int conv;
                            var str = o as string;
                            return int.TryParse(str, out conv);
                        }, 
                    this.dialogService,
                    (from prsm in this.Data where prsm.Charge > 0 select prsm.Charge.ToString(CultureInfo.InvariantCulture)).Distinct()));

            // Filter by score
            this.Filters.Add(new FilterViewModel(
                    "Score", 
                    "Filter by Score", 
                    "Enter minimum score to display:", 
                    (d, v) =>
                       {
                            double score = Convert.ToDouble(v);
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
                            double conv;
                            var str = o as string;
                            return double.TryParse(str, out conv);
                        }, 
                    this.dialogService));

            // Filter by QValue
            this.Filters.Add(new FilterViewModel(
                    "QValue", 
                    "Filter by QValue", 
                    "Enter minimum QValue to display:", 
                    (d, v) => d.Where(datum => ((PrSm)datum).QValue <= Convert.ToDouble(v)), 
                    o =>
                        {
                            double conv;
                            var str = o as string;
                            return double.TryParse(str, out conv);
                        }, 
                    this.dialogService, 
                    null, 
                    "0.01") { Selected = true });

            // Remove unidentified scans
            this.Filters.Add(new FilterViewModel(
                    "Hide Unidentified Scans", 
                    string.Empty, 
                    string.Empty, 
                    (d, v) => d.Where(datum => ((PrSm)datum).Sequence.Count > 0), 
                    o => true, 
                    this.dialogService));

            // Filter by raw file name
            this.Filters.Add(new FilterViewModel(
                    "Raw File Name", 
                    "Filter by data set name", 
                    "Enter data set to filter by:", 
                    (d, v) => d.Where(datum => ((PrSm)datum).RawFileName.Contains(v)), 
                    o => true, 
                    this.dialogService,
                    (from prsm in this.Data where prsm.RawFileName.Length > 0 select prsm.RawFileName).Distinct()));
        }
    }
}
