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
    using LcmsSpectator.Config;
    
    /// <summary>
    /// A class that represents a project composed of settings and multiple datasets.
    /// </summary>
    public class ProjectInfo
    {
        public ProjectInfo()
        {
            this.Name = "DefaultProject";
            this.ProjectFilePath = "DefaultProject/DefaultProject.xml";
            this.ProjectFilePath = "DefaultProject/DefaultProject_layout.xml";
            this.OutputDirectory = "DefaultProject";

            this.Datasets = new List<DatasetInfo>();
            this.ToleranceSettings = new ToleranceSettings();
            this.ImageExportSettings = new ImageExportSettings();
            this.Parameters = new MsPfParameters();
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
        /// Gets or sets the tolerance settings for the project.
        /// </summary>
        public ToleranceSettings ToleranceSettings { get; set; }

        /// <summary>
        /// Gets or sets the modification settings for the project.
        /// </summary>
        public ModificationSettings ModificationSettings { get; set; }

        /// <summary>
        /// Gets or sets the settings for the feature map.
        /// </summary>
        public FeatureMapSettings FeatureMapSettings { get; set; }

        /// <summary>
        /// Gets or sets the ion type settings for the project.
        /// </summary>
        public IonTypeSettings IonTypeSettings { get; set; }

        /// <summary>
        /// Gets or sets the image export settings for the project.
        /// </summary>
        public ImageExportSettings ImageExportSettings { get; set; }

        /// <summary>
        /// Gets or sets the parameter settings for this project.
        /// </summary>
        public MsPfParameters Parameters { get; set; }

        /// <summary>
        /// Gets or sets the list of files associated with this project (such as target lists).
        /// </summary>
        public List<FileInfo> Files { get; set; } 
    }
}
