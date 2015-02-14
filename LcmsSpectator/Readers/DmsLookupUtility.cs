using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace LcmsSpectator.Readers
{
	public class DmsLookupUtility
	{
		#region "Constants"
		
		public const string DmsConnectionString = "Data Source=gigasax;Initial Catalog=DMS5;Integrated Security=SSPI;";

		protected const string MsgfplusToolFilter = "MSGFPlus%";
        protected const string MsPathFinderToolFilter = "MSPathFinder%";
	    protected const string ProMexToolFilter = "ProMex%";
		protected const int MaxRetries = 3;

		#endregion

		#region "Structures"

		public struct UdtDatasetInfo
		{
			public int DatasetId;
			public string Dataset;
			public string Experiment;
			public string Organism;
			public string Instrument;
			public DateTime Created;
			public string DatasetFolderPath;
		}

		public struct UdtJobInfo
		{
			public int Job;
			public int DatasetId;
			public string Tool;
			public DateTime Completed;
			public string JobFolderPath;
			public string ParameterFile;
			public string SettingsFile;
			public string ProteinCollection;
			public string OrganismDb;
		}

		#endregion

		private readonly string _mConnectionString;

		/// <summary>
		/// Constructor
		/// </summary>
		public DmsLookupUtility() : this(DmsConnectionString)
		{
			
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="connectionString"></param>
		public DmsLookupUtility(string connectionString)
		{
			_mConnectionString = connectionString;
		}

		/// <summary>
		/// Tests the retrieval of datasets and jobs for datasets containing WSM419
		/// </summary>
		public void GetDemoDatasets()
		{
			var lstDatasets = GetDatasets(5, "WSM419");

			var lstMsgfPlusJobs = GetJobsByDataset(lstDatasets.Values.ToList());

			// Alternatively, could use:
			// var lstMSGFPlusJobs = GetJobsByDataset(5, "WSM419%");

			foreach (var dataset in lstDatasets)
			{
				List<UdtJobInfo> lstJobsForDataset;

				if (lstMsgfPlusJobs.TryGetValue(dataset.Key, out lstJobsForDataset))
				{
					foreach (var job in lstJobsForDataset)
					{
						Console.WriteLine("DatasetID " + dataset.Value.DatasetId + ": " + dataset.Value.Dataset + ", Job " + job.Job);
					}
				}
				else
				{
					Console.WriteLine("DatasetID " + dataset.Value.DatasetId + ": " + dataset.Value.Dataset + ", No MSGF+ jobs");
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
			string datasetNameFilter = string.Empty;
			return GetDatasets(mostRecentWeeks, datasetNameFilter);
		}

		/// <summary>
		/// Looks up datasets in DMS based on the filters
		/// </summary>
		/// <param name="mostRecentWeeks">Find datasets created within the last X weeks</param>
		/// <param name="datasetNameFilter">Optional dataset name filter; use percent sign for wildcard symbol</param>
		/// <returns>Dictionary where key is Dataset ID and value is dataset info</returns>
		public Dictionary<int, UdtDatasetInfo> GetDatasets(int mostRecentWeeks, string datasetNameFilter)
		{
			var dctDatasets = new Dictionary<int, UdtDatasetInfo>();

			var dateThreshold = DateTime.Now.AddDays(-7 * mostRecentWeeks);

			try
			{
				string sql = " SELECT Dataset_ID, Dataset, Experiment, Organism, Instrument, Created, Folder" +
							 " FROM V_Mage_Dataset_List " +
							 " WHERE Instrument <> 'DMS_Pipeline_Data'";

				sql += " AND Created >= '" + dateThreshold.ToString("yyyy-MM-dd") + "' ";

				if (!string.IsNullOrWhiteSpace(datasetNameFilter))
					sql += " AND Dataset Like '" + CheckFilter(datasetNameFilter) + "'";

				int retryCount = MaxRetries;
				if (retryCount < 1)
					retryCount = 1;

				while (retryCount > 0)
				{
					try
					{
						using (var cnDb = new SqlConnection(_mConnectionString))
						{
							cnDb.Open();

							var cmd = new SqlCommand(sql, cnDb);
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
									Created = reader.GetDateTime(5),
									DatasetFolderPath = GetDBString(reader, 6)
								};

								// Purged datasets that were archived prior to ~August 2013 will have DatasetFolderPath of the form
								// \\a2.emsl.pnl.gov\dmsarch\LTQ_Orb_2\2014_1\LNA_CF_Replete_vitro_B1_INH_peri_25Feb14_Leopard_14-02-01
								//
								// Newer purged datasetes will have DatasetFolderPath shown as
								// \\MyEMSL\IMS04_AgTOF05\2014_3\TB_UR_38_14Jul14_Methow_13-10-14
								//
								// Retrieving datasets from MyEMSL is doable using MyEMSLReader.dll but is probably something you don't want to worry about at this time								

								dctDatasets.Add(datasetInfo.DatasetId, datasetInfo);

							}
						}
						break;
					}
					catch (Exception ex)
					{
						retryCount -= 1;
						string msg = "Exception querying database in GetDatasets: " + ex.Message;
						msg += ", RetryCount = " + retryCount;

						Console.WriteLine(msg);

						//Delay for 3 second before trying again
						System.Threading.Thread.Sleep(3000);
					}
				}
			}
			catch (Exception ex)
			{
				string msg = "Exception connecting to database in GetDatasets: " + ex.Message + "; ConnectionString: " + _mConnectionString;
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
			string datasetNameFilter = string.Empty;
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
		    var msPathFinderJobs = GetJobsByDataset(mostRecentWeeks, datasetNameFilter, MsPathFinderToolFilter);
            var proMexJobs = GetJobsByDataset(mostRecentWeeks, datasetNameFilter, ProMexToolFilter);
		    var allJobs = msgfPlusJobs.Concat(msPathFinderJobs).Concat(proMexJobs).ToDictionary(x=> x.Key, x=> x.Value);
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

			var lstDatasets = GetDatasets(mostRecentWeeks, datasetNameFilter);

			// Find the MSGF+/MSPathFinder jobs for the datasets
			var lstMsgfPlusJobs = GetJobsByDataset(lstDatasets.Values.ToList(), toolNameFilter);

			return lstMsgfPlusJobs;
		}

		/// <summary>
		/// Looks up MSGF+ jobs in DMS for the datasets in the list
		/// </summary>
		/// <param name="lstDatasets"></param>
		/// <returns>Dictionary where key is Dataset ID and value is a list of jobs for the dataset</returns>
		public Dictionary<int, List<UdtJobInfo>> GetJobsByDataset(IEnumerable<UdtDatasetInfo> lstDatasets)
		{
            var msgfPlusJobs = GetJobsByDataset(lstDatasets, MsgfplusToolFilter);
            var msPathFinderJobs = GetJobsByDataset(lstDatasets, MsPathFinderToolFilter);
            var proMexJobs = GetJobsByDataset(lstDatasets, ProMexToolFilter);
            var allJobs = msgfPlusJobs.Concat(msPathFinderJobs).Concat(proMexJobs).ToDictionary(x => x.Key, x => x.Value);
            return allJobs;
		}

		/// <summary>
		/// Looks up jobs in DMS for the datasets in the list
		/// </summary>
		/// <param name="lstDatasets"></param>
		/// <param name="toolNameFilter">Optional: tool name to match; use % for a wildcard</param>
		/// <returns>Dictionary where key is Dataset ID and value is a list of jobs for the dataset</returns>
		public Dictionary<int, List<UdtJobInfo>> GetJobsByDataset(IEnumerable<UdtDatasetInfo> lstDatasets, string toolNameFilter)
		{

			var dctJobs = new Dictionary<int, List<UdtJobInfo>>();

			try
			{
				// Construct a comma-separated list of Dataset IDs
				var datasetIdList = new StringBuilder();
				foreach (var dataset in lstDatasets)
				{
					if (datasetIdList.Length > 0)
						datasetIdList.Append(",");

					datasetIdList.Append(dataset.DatasetId);
				}

				string sql = " SELECT Job, Dataset_ID, Tool, Job_Finish, Folder," +
									" Parameter_File, Settings_File," +
									" [Protein Collection List], [Organism DB]" +
							 " FROM V_Mage_Analysis_Jobs" +
							 " WHERE Dataset_ID IN (" + datasetIdList + ")";

				if (!string.IsNullOrWhiteSpace(toolNameFilter))
					sql += " AND Tool Like '" + CheckFilter(toolNameFilter) + "'";

				int retryCount = MaxRetries;
				if (retryCount < 1)
					retryCount = 1;

				while (retryCount > 0)
				{
					try
					{
						using (var cnDb = new SqlConnection(_mConnectionString))
						{
							cnDb.Open();

							var cmd = new SqlCommand(sql, cnDb);
							var reader = cmd.ExecuteReader();

							while (reader.Read())
							{

								//"Job, Dataset_ID, Tool, Job_Finish, Folder," +
								// " Parameter_File, Settings_File," +
								//" [Organism DB], [Protein Collection List]" +

								var jobInfo = new UdtJobInfo
								{
									Job = reader.GetInt32(0),
									DatasetId = reader.GetInt32(1),
									Tool = GetDBString(reader, 2),
									Completed = reader.GetDateTime(3),
									JobFolderPath = GetDBString(reader, 4),
									ParameterFile = GetDBString(reader, 5),
									SettingsFile = GetDBString(reader, 6),
									ProteinCollection = GetDBString(reader, 7),
									OrganismDb = GetDBString(reader, 8)
								};

								List<UdtJobInfo> jobsForDataset;

								if (dctJobs.TryGetValue(jobInfo.DatasetId, out jobsForDataset))
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
						string msg = "Exception querying database in GetJobs: " + ex.Message;
						msg += ", RetryCount = " + retryCount;

						Console.WriteLine(msg);

						//Delay for 3 second before trying again
						System.Threading.Thread.Sleep(3000);
					}
				}
			}
			catch (Exception ex)
			{
				string msg = "Exception connecting to database in GetJobs: " + ex.Message + "; ConnectionString: " + _mConnectionString;
				Console.WriteLine(msg);
			}

			return dctJobs;

		}

		/// <summary>
		/// Examines the filter to look for a percent sign
		/// </summary>
		/// <param name="filterSpec"></param>
		/// <returns>Original filter if it has a percent sign, otherwise %filterSpec%</returns>
		private string CheckFilter(string filterSpec)
		{
			if (string.IsNullOrWhiteSpace(filterSpec))
				return "%";

			if (!filterSpec.Contains("%"))
				return "%" + filterSpec + "%";

			return filterSpec;
		}

		private string GetDBString(SqlDataReader reader, int columnIndex)
		{
			if (!Convert.IsDBNull(reader.GetValue(columnIndex)))
			{
				return reader.GetString(columnIndex);
			}

			return string.Empty;
		}

	}
}
