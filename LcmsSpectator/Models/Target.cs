using System;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Readers.SequenceReaders;

namespace LcmsSpectator.Models
{
    public class Target
    {
        private string _sequenceText;
        public Sequence Sequence { get; private set; }
        public Target(string sequence, int charge=0)
        {
            SequenceText = sequence;
            Charge = charge;
        }

        public int Charge { get; set; }

        public string SequenceText
        {
            get { return _sequenceText; }
            set
            {
                var seqReader = new SequenceReader();
                var seq = Sequence;
                try
                {
                    Sequence = seqReader.Read(value);
                    _sequenceText = value;
                }
                catch (Exception)
                {
                    Sequence = seq;
                }
            }
        }
    }
}
