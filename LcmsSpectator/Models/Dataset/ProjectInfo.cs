// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProjectInfo.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   A class that represents a project composed of settings and multiple datasets.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Models.Dataset
{
    using System.Collections.Generic;
    using System.IO;

    using InformedProteomics.Backend.Data.Sequence;
    
    /// <summary>
    /// A class that represents a project composed of settings and multiple datasets.
    /// </summary>
    public class ProjectInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectInfo"/> class.
        /// </summary>
        public ProjectInfo()
        {
            this.Name = "DefaultProject";
            this.ProjectFilePath = string.Empty;
            this.LayoutFilePath = string.Empty;
            this.OutputDirectory = "DefaultProject";

            this.Datasets = new List<DatasetInfo>();
            this.RegisteredModifications = new List<Modification>(Modification.CommonModifications)
            {
                Modification.LysToHeavyLys,
                Modification.ArgToHeavyArg
            };

            this.Files = new List<FileInfo>();
        }

        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path to the project file.
        /// </summary>
        public string ProjectFilePath { get; set; }

        /// <summary>
        /// Gets or sets the project layout file path.
        /// </summary>
        public string LayoutFilePath { get; set; }

        /// <summary>
        /// Gets or sets the output directory path.
        /// </summary>
        public string OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets the list of datasets that are part of this project.
        /// </summary>
        public List<DatasetInfo> Datasets { get; set; }

        /// <summary>
        /// Gets or sets the list of all registered modifications.
        /// </summary>
        public List<Modification> RegisteredModifications { get; set; } 

        /// <summary>
        /// Gets or sets the list of files associated with this project (such as target lists).
        /// </summary>
        public List<FileInfo> Files { get; set; }

        /// <summary>
        /// Adds a dataset and initializes its output directory and layout output.
        /// </summary>
        /// <param name="dataset">The dataset to add.</param>
        public void AddandInitDataset(DatasetInfo dataset)
        {
            this.Datasets.Add(dataset);
            dataset.LayoutFile = Path.Combine(this.OutputDirectory, string.Format("{0}_layout.xml", dataset.Name));
        }
    }
}
