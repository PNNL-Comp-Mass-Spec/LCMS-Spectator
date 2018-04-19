﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LcmsSpectator.Writers
{

    using InformedProteomics.Backend.Data.Sequence;

    using Models;

    using PSI_Interface.CV;
    using PSI_Interface.IdentData;

    [Obsolete("This class is not functional")]
    public class MzIdWriter : IIdWriter
    {
        /// <summary>
        /// File path to write to.
        /// </summary>
        private string filePath;

        /// <summary>
        /// Maps modification numbers to accessions.
        /// </summary>
        private Dictionary<int, CV.TermInfo> accessionModMap;

        public MzIdWriter(string filePath)
        {
            accessionModMap = new Dictionary<int, CV.TermInfo>();
            PopulateAccessionModMap();
        }


        public void Write(IEnumerable<PrSm> ids)
        {
            var first = ids.FirstOrDefault();
            var name = string.Empty;

            if (first != null)
            {
                name = first.RawFileName;
            }

        }

        /// <summary>
        /// Convert PSI_Interface modifications from InformedProteomics sequence.
        /// </summary>
        /// <param name="sequence">The InformedProteomics sequence.</param>
        /// <returns>PSI_Interface modifications list.</returns>
        private IEnumerable<Modification> GetModifications(Sequence sequence)
        {
            var modList = new List<Modification>();

            foreach (var residue in sequence)
            {
                if (!(residue is ModifiedAminoAcid modAa))
                {
                    continue;
                }

                var modification = modAa.Modification;

                var cvParams = new IdentDataObj();
            }

            return modList;
        }

        /// <summary>
        /// Convert CV TermData modifications to map int (accession #) -> TermInfo.
        /// </summary>
        private void PopulateAccessionModMap()
        {
            foreach (var cv in CV.TermData)
            {
                if (cv.Value.CVRef == @"UNIMOD")
                {
                    var accStr = cv.Value.Id.Split(':');
                    if (accStr.Length < 2 || int.TryParse(accStr[1], out var accession))
                    {
                        continue;
                    }

                    accessionModMap.Add(accession, cv.Value);
                }
            }
        }
    }
}
