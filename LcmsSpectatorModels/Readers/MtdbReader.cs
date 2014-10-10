using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectatorModels.Models;
using MTDBFramework;
using MTDBFramework.Data;
using MTDBFramework.Database;

namespace LcmsSpectatorModels.Readers
{
	public class MtdbReader : IIdFileReader
	{
		private string _fileName;

		public MtdbReader(string fileName)
		{
			_fileName = fileName;
		}


		public IdentificationTree Read()
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
						Modification mod = Modification.RegisterAndGetModification(ptm.Name, modComposition);
						entry.Sequence[ptm.Location - 1] = new ModifiedAminoAcid(entry.Sequence[ptm.Location - 1], mod);
					}
					
					entry.SequenceText = rawSequence;
					entry.Charge = id.Charge;
					entry.Scan = id.Scan;
					tree.Add(entry);
				}
			}

			return tree;
		}

	}
}
