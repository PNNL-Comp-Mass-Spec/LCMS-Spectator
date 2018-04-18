// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DmsLookupUtility.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
// Class for searching for data sets and jobs on the PNNL DMS system.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Data;

namespace LcmsSpectator.Readers
{
    using System;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Class for searching for data sets and jobs on the PNNL DMS system.
    /// </summary>
    public class DmsLookupUtility
    {
        /// <summary>
        /// Connection string for DMS database.
        /// </summary>
        public const string DmsConnectionString = "Data Source=gigasax;Initial Catalog=DMS5;Integrated Security=SSPI;";

        /// <summary>
        /// Filter for MS-GF+ jobs.
        /// </summary>
        protected const string MsgfplusToolFilter = "MSGFPlus%";

        /// <summary>
        /// Filter for MSPathFinder jobs.
        /// </summary>
        protected const string MsPathFinderToolFilter = "MSPathFinder%";

        /// <summary>
        /// Maximum number of tries to query database before timing out.
        /// </summary>
        protected const int MaxRetries = 3;

        /// <summary>
        /// User provided connection string for DMS database.
        /// </summary>
        private readonly string connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="DmsLookupUtility"/> class.
        /// </summary>
        public DmsLookupUtility() : this(DmsConnectionString)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DmsLookupUtility"/> class.
        /// </summary>
        /// <param name="connectionString">The connection String.</param>
        public DmsLookupUtility(string connectionString)
        {
            this.connectionString = connectionString;
        }

        /// <summary>
        /// Tests the retrieval of datasets and jobs for datasets containing WSM419
        /// </summary>
        public void GetDemoDatasets()
        {
            var lstDatasets = GetDatasets(5, "WSM419");

            var lstMsgfPlusJobs = GetJobsByDataset(lstDatasets.Values.ToList());

            // Alternatively, could use:
            //// var lstMSGFPlusJobs = GetJobsByDataset(5, "WSM419%");

            foreach (var dataset in lstDatasets)
            {

                if (lstMsgfPlusJobs.TryGetValue(dataset.Key, out var lstJobsForDataset))
                {
                    foreach (var job in lstJobsForDataset)
                    {
                        Console.WriteLine(@"DatasetID " + dataset.Value.DatasetId + @": " + dataset.Value.Dataset + @", Job " + job.Job);
                    }
                }
                else
                {
                    Console.WriteLine(@"DatasetID " + dataset.Value.DatasetId + @": " + dataset.Value.Dataset + @", No MSGF+ jobs");
                }
            }
        }

        /// <summary>
        /// Looks up datasets in DMS based on mostRecentWeeks
        /// </summary>
        /// <param name="mostRecentWeeks">Find datasets created within the last X weeks</param>
        /// <returns>Dictionary where key is Dataset ID and value is dataset info</returns>
        public Dictionary<int, UdtDatasetInfo> GetDatasets(int mostRecentWeeks)
        {
            var datasetNameFilter = string.Empty;
            return GetDatasets(mostRecentWeeks, datasetNameFilter);
        }

        /// <summary>
        /// Looks up datasets in DMS based on the filters
        /// </summary>
        /// <param name="mostRecentWeeks">Find datasets created within the last X weeks</param>
        /// <param name="datasetNameFilter">Optional dataset name filter; use percent sign or asterisk for wildcard symbol</param>
        /// <returns>Dictionary where key is Dataset ID and value is dataset info</returns>
        public Dictionary<int, UdtDatasetInfo> GetDatasets(int mostRecentWeeks, string datasetNameFilter)
        {
            var dctDatasets = new Dictionary<int, UdtDatasetInfo>();

            var dateThreshold = DateTime.Now.AddDays(-7 * mostRecentWeeks);

            try
            {
                var sql = " SELECT Dataset_ID, Dataset, Experiment, Organism, Instrument, Created, Folder" +
                                " FROM V_Mage_Dataset_List " +
                                " WHERE Instrument <> 'DMS_Pipeline_Data'";

                sql += " AND Created >= '" + dateThreshold.ToString("yyyy-MM-dd") + "' ";

                if (!string.IsNullOrWhiteSpace(datasetNameFilter))
                {
                    sql += " AND Dataset Like '" + CheckFilter(datasetNameFilter) + "'";
                }

                var retryCount = MaxRetries;
                if (retryCount < 1)
                {
                    retryCount = 1;
                }

                while (retryCount > 0)
                {
                    try
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            var cmd = new SqlCommand(sql, connection);
                            var reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                // Note: using GetDBString to convert null strings to empty strings
                                // Numeric values could possibly be null, but shouldn't be in this query
                                var datasetInfo = new UdtDatasetInfo
                                {
                                    DatasetId = reader.GetInt32(0),
                                    Dataset = GetDBString(reader, 1),
                                    Experiment = GetDBString(reader, 2),
                                    Organism = GetDBString(reader, 3),
                                    Instrument = GetDBString(reader, 4),
                                    Created = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
                                    DatasetFolderPath = GetDBString(reader, 6)
                                };

                                // Purged datasets that were archived prior to ~August 2013 will have DatasetFolderPath of the form
                                // \\agate.emsl.pnl.gov\dmsarch\LTQ_Orb_2\2014_1\LNA_CF_Replete_vitro_B1_INH_peri_25Feb14_Leopard_14-02-01

                                // Newer purged datasetes will have DatasetFolderPath shown as
                                // \\MyEMSL\IMS04_AgTOF05\2014_3\TB_UR_38_14Jul14_Methow_13-10-14

                                // Retrieving datasets from MyEMSL is doable using MyEMSLReader.dll but is not yet supported
                                dctDatasets.Add(datasetInfo.DatasetId, datasetInfo);
                            }
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount -= 1;
                        var msg = "Exception querying database in GetDatasets: " + ex.Message;
                        msg += ", RetryCount = " + retryCount;

                        Console.WriteLine(msg);

                        // Delay for 3 second before trying again
                        System.Threading.Thread.Sleep(3000);
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Exception connecting to database in GetDatasets: " + ex.Message + "; ConnectionString: " + connectionString;
                Console.WriteLine(msg);
            }

            return dctDatasets;
        }

        /// <summary>
        /// Looks up MSGF+ jobs in DMS for the datasets matched by the filters
        /// </summary>
        /// <param name="mostRecentWeeks">Find jobs for datasets created within the last X weeks</param>
        /// <returns>Dictionary where key is Dataset ID and value is a list of jobs for the dataset</returns>
        public Dictionary<int, List<UdtJobInfo>> GetJobsByDataset(int mostRecentWeeks)
        {
            var datasetNameFilter = string.Empty;
            return GetJobsByDataset(mostRecentWeeks, datasetNameFilter);
        }

        /// <summary>
        /// Looks up MSGF+ jobs in DMS for the datasets matched by the filters
        /// </summary>
        /// <param name="mostRecentWeeks">Find jobs for datasets created within the last X weeks</param>
        /// <param name="datasetNameFilter">Optional: dataset names to match; use % for a wildcard</param>
        /// <returns>Dictionary where key is Dataset ID and value is a list of jobs for the dataset</returns>
        /// <remarks>If datasetNameFilter does not have a % sign, then will auto-add % signs at the beginning and end to result in partial name matches</remarks>
        public Dictionary<int, List<UdtJobInfo>> GetJobsByDataset(int mostRecentWeeks, string datasetNameFilter)
        {
            var msgfPlusJobs = GetJobsByDataset(mostRecentWeeks, datasetNameFilter, MsgfplusToolFilter);
            var mspfJobs = GetJobsByDataset(mostRecentWeeks, datasetNameFilter, MsPathFinderToolFilter);
            var allJobs = msgfPlusJobs.Concat(mspfJobs).ToDictionary(x => x.Key, x => x.Value);
            return allJobs;
        }

        /// <summary>
        /// Looks up jobs in DMS for the datasets matched by the filters
        /// </summary>
        /// <param name="mostRecentWeeks">Find datasets created within the last X weeks</param>
        /// <param name="datasetNameFilter">Optional: dataset names to match; use % for a wildcard</param>
        /// <param name="toolNameFilter">Optional: tool name to match; use % for a wildcard</param>
        /// <returns>Dictionary where key is Dataset ID and value is a list of jobs for the dataset</returns>
        /// <remarks>If datasetNameFilter or toolNameFilter do not have a % sign, then will auto-add % signs at the beginning and end to result in partial name matches</remarks>
        public Dictionary<int, List<UdtJobInfo>> GetJobsByDataset(int mostRecentWeeks, string datasetNameFilter, string toolNameFilter)
        {
            var datasets = GetDatasets(mostRecentWeeks, datasetNameFilter);

            // Find the MSGF+/MSPathFinder jobs for the datasets
            var msgfPlusJobs = GetJobsByDataset(datasets.Values.ToList(), toolNameFilter);

            return msgfPlusJobs;
        }

        /// <summary>
        /// Looks up MSGF+ jobs in DMS for the datasets in the list
        /// </summary>
        /// <param name="datasets">List of data sets to get jobs for</param>
        /// <returns>Dictionary where key is Dataset ID and value is a list of jobs for the dataset</returns>
        public Dictionary<int, List<UdtJobInfo>> GetJobsByDataset(IEnumerable<UdtDatasetInfo> datasets)
        {
            var udtDatasetInfos = datasets as UdtDatasetInfo[] ?? datasets.ToArray();
            var msgfPlusJobs = GetJobsByDataset(udtDatasetInfos, MsgfplusToolFilter);
            var mspfPathFinderJobs = GetJobsByDataset(udtDatasetInfos, MsPathFinderToolFilter);
            var allJobs = msgfPlusJobs.Concat(mspfPathFinderJobs).ToDictionary(x => x.Key, x => x.Value);
            return allJobs;
        }

        /// <summary>
        /// Looks up jobs in DMS for the datasets in the list
        /// </summary>
        /// <param name="datasets">The data set to find jobs for.</param>
        /// <param name="toolNameFilter">Optional: tool name to match; use % for a wildcard</param>
        /// <returns>Dictionary where key is Dataset ID and value is a list of jobs for the dataset</returns>
        public Dictionary<int, List<UdtJobInfo>> GetJobsByDataset(IEnumerable<UdtDatasetInfo> datasets, string toolNameFilter)
        {
            var dctJobs = new Dictionary<int, List<UdtJobInfo>>();

            try
            {
                // Construct a comma-separated list of Dataset IDs
                var datasetIdList = new StringBuilder();
                foreach (var dataset in datasets)
                {
                    if (datasetIdList.Length > 0)
                    {
                        datasetIdList.Append(",");
                    }

                    datasetIdList.Append(dataset.DatasetId);
                }

                var sql = " SELECT Job, Dataset_ID, Tool, Job_Finish, Folder," +
                                    " Parameter_File, Settings_File," +
                                    " [Protein Collection List], [Organism DB]" +
                                " FROM V_Mage_Analysis_Jobs" +
                                " WHERE Dataset_ID IN (" + datasetIdList + ")";

                if (!string.IsNullOrWhiteSpace(toolNameFilter))
                {
                    sql += " AND Tool Like '" + CheckFilter(toolNameFilter) + "'";
                }

                var retryCount = MaxRetries;
                if (retryCount < 1)
                {
                    retryCount = 1;
                }

                while (retryCount > 0)
                {
                    try
                    {
                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.Open();

                            var cmd = new SqlCommand(sql, connection);
                            var reader = cmd.ExecuteReader();

                            while (reader.Read())
                            {
                                // "Job, Dataset_ID, Tool, Job_Finish, Folder," +
                                // " Parameter_File, Settings_File," +
                                // " [Organism DB], [Protein Collection List]" +
                                var jobInfo = new UdtJobInfo
                                {
                                    Job = reader.GetInt32(0),
                                    DatasetId = reader.GetInt32(1),
                                    Tool = GetDBString(reader, 2),
                                    Completed = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3),
                                    JobFolderPath = GetDBString(reader, 4),
                                    ParameterFile = GetDBString(reader, 5),
                                    SettingsFile = GetDBString(reader, 6),
                                    ProteinCollection = GetDBString(reader, 7),
                                    OrganismDb = GetDBString(reader, 8)
                                };


                                if (dctJobs.TryGetValue(jobInfo.DatasetId, out var jobsForDataset))
                                {
                                    jobsForDataset.Add(jobInfo);
                                }
                                else
                                {
                                    jobsForDataset = new List<UdtJobInfo> { jobInfo };
                                    dctJobs.Add(jobInfo.DatasetId, jobsForDataset);
                                }
                            }
                        }

                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount -= 1;
                        var msg = "Exception querying database in GetJobs: " + ex.Message;
                        msg += ", RetryCount = " + retryCount;

                        Console.WriteLine(msg);

                        // Delay for 3 second before trying again
                        System.Threading.Thread.Sleep(3000);
                    }
                }
            }
            catch (Exception ex)
            {
                var msg = "Exception connecting to database in GetJobs: " + ex.Message + "; ConnectionString: " + connectionString;
                Console.WriteLine(msg);
            }

            return dctJobs;
        }

        /// <summary>
        /// Examines the filter to look for a percent sign
        /// </summary>
        /// <param name="filterSpec">Filter to inspect</param>
        /// <returns>Original filter if it has a percent sign, otherwise %filterSpec%</returns>
        private string CheckFilter(string filterSpec)
        {
            if (string.IsNullOrWhiteSpace(filterSpec))
            {
                return "%";
            }

            if (filterSpec.Contains("*"))
            {
                // Replace * wildcards with SQL-style % wildcards
                filterSpec = filterSpec.Replace("*", "%");
            }

            if (!filterSpec.Contains("%"))
            {
                return "%" + filterSpec + "%";
            }

            return filterSpec;
        }

        /// <summary>The get db string.</summary>
        /// <param name="reader">The database reader.</param>
        /// <param name="columnIndex">The column index.</param>
        /// <returns>The <see cref="string"/>.</returns>
        private string GetDBString(IDataRecord reader, int columnIndex)
        {
            if (!Convert.IsDBNull(reader.GetValue(columnIndex)))
            {
                return reader.GetString(columnIndex);
            }

            return string.Empty;
        }

        /// <summary>
        /// Information about a data set on the PNNL DMS system.
        /// </summary>
        public struct UdtDatasetInfo
        {
            /// <summary>
            /// The id of the DMS data set.
            /// </summary>
            public int DatasetId;

            /// <summary>
            /// The name of the DMS data set.
            /// </summary>
            public string Dataset;

            /// <summary>
            /// The name of the type of experiment.
            /// </summary>
            public string Experiment;

            /// <summary>
            /// The name of the organism.
            /// </summary>
            public string Organism;

            /// <summary>
            /// The type of instrument used.
            /// </summary>
            public string Instrument;

            /// <summary>
            /// The date and time that this data set was created.
            /// </summary>
            public DateTime Created;

            /// <summary>
            /// The path for the folder that this data set is in.
            /// </summary>
            public string DatasetFolderPath;
        }

        /// <summary>
        /// Information about a job on the PNNL DMS system.
        /// </summary>
        public struct UdtJobInfo
        {
            /// <summary>
            /// The ID of the DMS job.
            /// </summary>
            public int Job;

            /// <summary>
            /// The ID of the data set that this job is associated with.
            /// </summary>
            public int DatasetId;

            /// <summary>
            /// The name of the tool used for this job.
            /// </summary>
            public string Tool;

            /// <summary>
            /// The date and time that this job was completed.
            /// </summary>
            public DateTime Completed;

            /// <summary>
            /// The path for the folder that this job is in.
            /// </summary>
            public string JobFolderPath;

            /// <summary>
            /// The path for the parameter file for this job.
            /// </summary>
            public string ParameterFile;

            /// <summary>
            /// The path for the settings file for this job.
            /// </summary>
            public string SettingsFile;

            /// <summary>
            /// The protein collection for this job.
            /// </summary>
            public string ProteinCollection;

            /// <summary>
            /// The name of the organism database used for this job.
            /// </summary>
            public string OrganismDb;
        }
    }
}
