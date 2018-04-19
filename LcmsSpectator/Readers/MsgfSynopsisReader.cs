using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LcmsSpectator.Readers
{
    using InformedProteomics.Backend.Data.Sequence;

    using Models;
    using SequenceReaders;

    using PHRPReader;

    public class MsgfSynopsisReader : IIdFileReader
    {
        private readonly string filePath;

        public MsgfSynopsisReader(string filePath)
        {
            this.filePath = filePath;
            Modifications = new List<Modification>();
        }

        /// <summary>
        /// Gets a list of modifications that potentially need to be registered after reading.
        /// </summary>
        public IList<Modification> Modifications { get; }

        public IEnumerable<PrSm> Read(IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            var oStartupOptions = new clsPHRPStartupOptions { LoadModsAndSeqInfo = true };
            var phrpReader = new clsPHRPReader(filePath, oStartupOptions);

            if (!string.IsNullOrEmpty(phrpReader.ErrorMessage))
            {
                throw new Exception(phrpReader.ErrorMessage);
            }

            var identifications = new List<PrSm>();
            while (phrpReader.MoveNext())
            {
                phrpReader.FinalizeCurrentPSM();
                var psm = phrpReader.CurrentPSM;
                var proteins = psm.Proteins;

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
                        Score = Convert.ToDouble(psm.MSGFSpecProb),
                        UseGolfScoring = true,
                        QValue = 0,
                    };

                    prsm.SetSequence(GetSequenceText(parsedSequence), parsedSequence);

                    identifications.Add(prsm);
                }
            }

            return identifications;
        }

        public async Task<IEnumerable<PrSm>> ReadAsync(IEnumerable<string> modIgnoreList = null, IProgress<double> progress = null)
        {
            var oStartupOptions = new clsPHRPStartupOptions { LoadModsAndSeqInfo = true };
            var phrpReader = new clsPHRPReader(filePath, oStartupOptions);

            if (!string.IsNullOrEmpty(phrpReader.ErrorMessage))
            {
                throw new Exception(phrpReader.ErrorMessage);
            }

            var identifications = await Task.Run(
                () =>
                    {
                        var ids = new List<PrSm>();
                        while (phrpReader.MoveNext())
                        {
                            phrpReader.FinalizeCurrentPSM();
                            var psm = phrpReader.CurrentPSM;
                            var proteins = psm.Proteins;

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
                                                   Score = Convert.ToDouble(psm.MSGFSpecProb),
                                                   UseGolfScoring = true,
                                                   QValue = 0,
                                               };

                                prsm.SetSequence(GetSequenceText(parsedSequence), parsedSequence);

                                ids.Add(prsm);
                            }
                        }

                        return ids;
                    });

            return identifications;
        }

        /// <summary>
        /// Parse a CLEAN sequence (containing no pre/post residues or modifications).
        /// </summary>
        /// <param name="sequenceText">The clean sequence.</param>
        /// <param name="modInfo">The modification info for the sequence.</param>
        /// <returns>The parsed sequence.</returns>
        private Sequence ParseSequence(string sequenceText, List<clsAminoAcidModInfo> modInfo)
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
            sequence = new Sequence(sequence);

            return sequence;
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
