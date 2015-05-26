// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MzIdentMlReader.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Reader for MZID files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Readers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using LcmsSpectator.Models;
    using LcmsSpectator.Readers.SequenceReaders;
    using MTDBFramework.Algorithms;
    using MTDBFramework.Data;
    
    /// <summary>
    /// Reader for MZID files.
    /// </summary>
    public class MzIdentMlReader : IIdFileReader
    {
        /// <summary>
        /// Path for MZID file.
        /// </summary>
        private readonly string filePath;

        /// <summary>
        /// MTDB Creator MZID reader.
        /// </summary>
        private readonly MTDBFramework.IO.MzIdentMlReader mzIdentMlReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="MzIdentMlReader"/> class.
        /// </summary>
        /// <param name="filePath">The path for the MZID file.</param>
        public MzIdentMlReader(string filePath)
        {
            this.filePath = filePath;
            var options = new Options
            {
                MsgfQValue = 1.0,
                MaxMsgfSpecProb = 1.0,
                TargetFilterType = TargetWorkflowType.BOTTOM_UP
            };
            this.mzIdentMlReader = new MTDBFramework.IO.MzIdentMlReader(options);
        }

        /// <summary>
        /// Read a MZID results file.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>Identification tree of identifications.</returns>
        public IEnumerable<PrSm> Read(IEnumerable<string> modIgnoreList = null)
        {
            return this.ReadAsync(modIgnoreList).Result;
        }

        /// <summary>
        /// Read a MZID results file asynchronously.
        /// </summary>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <returns>Identification tree of MZID identifications.</returns>
        public async Task<IEnumerable<PrSm>> ReadAsync(IEnumerable<string> modIgnoreList)
        {
            var dataSet = await Task.Run(() => this.mzIdentMlReader.Read(this.filePath));
            var prsms = new List<PrSm>();

            var evidences = dataSet.Evidences;

            var sequenceReader = new SequenceReader();

            foreach (var evidence in evidences)
            {
                var msgfPlusEvidence = evidence as MsgfPlusResult;
                var sequenceText = evidence.SeqWithNumericMods;
                var index = sequenceText.IndexOf('.');
                var lastIndex = sequenceText.LastIndexOf('.');
                if (index != lastIndex && index >= 0 && lastIndex >= 0 && sequenceText.Length > 1)
                {
                    sequenceText = sequenceText.Substring(
                        index + 1,
                        sequenceText.Length - (sequenceText.Length - lastIndex) - (index + 1));
                }

                double qvalue = -1;
                double score = double.NaN;

                if (msgfPlusEvidence != null)
                {
                    score = msgfPlusEvidence.SpecEValue;
                    qvalue = msgfPlusEvidence.QValue;
                }

                foreach (var protein in evidence.Proteins)
                {
                    var prsm = new PrSm(sequenceReader)
                    {
                        Heavy = false,
                        Scan = evidence.Scan,
                        Charge = evidence.Charge,
                        ////Sequence = sequenceReader.Read(sequenceText),
                        SequenceText = sequenceText,
                        ProteinName = protein.ProteinName,
                        Score = score,
                        UseGolfScoring = true,
                        QValue = qvalue,
                    };
                    prsms.Add(prsm);
                }
            }

            return prsms;
        }
    }
}
