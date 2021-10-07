using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Models;
using LcmsSpectator.Readers.SequenceReaders;
using PHRPReader;

namespace LcmsSpectator.Readers
{
    public class MsgfSynopsisReader : BaseReader
    {
        private readonly string filePath;

        public MsgfSynopsisReader(string filePath)
        {
            this.filePath = filePath;
        }

        /// <summary>
        /// Read a Peptide Hits Results Processor synopsis results file
        /// </summary>
        /// <param name="scanStart">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="scanEnd">Optional filter to apply when reading from the peptide ID file</param>
        /// <param name="modIgnoreList">Ignores modifications contained in this list.</param>
        /// <param name="progress">The progress reporter.</param>
        /// <returns>Identification tree of MS-GF+ identifications.</returns>
        protected override async Task<IEnumerable<PrSm>> ReadFile(
            int scanStart,
            int scanEnd,
            IReadOnlyCollection<string> modIgnoreList = null,
            IProgress<double> progress = null)
        {
            var startupOptions = new StartupOptions { LoadModsAndSeqInfo = true };
            var phrpReader = new ReaderFactory(filePath, startupOptions);

            if (!string.IsNullOrEmpty(phrpReader.ErrorMessage))
            {
                throw new Exception(phrpReader.ErrorMessage);
            }

            var prsmList = await Task.Run(
                               () =>
                               {
                                   var ids = new List<PrSm>();
                                   while (phrpReader.MoveNext())
                                   {
                                       phrpReader.FinalizeCurrentPSM();
                                       var psm = phrpReader.CurrentPSM;
                                       var proteins = psm.Proteins;

                                       if (scanStart > 0 && (psm.ScanNumber < scanStart || psm.ScanNumber > scanEnd))
                                       {
                                           continue;
                                       }

                                       double qValue;
                                       if (psm.AdditionalScores.TryGetValue("QValue", out var qValueText))
                                       {
                                           qValue = ParseDouble(qValueText, 4);
                                       }
                                       else
                                       {
                                           qValue = 0;
                                       }

                                       var parsedSequence = ParseSequence(psm.PeptideCleanSequence, psm.ModifiedResidues);
                                       foreach (var protein in proteins)
                                       {
                                           var prsm = new PrSm
                                           {
                                               Heavy = false,
                                               ProteinName = protein,
                                               ProteinDesc = string.Empty,
                                               Charge = psm.Charge,
                                               Sequence = parsedSequence,
                                               Scan = psm.ScanNumber,
                                               Score = Convert.ToDouble(psm.MSGFSpecEValue),
                                               UseGolfScoring = true,
                                               QValue = qValue,
                                           };

                                           prsm.SetSequence(GetSequenceText(parsedSequence), parsedSequence);

                                           ids.Add(prsm);
                                       }
                                   }

                                   return ids;
                               });

            return prsmList;
        }

        /// <summary>
        /// Parse a CLEAN sequence (containing no pre/post residues or modifications).
        /// </summary>
        /// <param name="sequenceText">The clean sequence.</param>
        /// <param name="modInfo">The modification info for the sequence.</param>
        /// <returns>The parsed sequence.</returns>
        private Sequence ParseSequence(string sequenceText, IEnumerable<AminoAcidModInfo> modInfo)
        {
            var sequenceReader = new SequenceReader();
            var sequence = sequenceReader.Read(sequenceText);
            foreach (var mod in modInfo)
            {
                if (mod.AmbiguousMod)
                    continue;

                var location = mod.ResidueLocInPeptide - 1;
                var aminoAcid = sequence[location];
                var modification = TryGetExistingModification(
                    mod.ModDefinition.MassCorrectionTag,
                    mod.ModDefinition.ModificationMass);

                if (modification == null)
                {   // could not find existing modification
                    modification = new Modification(0, mod.ModDefinition.ModificationMass, mod.ModDefinition.MassCorrectionTag);
                    Modifications.Add(modification);
                }

                sequence[location] = new ModifiedAminoAcid(aminoAcid, modification);
            }

            // Force it to recalculate mass now that the modifications have been added.
            return new Sequence(sequence);
        }

        private string GetSequenceText(Sequence sequence)
        {
            var stringBuilder = new StringBuilder();
            foreach (var aminoAcid in sequence)
            {
                stringBuilder.Append(aminoAcid.Residue);
                if (aminoAcid is ModifiedAminoAcid modifiedAminoAcid)
                {
                    stringBuilder.AppendFormat("[{0}]", modifiedAminoAcid.Modification.Name);
                }
            }

            var result = stringBuilder.ToString();

            if (result.Length == 0)
            {
                Console.WriteLine();
            }

            return result;
        }

        private Modification TryGetExistingModification(string name, double mass)
        {
            var modification = Modification.Get(name);
            if (modification == null)
            {
                var massMods = Modification.GetFromMass(mass);
                if (massMods != null && massMods.Any())
                {
                    modification = massMods[0];
                }
            }

            return modification;
        }
    }
}
