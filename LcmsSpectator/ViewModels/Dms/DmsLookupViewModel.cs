// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DmsLookupViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A view model for searching DMS for datasets and jobs.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models;
using LcmsSpectator.Readers;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Dms
{
    /// <summary>
    /// A view model for searching DMS for datasets and jobs.
    /// </summary>
    public class DmsLookupViewModel : ReactiveObject
    {
        /// <summary>
        /// The path for the FASTA files on DMS.
        /// </summary>
        /// <remarks>Protein collection based FASTA files are in this directory; legacy FASTA files are elsewhere</remarks>
        public const string FastaFilePath = @"\\gigasax\DMS_FASTA_File_Archive\dynamic\forward";

        /// <summary>
        /// The name of the the previous results file
        /// </summary>
        private const string PreviousResultsFile = @"LCMSSpectator\dmsSearches.txt";

        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// Utility for querying DMS.
        /// </summary>
        private readonly DmsLookupUtility dmsLookupUtility;

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
        /// The selected DMS dataset.
        /// </summary>
        private DmsDatasetViewModel selectedDataset;

        /// <summary>
        /// The selected DMS job for the selected dataset.
        /// </summary>
        private DmsJobViewModel selectedJob;

        /// <summary>
        /// Search status
        /// </summary>
        private string searchStatus;

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
        /// A dictionary mapping name of dataset to number of weeks for quick lookup.
        /// </summary>
        private Dictionary<string, int> previousDatasets;

        /// <summary>
        /// Initializes a new instance of the <see cref="DmsLookupViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        public DmsLookupViewModel(IDialogService dialogService)
        {
            IsFirstSearch = true;
            Status = false;
            this.dialogService = dialogService;
            previousResultsList = new List<Tuple<string, int>>();
            NumberOfWeeks = 10;
            PreviousDatasets = new Dictionary<string, int>();
            Datasets = new ReactiveList<DmsDatasetViewModel>();
            Jobs = new ReactiveList<DmsJobViewModel>();
            dmsLookupUtility = new DmsLookupUtility();

            var lookUpCommand = ReactiveCommand.Create();
            lookUpCommand.Subscribe(_ => Lookup());
            LookupCommand = lookUpCommand;

            // If there is no data set selected or there is no job selected, disable open button
            var openCommand = ReactiveCommand.Create(this.WhenAnyValue(x => x.SelectedDataset, x => x.SelectedJob)
                                                     .Select(x => !string.IsNullOrEmpty(x.Item1?.DatasetFolderPath) && !string.IsNullOrEmpty(x.Item2?.JobFolderPath)));
            openCommand.Subscribe(_ => OpenImplementation());
            OpenCommand = openCommand;

            var closeCommand = ReactiveCommand.Create();
            closeCommand.Subscribe(_ => CloseImplementation());
            CloseCommand = closeCommand;

            SelectedDataset = new DmsDatasetViewModel();
            SelectedJob = new DmsJobViewModel();

            // When a data set is selected, find jobs for the data set
            this.WhenAnyValue(x => x.SelectedDataset)
                .Subscribe(x =>
                {
                    Jobs.Clear();
                    if (selectedDataset == null)
                    {
                        return;
                    }

                    var jobMap =
                        dmsLookupUtility.GetJobsByDataset(
                            new List<DmsLookupUtility.UdtDatasetInfo> { selectedDataset.UdtDatasetInfo });
                    if (jobMap.TryGetValue(selectedDataset.DatasetId, out var jobs))
                    {
                        foreach (var job in jobs)
                        {
                            Jobs.Add(new DmsJobViewModel(job));
                        }
                    }

                    if (Jobs.Count > 0)
                    {
                        SelectedJob = Jobs[0];
                    }
                });

            // When a null data set is selected and a search has ocurred, show no results screen
            this.WhenAnyValue(x => x.Datasets.Count, x => x.IsFirstSearch)
                .Select(x => x.Item1 == 0 && !x.Item2)
                .Subscribe(noResults => IsNoResultsShown = noResults);

            // When the dataset filter changes, find the number of weeks previously selected for the filter
            this.WhenAnyValue(x => x.DatasetFilter)
                .Where(filter => !string.IsNullOrEmpty(filter))
                .Where(filter => PreviousDatasets.ContainsKey(filter))
                .Select(filter => PreviousDatasets[filter])
                .Subscribe(numWeeks => NumberOfWeeks = numWeeks);

            OpenPreviousResultFile();
        }

        /// <summary>
        /// Event that is triggered when open or close are executed.
        /// </summary>
        public event EventHandler ReadyToClose;

        /// <summary>
        /// Gets the list of data sets found through searching DMS.
        /// </summary>
        public ReactiveList<DmsDatasetViewModel> Datasets { get; }

        /// <summary>
        /// Gets the list of jobs associated with the selected dataset.
        /// </summary>
        public ReactiveList<DmsJobViewModel> Jobs { get; }

        /// <summary>
        /// Gets a command that searches DMS for data sets.
        /// </summary>
        public IReactiveCommand LookupCommand { get; }

        /// <summary>
        /// Gets a command that opens the selected data set and job.
        /// </summary>
        public IReactiveCommand OpenCommand { get; }

        /// <summary>
        /// Gets a command that closes the search without opening a data set or job.
        /// </summary>
        public IReactiveCommand CloseCommand { get; }

        /// <summary>
        /// Gets a value indicating whether a valid data set has been selected.
        /// </summary>
        public bool Status { get; private set; }

        /// <summary>
        /// Gets or sets the selected DMS dataset.
        /// </summary>
        public DmsDatasetViewModel SelectedDataset
        {
            get => selectedDataset;
            set => this.RaiseAndSetIfChanged(ref selectedDataset, value);
        }

        /// <summary>
        /// Gets or sets the selected DMS job for the selected dataset.
        /// </summary>
        public DmsJobViewModel SelectedJob
        {
            get => selectedJob;
            set => this.RaiseAndSetIfChanged(ref selectedJob, value);
        }

        /// <summary>
        /// Search status
        /// </summary>
        public string SearchStatus
        {
            get => searchStatus;
            set => this.RaiseAndSetIfChanged(ref searchStatus, value);
        }
        /// <summary>
        /// Gets a value indicating whether or not the No Results alert should be shown.
        /// Set to true when a search has been performed that yielded 0 results.
        /// </summary>
        public bool IsNoResultsShown
        {
            get => isNoResultsShown;
            private set => this.RaiseAndSetIfChanged(ref isNoResultsShown, value);
        }

        /// <summary>
        /// Gets or sets the number of weeks in the past to search for datasets.
        /// </summary>
        public int NumberOfWeeks
        {
            get => numberOfWeeks;
            set => this.RaiseAndSetIfChanged(ref numberOfWeeks, value);
        }

        /// <summary>
        /// Gets or sets the name of the dataset to search for.
        /// </summary>
        public string DatasetFilter
        {
            get => dataSetFilter;
            set => this.RaiseAndSetIfChanged(ref dataSetFilter, value);
        }

        /// <summary>
        /// Gets a dictionary mapping name of dataset to number of weeks for quick lookup.
        /// </summary>
        public Dictionary<string, int> PreviousDatasets
        {
            get => previousDatasets;
            private set => this.RaiseAndSetIfChanged(ref previousDatasets, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether a search has been performed yet.
        /// </summary>
        private bool IsFirstSearch
        {
            get => isFirstSearch;
            set => this.RaiseAndSetIfChanged(ref isFirstSearch, value);
        }

        /// <summary>
        /// Checks to see if the data set selected is a valid data set.
        /// </summary>
        /// <returns>A value indicating whether the data set selected is valid.</returns>
        public bool ValidateDataSet()
        {
            return !string.IsNullOrEmpty(SelectedDataset?.DatasetFolderPath) && Directory.Exists(SelectedDataset.DatasetFolderPath);
        }

        /// <summary>
        /// Checks to see if the data set selected is a valid job.
        /// </summary>
        /// <returns>A value indicating whether the job selected is valid.</returns>
        public bool ValidateJob()
        {
            return !string.IsNullOrEmpty(SelectedJob?.JobFolderPath) && Directory.Exists(SelectedJob.JobFolderPath);
        }

        /// <summary>
        /// Get a list of all raw files associated with the selected data set.
        /// </summary>
        /// <returns>List containing full paths associated with the selected data set.</returns>
        public List<string> GetRawFileNames()
        {
            if (!ValidateDataSet())
            {
                return new List<string>();
            }

            var dataSetDirFiles = Directory.GetFiles(SelectedDataset.DatasetFolderPath);
            var rawFileNames = (from filePath in dataSetDirFiles
                            let ext = Path.GetExtension(filePath)
                            where !string.IsNullOrEmpty(ext)
                            let extL = ext.ToLower()
                            where (extL == ".raw" || extL == ".mzml" || extL == ".gz")
                            select filePath).ToList();
            for (var i = 0; i < rawFileNames.Count; i++)
            {
                var pbfFile = GetPbfFileName(rawFileNames[i]);
                if (!string.IsNullOrEmpty(pbfFile))
                {
                    rawFileNames[i] = pbfFile;
                }
            }

            return rawFileNames;
        }

        /// <summary>
        /// Get the ID file associated with the selected job.
        /// </summary>
        /// <returns>Full path of the ID file associated with the selected job.</returns>
        public string GetIdFileName()
        {
            if (!ValidateJob())
            {
                return null;
            }

            var jobDir = Directory.GetFiles(SelectedJob.JobFolderPath);
            return (from idFp in jobDir
                           let ext = Path.GetExtension(idFp).ToLower()
                           where ext == ".mzid" || ext == ".gz" || ext == ".zip"
                           select idFp).FirstOrDefault();
        }

        /// <summary>
        /// Get the feature file associated with the selected job.
        /// </summary>
        /// <returns>Full path of the feature file associated with the selected job.</returns>
        public string GetFeatureFileName()
        {
            if (!ValidateJob())
            {
                return null;
            }

            // Find promex folder
            var promexDir = Directory.GetDirectories(SelectedDataset.DatasetFolderPath).FirstOrDefault(d => d.Contains("ProMex"));
            if (string.IsNullOrEmpty(promexDir))
            {
                return null;
            }

            var promexDirFiles = Directory.GetFiles(promexDir);

            return (from idFp in promexDirFiles
                    let ext = Path.GetExtension(idFp).ToLower()
                    where ext == ".ms1ft"
                    select idFp).FirstOrDefault();
        }

        /// <summary>
        /// Get the tool type for the selected job
        /// </summary>
        /// <returns>The tool type used for the selected job.</returns>
        public ToolType? GetTool()
        {
            if (!ValidateJob())
            {
                return null;
            }

            ToolType toolType;
            switch (SelectedJob.Tool)
            {
                case "MS-GF+":
                    toolType = ToolType.MsgfPlus;
                    break;
                case "MSPathFinder":
                    toolType = ToolType.MsPathFinder;
                    break;
                default:
                    toolType = ToolType.Other;
                    break;
            }

            return toolType;
        }

        /// <summary>
        /// Lookup datasets and jobs for the past NumberOfWeeks with a filter given by DatasetFilter
        /// </summary>
        private void Lookup()
        {
            Datasets.Clear();
            SearchStatus = string.Empty;

            if (NumberOfWeeks < 1)
            {
                dialogService.MessageBox("Number of weeks must be greater than 0.");
                return;
            }

            if (string.IsNullOrWhiteSpace(DatasetFilter))
            {
                dialogService.MessageBox("Please enter a dataset filter.");
                return;
            }

            IsFirstSearch = false;

            var dataSets = dmsLookupUtility.GetDatasets(NumberOfWeeks, DatasetFilter.Trim());
            foreach (var dataset in dataSets)
            {
                Datasets.Add(new DmsDatasetViewModel(dataset.Value));
            }

            SearchStatus = string.Format("Found {0} dataset{1}", dataSets.Count, dataSets.Count == 1 ? "" : "s");

            if (Datasets.Count > 0)
            {
                SelectedDataset = Datasets[0];
            }
        }

        /// <summary>
        /// Open the file containing the list of previous data sets searched for on DMS.
        /// </summary>
        private void OpenPreviousResultFile()
        {
            var prevResults = new Dictionary<string, int>();
            var previousResultFilePath = GetOrCreatePreviousSearchPath();
            if (File.Exists(previousResultFilePath))
            {
                var file = File.ReadAllLines(previousResultFilePath);
                foreach (var line in file)
                {
                    var parts = line.Split('\t');
                    if (parts.Length < 2)
                    {
                        continue;
                    }

                    var dataSetName = parts[0];
                    if (!int.TryParse(parts[1], out var numWeeks))
                    {
                        continue;
                    }

                    previousResultsList.Add(new Tuple<string, int>(dataSetName, numWeeks));
                    if (!prevResults.ContainsKey(dataSetName))
                    {
                        prevResults.Add(dataSetName, numWeeks);
                    }
                }
            }

            PreviousDatasets = prevResults;
        }

        /// <summary>
        /// Write the file containing the list of previous data sets searched for on DMS.
        /// </summary>
        private void WritePreviousResultFile()
        {
            var previousResults = previousResultsList;
            if (previousResultsList.Count > 30)
            {
                previousResults = previousResultsList.GetRange(0, Math.Min(30, previousResultsList.Count - 1));
            }

            using (var outFile = new StreamWriter(GetOrCreatePreviousSearchPath()))
            {
                foreach (var item in previousResults)
                {
                    outFile.WriteLine("{0}\t{1}", item.Item1, item.Item2);
                }
            }
        }

        /// <summary>
        /// Get the PBF file (if it exists) for a certain raw file associated with this data set.
        /// </summary>
        /// <param name="rawFilePath">The path of the raw file to find associated PBF files.</param>
        /// <returns>The full path to the PBF file.</returns>
        private string GetPbfFileName(string rawFilePath)
        {
            string pbfFilePath = null;
            if (!ValidateDataSet())
            {
                return null;
            }

            var dataSetDirDirectories = Directory.GetDirectories(SelectedDataset.DatasetFolderPath);
            var pbfFolderPath = (from folderPath in dataSetDirDirectories
                                 let folderName = Path.GetFileNameWithoutExtension(folderPath)
                                 where folderName.StartsWith("PBF_Gen")
                                 select folderPath).FirstOrDefault();
            if (!string.IsNullOrEmpty(pbfFolderPath))
            {
                var pbfIndirectionPath = string.Format(@"{0}\{1}.pbf_CacheInfo.txt", pbfFolderPath, Path.GetFileNameWithoutExtension(rawFilePath));
                if (!string.IsNullOrEmpty(pbfIndirectionPath) && File.Exists(pbfIndirectionPath))
                {
                    var lines = File.ReadAllLines(pbfIndirectionPath);
                    if (lines.Length > 0)
                    {
                        pbfFilePath = lines[0];
                    }
                }
            }

            return pbfFilePath;
        }

        /// <summary>
        /// Implementation for OpenCommand.
        /// Sets status to true, writes the previous result file, and triggers the ReadyToClose event.
        /// </summary>
        private void OpenImplementation()
        {
            Status = true;
            previousResultsList.Insert(0, new Tuple<string, int>(DatasetFilter.Trim(), NumberOfWeeks));
            WritePreviousResultFile();
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Implementation for CloseCommand.
        /// Sets status to false and triggers the ReadyToClose event.
        /// </summary>
        private void CloseImplementation()
        {
            Status = false;
            ReadyToClose?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Will return the path to the previous search result file, and will
        /// create the directory in the user's AppData path if it does not already exist.
        /// </summary>
        /// <returns>The path to the previous search result file.</returns>
        private string GetOrCreatePreviousSearchPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var filePath = Path.Combine(appDataPath, PreviousResultsFile);
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return filePath;
        }
    }
}
