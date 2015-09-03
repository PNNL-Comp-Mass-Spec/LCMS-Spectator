// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DmsLookupViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model for searching DMS for datasets and jobs.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassFeature;
using LcmsSpectator.Models.Dataset;
using Splat;

namespace LcmsSpectator.ViewModels.Dms
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;

    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models;
    using LcmsSpectator.Readers;

    using ReactiveUI;

    /// <summary>
    /// A view model for searching DMS for datasets and jobs.
    /// </summary>
    public class DmsLookupViewModel : ReactiveObject, IDatasetInfoProvider
    {
        /// <summary>
        /// The path for the FASTA files on DMS.
        /// </summary>
        public const string FastaFilePath = @"\\gigasax\DMS_FASTA_File_Archive\static\forward";

        /// <summary>
        /// The name of the the previous results file
        /// </summary>
        private const string PreviousResultsFile = "dmsSearches.txt";

        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// Utility for querying DMS.
        /// </summary>
        private readonly DmsLookupUtility dmsLookupUtility;

        /// <summary>
        /// Cache mapping dataset IDs to jobs.
        /// </summary>
        private readonly MemoizingMRUCache<DmsLookupUtility.UdtDatasetInfo, List<DmsLookupUtility.UdtJobInfo>> jobCache;

        /// <summary>
        /// List containing previous searches performed and the number of weeks searched.
        /// </summary>
        private readonly List<Tuple<string, int>> previousResultsList;

        /// <summary>
        /// A value indicating whether or not the No Results alert should be shown.
        /// Set to true when a search has been performed that yielded 0 results.
        /// </summary>
        private bool isNoResultsShown;

        /// <summary>
        /// A value indicating whether a search has been performed yet.
        /// </summary>
        private bool isFirstSearch;

        /// <summary>
        /// The number of weeks in the past to search for datasets.
        /// </summary>
        private int numberOfWeeks;

        /// <summary>
        /// The name of the dataset to search for.
        /// </summary>
        private string dataSetFilter;

        /// <summary>
        /// The path to the output directory.
        /// </summary>
        private string outputDirectory;

        /// <summary>
        /// The name of the file being copied.
        /// </summary>
        private string copyStatusText;

        /// <summary>
        /// The progress of the file copy.
        /// </summary>
        private double progress;

        /// <summary>
        /// A value indicating whether files are currently being copied.
        /// </summary>
        private bool isCopying;

        /// <summary>
        /// A value indicating whether files should be
        /// copied to the output directory upon opening.
        /// </summary>
        private bool shouldCopyFiles;

        /// <summary>
        /// Save the dataset info.
        /// </summary>
        private List<DatasetInfo> datasets; 

        /// <summary>
        /// A dictionary mapping name of dataset to number of weeks for quick lookup.
        /// </summary>
        private Dictionary<string, int> previousDatasets;

        /// <summary>
        /// A cancellation token for cancelling the file copy.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="DmsLookupViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public DmsLookupViewModel(IDialogService dialogService)
        {
            this.IsFirstSearch = true;
            this.Status = false;
            this.dialogService = dialogService;
            this.previousResultsList = new List<Tuple<string, int>>();
            this.NumberOfWeeks = 10;
            this.PreviousDatasets = new Dictionary<string, int>();
            this.Datasets = new ReactiveList<DmsDatasetViewModel> { ChangeTrackingEnabled = true };
            this.Jobs = new ReactiveList<DmsJobViewModel> { ChangeTrackingEnabled = true };
            this.dmsLookupUtility = new DmsLookupUtility();
            this.jobCache = new MemoizingMRUCache<DmsLookupUtility.UdtDatasetInfo, List<DmsLookupUtility.UdtJobInfo>>(
                (dataset, o) =>
                {
                    var jobMap = this.dmsLookupUtility.GetJobsByDataset(
                                        new List<DmsLookupUtility.UdtDatasetInfo> { dataset });
                    List<DmsLookupUtility.UdtJobInfo> jobs;
                    jobMap.TryGetValue(dataset.DatasetId, out jobs);
                    return jobs;
                },
            20);

            var lookUpCommand = ReactiveCommand.Create();
            lookUpCommand.Subscribe(_ => this.Lookup());
            this.LookupCommand = lookUpCommand;

            // If there is no data set selected or there is no job selected, disable open button
            this.OpenCommand = ReactiveCommand.CreateAsyncTask(
                                              this.Datasets.ItemChanged.Select(_ => this.Datasets.Any(ds => ds.Selected)),
                                              async _ => await this.OpenImplementation());

            this.CloseCommand = ReactiveCommand.Create();
            this.CloseCommand.Subscribe(_ => this.CloseImplementation());;

            this.BrowseOutputDirectoriesCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.ShouldCopyFiles));
            this.BrowseOutputDirectoriesCommand.Subscribe(_ => this.BrowseOutputDirectoriesImplementation());

            // Add jobs when dataset is selected.
            this.Datasets.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Where(x => x.Sender.Selected)
                .SelectMany(async x => await Task.Run(() => this.jobCache.Get(x.Sender.UdtDatasetInfo)))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(jobs => this.Jobs.AddRange(jobs.Select(j => new DmsJobViewModel(j))));

            // Remove jobs when dataset is deselected
            this.Datasets.ItemChanged.Where(x => x.PropertyName == "Selected")
                .Where(x => !x.Sender.Selected)
                .Select(x => this.Jobs.Where(j => j.DatasetId == x.Sender.DatasetId))
                .Subscribe(jobs => this.Jobs.RemoveAll(jobs));

            // Clear cached dataset into when selected datasets/jobs change
            this.Datasets.ItemChanged.Subscribe(_ => this.datasets = null);
            this.Jobs.ItemChanged.Subscribe(_ => this.datasets = null);

            // When a null data set is selected and a search has ocurred, show no results screen
            this.WhenAnyValue(x => x.Datasets.Count, x => x.IsFirstSearch)
                .Select(x => x.Item1 == 0 && !x.Item2)
                .Subscribe(noResults => this.IsNoResultsShown = noResults);

            // When the dataset filter changes, find the number of weeks previously selected for the filter
            this.WhenAnyValue(x => x.DatasetFilter)
                .Where(filter => !string.IsNullOrEmpty(filter))
                .Where(filter => this.PreviousDatasets.ContainsKey(filter))
                .Select(filter => this.PreviousDatasets[filter])
                .Subscribe(numWeeks => this.NumberOfWeeks = numWeeks);

            this.OpenPreviousResultFile();
        }

        /// <summary>
        /// Event that is triggered when open or close are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets the list of data sets found through searching DMS.
        /// </summary>
        public ReactiveList<DmsDatasetViewModel> Datasets { get; private set; }

        /// <summary>
        /// Gets the list of jobs associated with the selected dataset.
        /// </summary>
        public ReactiveList<DmsJobViewModel> Jobs { get; private set; }

        /// <summary>
        /// Gets a command that searches DMS for data sets.
        /// </summary>
        public IReactiveCommand LookupCommand { get; private set; }

        /// <summary>
        /// Gets a command that opens the selected data set and job.
        /// </summary>
        public ReactiveCommand<Unit> OpenCommand { get; private set; }

        /// <summary>
        /// Gets a command that closes the search without opening a data set or job.
        /// </summary>
        public ReactiveCommand<object> CloseCommand { get; private set; }

        /// <summary>
        /// Gets a command that opens a file browser for selecting the output directory.
        /// </summary>
        public ReactiveCommand<object> BrowseOutputDirectoriesCommand { get; private set; }

        /// <summary>
        /// Gets a value indicating whether a valid data set has been selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets a value indicating whether or not the No Results alert should be shown.
        /// Set to true when a search has been performed that yielded 0 results.
        /// </summary>
        public bool IsNoResultsShown
        {
            get { return this.isNoResultsShown; }
            private set { this.RaiseAndSetIfChanged(ref this.isNoResultsShown, value); }
        }

        /// <summary>
        /// Gets or sets the number of weeks in the past to search for datasets.
        /// </summary>
        public int NumberOfWeeks
        {
            get { return this.numberOfWeeks; }
            set { this.RaiseAndSetIfChanged(ref this.numberOfWeeks, value); }
        }

        /// <summary>
        /// Gets or sets the name of the dataset to search for.
        /// </summary>
        public string DatasetFilter
        {
            get { return this.dataSetFilter; }
            set { this.RaiseAndSetIfChanged(ref this.dataSetFilter, value); }
        }

        /// <summary>
        /// Gets a dictionary mapping name of dataset to number of weeks for quick lookup.
        /// </summary>
        public Dictionary<string, int> PreviousDatasets
        {
            get { return this.previousDatasets; }
            private set { this.RaiseAndSetIfChanged(ref this.previousDatasets, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a search has been performed yet.
        /// </summary>
        private bool IsFirstSearch
        {
            get { return this.isFirstSearch; }
            set { this.RaiseAndSetIfChanged(ref this.isFirstSearch, value); }
        }

        /// <summary>
        /// Gets or sets the path to the output directory.
        /// </summary>
        public string OutputDirectory
        {
            get { return this.outputDirectory; }
            set { this.RaiseAndSetIfChanged(ref this.outputDirectory, value); }
        }

        /// <summary>
        /// Gets the name of the file being copied.
        /// </summary>
        public string CopyStatusText
        {
            get { return this.copyStatusText; }
            private set { this.RaiseAndSetIfChanged(ref this.copyStatusText, value); }
        }

        /// <summary>
        /// Gets or sets the progress of the file copy.
        /// </summary>
        public double Progress
        {
            get { return this.progress; }
            private set { this.RaiseAndSetIfChanged(ref this.progress, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether files are currently being copied.
        /// </summary>
        public bool IsCopying
        {
            get { return this.isCopying; }
            private set { this.RaiseAndSetIfChanged(ref this.isCopying, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether files should be
        /// copied to the output directory upon opening.
        /// </summary>
        public bool ShouldCopyFiles
        {
            get { return this.shouldCopyFiles; }
            set { this.RaiseAndSetIfChanged(ref this.shouldCopyFiles, value); }
        }

        /// <summary>
        /// Gets the dataset info for the selected dataset.
        /// </summary>
        /// <returns>The <see cref="DatasetInfo" />.</returns>
        public List<DatasetInfo> GetDatasetInfo()
        {
            if (this.datasets != null)
            {
                return this.datasets;
            }

            var selectedDatasets = this.Datasets.Where(ds => ds.Selected).ToList();
            var selectedJobs = this.Jobs.Where(job => job.Selected).ToList();

            if (!selectedDatasets.Any())
            {
                return null;
            }

            var datasets = new List<DatasetInfo> { Capacity = selectedDatasets.Count };
            foreach (var dataset in selectedDatasets)
            {
                var fileSet = new List<string>();
                fileSet.AddRange(dataset.GetRawFileNames());
                var jobs = selectedJobs.Where(j => j.DatasetId == dataset.DatasetId);
                foreach (var job in jobs)
                {
                    fileSet.Add(job.GetFeatureFileName());
                    fileSet.Add(job.GetIdFileName());
                }

                datasets.Add(new DatasetInfo(fileSet));
            }

            this.datasets = datasets;
            return datasets;
        }

        /// <summary>
        /// Lookup datasets and jobs for the past NumberOfWeeks with a filter given by DatasetFilter
        /// </summary>
        private void Lookup()
        {
            this.Datasets.Clear();
            if (this.NumberOfWeeks < 1)
            {
                this.dialogService.MessageBox("Number of weeks must be greater than 0.");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.DatasetFilter))
            {
                this.dialogService.MessageBox("Please enter a dataset filter.");
                return;
            }

            this.IsFirstSearch = false;

            var dataSets = this.dmsLookupUtility.GetDatasets(this.NumberOfWeeks, this.DatasetFilter.Trim());
            foreach (var dataset in dataSets)
            {
                this.Datasets.Add(new DmsDatasetViewModel(dataset.Value));
            }

            if (this.Datasets.Count > 0)
            {
                this.Datasets[0].Selected = true;
            }
        }

        /// <summary>
        /// Open the file containing the list of previous data sets searched for on DMS.
        /// </summary>
        private void OpenPreviousResultFile()
        {
            var prevResults = new Dictionary<string, int>();
            if (File.Exists(PreviousResultsFile))
            {
                var file = File.ReadAllLines(PreviousResultsFile);
                foreach (var line in file)
                {
                    int numWeeks;
                    var parts = line.Split('\t');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    var dataSetName = parts[0];
                    if (!int.TryParse(parts[1], out numWeeks))
                    {
                        continue;
                    }

                    this.previousResultsList.Add(new Tuple<string, int>(dataSetName, numWeeks));
                    if (!prevResults.ContainsKey(dataSetName))
                    {
                        prevResults.Add(dataSetName, numWeeks);
                    }
                }
            }

            this.PreviousDatasets = prevResults;
        }

        /// <summary>
        /// Write the file containing the list of previous data sets searched for on DMS.
        /// </summary>
        private void WritePreviousResultFile()
        {
            var previousResults = this.previousResultsList;
            if (this.previousResultsList.Count > 30)
            {
                previousResults = this.previousResultsList.GetRange(0, Math.Min(30, this.previousResultsList.Count - 1));
            }

            using (var outFile = new StreamWriter(PreviousResultsFile))
            {
                foreach (var item in previousResults)
                {
                    outFile.WriteLine("{0}\t{1}", item.Item1, item.Item2);
                }
            }
        }

        /// <summary>
        /// Copy files from DMS dataset folder to output directory.
        /// </summary>
        /// <returns>Task for awaiting file copy.</returns>
        private async Task CopyFilesAsync()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            await Task.Run(
                () =>
                {
                    if (!Directory.Exists(this.OutputDirectory))
                    {
                        return;
                    }

                    this.IsCopying = true;
                    this.Progress = 0;

                    var datasets = this.GetDatasetInfo();
                    foreach (var dataset in datasets)
                    {
                        for (int i = 0; i < dataset.Files.Count; i++)
                        {
                            if (this.cancellationTokenSource.IsCancellationRequested)
                            {
                                break;
                            }

                            var fileName = Path.GetFileName(dataset.Files[i].FilePath);
                            this.CopyStatusText = string.Format("Copying {0}", fileName);
                            var newPath = string.Format("{0}\\{1}", this.OutputDirectory, fileName);
                            File.Copy(dataset.Files[i].FilePath, newPath, true);
                            dataset.Files[i].FilePath = newPath;

                            this.Progress = ((i + 1) / (double)dataset.Files.Count) * 100;
                        }   
                    }

                    this.CopyStatusText = string.Empty;
                    this.IsCopying = false;
                    this.Progress = 100;
                },
                this.cancellationTokenSource.Token);
        }

        /// <summary>
        /// Implementation for <see cref="OpenCommand" />.
        /// Sets status to true, writes the previous result file, and triggers the ReadyToClose event.
        /// </summary>
        private async Task OpenImplementation()
        {
            this.Status = true;
            this.previousResultsList.Insert(0, new Tuple<string, int>(this.DatasetFilter.Trim(), this.NumberOfWeeks));
            this.WritePreviousResultFile();
            if (this.ShouldCopyFiles && !string.IsNullOrWhiteSpace(this.OutputDirectory))
            {
                await this.CopyFilesAsync();
            }

            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation for <see cref="CloseCommand" />.
        /// Sets status to false and triggers the ReadyToClose event.
        /// </summary>
        private void CloseImplementation()
        {
            this.Status = false;
            if (this.ReadyToClose != null)
            {
                this.ReadyToClose(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Implementation for <see cref="BrowseOutputDirectoriesCommand" />.
        /// Gets a command that opens a file browser for selecting the output directory.
        /// </summary>
        private void BrowseOutputDirectoriesImplementation()
        {
            var folder = this.dialogService.OpenFolder("Select output directory.");
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                this.OutputDirectory = folder;
            }
        }
    }
}
