using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.Readers.SequenceReaders
{
    public class SequenceReader: ISequenceReader
    {
        public Sequence Read(string sequence, bool trim=false)
        {
            ISequenceReader reader;
            if (!sequence.Contains("[")) reader = new MsgfPlusSequenceReader();
            else reader = new LcmsSpectatorSequenceReader();
            return reader.Read(sequence, trim);
        }
    }
}
