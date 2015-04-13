// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TargetFileReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for list of targets containing target sequence and charge state.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using LcmsSpectator.Models;
    
    /// <summary>
    /// Reader for list of targets containing target sequence and charge state.
    /// </summary>
    public class TargetFileReader
    {
        /// <summary>
        /// Stream for target list file.
        /// </summary>
        private readonly Stream inputStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFileReader"/> class.
        /// </summary>
        /// <param name="targetFileName">
        /// The target file name.
        /// </param>
        public TargetFileReader(string targetFileName)
        {
            this.TargetFilePath = targetFileName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetFileReader"/> class.
        /// </summary>
        /// <param name="inputStream">
        /// The input stream.
        /// </param>
        public TargetFileReader(Stream inputStream)
        {
            this.inputStream = inputStream;
        }

        /// <summary>
        /// Gets the path to the target list file.
        /// </summary>
        public string TargetFilePath { get; private set; }
        
        /// <summary>
        /// Reads a target list file.
        /// </summary>
        /// <returns>Returns list of targets.</returns>
        public List<Target> Read()
        {
            if (string.IsNullOrEmpty(this.TargetFilePath))
            {
                return this.ReadStream();
            }

            return this.ReadFile();
        }

        /// <summary>
        /// Reads a target list from an open stream.
        /// </summary>
        /// <returns>List of targets.</returns>
        public List<Target> ReadStream()
        {
            var targetList = new List<Target>();
            var reader = new StreamReader(this.inputStream);
            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    continue;
                }

                var parts = line.Split('\t');
                if (parts[0] == "Sequence")
                {
                    continue;
                }

                var sequence = parts[0];
                var charge = 0;
                if (parts.Length > 1)
                {
                    charge = Convert.ToInt32(parts[1]);
                }

                targetList.Add(new Target(sequence, charge));
            }

            return targetList;
        }

        /// <summary>
        /// Reads list of targets from the file path.
        /// </summary>
        /// <returns>List of targets.</returns>
        public List<Target> ReadFile()
        {
            var targetList = new List<Target>();
            var file = File.ReadLines(this.TargetFilePath);
            foreach (var line in file)
            {
                var parts = line.Split('\t');
                if (parts[0] == "Sequence")
                {
                    continue;
                }

                var sequence = parts[0];
                var charge = 0;
                if (parts.Length > 1)
                {
                    charge = Convert.ToInt32(parts[1]);
                }

                targetList.Add(new Target(sequence, charge));
            }

            return targetList;
        }
    }
}
