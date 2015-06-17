using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.Writers
{
    using System.IO.Packaging;
    using System.Reflection;

    using InformedProteomics.Backend.Data.Sequence;

    using LcmsSpectator.Models;

    using PSI_Interface.CV;
    using PSI_Interface.IdentData;
    using PSI_Interface.IdentData.mzIdentML;

    using Modification = PSI_Interface.IdentData.Modification;

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
            this.accessionModMap = new Dictionary<int, CV.TermInfo>();
            this.PopulateAccessionModMap();   
        }


        public void Write(IEnumerable<PrSm> ids)
        {
            var first = ids.FirstOrDefault();
            string name = string.Empty;

            if (first != null)
            {
                name = first.RawFileName;
            }
            
            var mzIdWriter = new MzIdentMLWriter(this.filePath);

            var identData = new IdentData
                                {
                                    Name = name,

                                };
        }

        /// <summary>
        /// Convert PSI_Interface modifications from InformedProteomics sequence.
        /// </summary>
        /// <param name="sequence">The InformedProteomics sequence.</param>
        /// <returns>PSI_Interface modifications list.</returns>
        private IEnumerable<Modification> GetModifications(Sequence sequence)
        {
            var modList = new List<Modification>();

            for (int i = 0; i < sequence.Count; i++)
            {
                var modAa = sequence[i] as ModifiedAminoAcid;

                if (modAa == null)
                {
                    continue;
                }

                var modification = modAa.Modification;

                var cvParams = new IdentData();

                var mod = new Modification
                {
                    MonoisotopicMassDelta = modAa.Mass,
                    Location = i,
                    ////CVParams = this.accessionModMap[modification.AccessionNum]
                };

                modList.Add(mod);
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
                    int accession;
                    if (accStr.Length < 2 || int.TryParse(accStr[1], out accession))
                    {
                        continue;
                    }

                    this.accessionModMap.Add(accession, cv.Value);
                }
            }
        }
    }
}
