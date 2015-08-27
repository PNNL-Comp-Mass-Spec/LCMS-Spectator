using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LcmsSpectator.Models;
using LcmsSpectator.Models.Dataset;

namespace LcmsSpectator.Readers
{
    public interface IProjectLoader
    {
        /// <summary>
        /// Load a project from a file into a <see cref="ProjectInfo" />.
        /// </summary>
        /// <param name="projectFilePath">The path to the project info file.</param>
        /// <returns>The loaded <see cref="ProjectInfo" />.</returns>
        ProjectInfo LoadProject(string projectFilePath);

        /// <summary>
        /// Save a <see cref="ProjectInfo" /> from a file into a file.
        /// </summary>
        /// <param name="projectInfo">The <see cref="ProjectInfo" /> to save.</param>
        void SaveProject(ProjectInfo projectInfo);

        /// <summary>
        /// Create a new project.
        /// </summary>
        /// <returns>The new <see cref="ProjectInfo" />.</returns>
        ProjectInfo CreateNewProjectInfo(string outputFile, string outputDir, List<DatasetInfo> datasets);
    }
}
