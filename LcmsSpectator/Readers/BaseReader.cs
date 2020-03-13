using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Models;

namespace LcmsSpectator.Readers
{
    public abstract class BaseReader : IIdFileReader
    {
        /// <summary>
        /// List of modifications that potentially need to be registered after reading.
        /// </summary>
        public IList<Modification> Modifications { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected BaseReader()
        {
            Modifications = new List<Modification>();
        }

        protected double ParseDouble(string valueText, int digitsAfterDecimalToRound)
        {
            if (double.TryParse(valueText, out var value))
            {
                return Math.Round(value, digitsAfterDecimalToRound);
            }

            return 0;
        }

        /// <summary>
        /// Read a peptide identification  results file.
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns><returns>The Protein-Spectrum-Match identifications.</returns></returns>
        public IEnumerable<PrSm> Read(int scanStart, int scanEnd, IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            var validatedIgnoreList = ValidateFilters(ref scanStart, ref scanEnd, modIgnoreList);
            return ReadFile(scanStart, scanEnd, validatedIgnoreList).Result;
        }

        /// <summary>
        /// Read a peptide identification results file asynchronously.
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Identification tree of MSPathFinder identifications.</returns>
        public async Task<IEnumerable<PrSm>> ReadAsync(int scanStart, int scanEnd, IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            var validatedIgnoreList = ValidateFilters(ref scanStart, ref scanEnd, modIgnoreList);
            return await ReadFile(scanStart, scanEnd, validatedIgnoreList);
        }

        /// <summary>
        /// Read a results file
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>The Protein-Spectrum-Match identifications.</returns>
        protected abstract Task<IEnumerable<PrSm>> ReadFile(
            int scanStart,
            int scanEnd,
            IReadOnlyCollection<string> modIgnoreList = null,
            IProgress<double> progress = null);

        /// <summary>
        /// Append data in idData to prsmList
        /// </summary>
        /// <param name="prsmList"></param>
        /// <param name="idData"></param>
        /// <param name="scanStart"></param>
        /// <param name="scanEnd"></param>
        protected void StoreIDs(List<PrSm> prsmList, IList<PrSm> idData, int scanStart, int scanEnd)
        {
            if (idData == null || !idData.Any())
                return;

            var scanNumber = idData.First().Scan;
            if (scanStart > 0 && (scanNumber < scanStart || scanNumber > scanEnd))
                return;

            prsmList.AddRange(idData);
        }

        private IReadOnlyCollection<string> ValidateFilters(ref int scanStart, ref int scanEnd, IEnumerable<string> modIgnoreList)
        {
            if (scanStart > 0 && scanEnd < scanStart)
            {
                scanEnd = scanStart;
            }

            if (modIgnoreList == null)
            {
                return new List<string>();
            }

            return modIgnoreList.ToList();
        }

    }
}
