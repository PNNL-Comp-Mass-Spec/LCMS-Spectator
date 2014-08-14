using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectatorModels.Readers.SequenceReaders;

namespace LcmsSpectatorModels.Models
{
    public class Target
    {
        public string SequenceText { get; private set; }
        public Sequence Sequence { get; private set; }
        public Target(string sequence)
        {
            var seqReader = new SequenceReader();
            SequenceText = sequence;
            Sequence = seqReader.Read(sequence);
        }
    }
}
