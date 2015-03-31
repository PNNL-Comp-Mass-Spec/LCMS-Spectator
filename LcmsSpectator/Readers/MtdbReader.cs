using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Config;
using LcmsSpectator.Models;
using MTDBFramework;
using MTDBFramework.Database;

namespace LcmsSpectator.Readers
{
	public class MtdbReader : IIdFileReader
	{
		private readonly string _fileName;

		public MtdbReader(string fileName)
		{
			_fileName = fileName;
		}


        public IdentificationTree Read(IEnumerable<string> modIgnoreList = null)
		{
			IdentificationTree tree = new IdentificationTree();

			if (!File.Exists(_fileName))
			{
				return tree;
			}

			TargetDatabase database = MtdbCreator.LoadDB(_fileName);

			foreach (var target in database.ConsensusTargets)
			{
				foreach (var id in target.Evidences)
				{
                    // Degan: attempting to make it recognize the proteins from .mtdb format
				    foreach (var prot in id.Proteins)
				    {
				        string strippedSequence = target.Sequence;
				        strippedSequence = strippedSequence.Remove(0, 2);
				        strippedSequence = strippedSequence.Remove(strippedSequence.Length - 2, 2);
				        PrSm entry = new PrSm();
				        entry.Sequence = new Sequence(strippedSequence, new AminoAcidSet());

				        string rawSequence = target.Sequence;
				        int offset = target.Sequence.IndexOf('.');
				        int length = target.Sequence.Length;

				        foreach (var ptm in id.Ptms)
				        {
				            var position = rawSequence.Length - (length - (ptm.Location + offset));
				            string symbol = "";
				            // We only need to add the sign on positive values - the '-' is automatic on negative values
				            if (ptm.Mass >= 0)
				            {
				                symbol = "+";
				            }
				            rawSequence = rawSequence.Insert(position + 1, symbol + ptm.Mass);

				            Composition modComposition = Composition.ParseFromPlainString(ptm.Formula);
				            Modification mod = IcParameters.Instance.RegisterModification(ptm.Name, modComposition);
				            entry.Sequence[ptm.Location - 1] = new ModifiedAminoAcid(entry.Sequence[ptm.Location - 1], mod);
				        }

                        // Degan: attempting to make it recognize the proteins from .mtdb format
				        entry.ProteinName = prot.ProteinName;

                        // Degan: choosing stripped sequence rather than the raw sequence to exclude prior and successive amino acid
                        entry.SequenceText = strippedSequence;
				        entry.Charge = id.Charge;
				        entry.Scan = id.Scan;
				        tree.Add(entry);
				    }
				}
			}

			return tree;
		}

	    public Task<IdentificationTree> ReadAsync(IEnumerable<string> modIgnoreList = null)
	    {
	        return Task.Run(() => Read(modIgnoreList));
	    }

	}
}
