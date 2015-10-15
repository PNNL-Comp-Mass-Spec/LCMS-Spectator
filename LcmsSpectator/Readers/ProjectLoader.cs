using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using LcmsSpectator.Models;
using LcmsSpectator.Models.Dataset;

namespace LcmsSpectator.Readers
{
    using System.Xml;

    public class ProjectLoader : IProjectLoader
    {
        /// <summary>
        /// Load a project from a file into a <see cref="ProjectInfo" />.
        /// </summary>
        /// <param name="projectFilePath">The path to the project info file.</param>
        /// <returns>The loaded <see cref="ProjectInfo" />.</returns>
        public ProjectInfo LoadProject(string projectFilePath)
        {
            ProjectInfo projectInfo;
            var projectSerializer = new DataContractSerializer(typeof(ProjectInfo));
            using (var reader = File.Open(projectFilePath, FileMode.Open))
            {
                projectInfo = (ProjectInfo) projectSerializer.ReadObject(reader);
            }

            return projectInfo;
        }

        /// <summary>
        /// Save a <see cref="ProjectInfo" /> from a file into a file.
        /// </summary>
        /// <param name="projectInfo">The <see cref="ProjectInfo" /> to save.</param>
        public void SaveProject(ProjectInfo projectInfo)
        {
            if (string.IsNullOrEmpty(projectInfo.ProjectFilePath))
            {
                return;
            }

            var xmlSettings = new XmlWriterSettings() { Indent = true, CloseOutput = true };
            var projectSerializer = new DataContractSerializer(typeof(ProjectInfo));
            using (var writer = XmlWriter.Create(File.Open(projectInfo.ProjectFilePath, FileMode.Create), xmlSettings))
            {
                projectSerializer.WriteObject(writer, projectInfo);
            }
        }

        /// <summary>
        /// Create and save a new project.
        /// </summary>
        /// <returns>The new <see cref="ProjectInfo" />.</returns>
        public ProjectInfo CreateNewProjectInfo(string outputFile, string outputDir, List<DatasetInfo> datasets)
        {
            var projectFileName = Path.GetFileNameWithoutExtension(outputFile);

            datasets.ForEach(ds => ds.LayoutFile = string.Format("{0}\\{1}_layout.xml", outputDir, ds.Name));

            var projectInfo = new ProjectInfo
            {
                Name = projectFileName,
                ProjectFilePath = outputFile,
                OutputDirectory = outputDir,
                LayoutFilePath = string.Format("{0}\\{1}_layout.xml", outputDir, projectFileName),
                Datasets = datasets
            };

            this.SaveProject(projectInfo);

            return projectInfo;
        }
    }
}
