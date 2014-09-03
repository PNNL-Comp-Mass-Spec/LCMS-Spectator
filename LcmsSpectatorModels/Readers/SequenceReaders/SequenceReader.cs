using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectatorModels.Readers.SequenceReaders
{
    public class SequenceReader: ISequenceReader
    {
        public Sequence Read(string sequence)
        {
            ISequenceReader reader;
            if (!sequence.Contains("[")) reader = new MsgfPlusSequenceReader();
            else reader = new LcmsSpectatorSequenceReader();
            return reader.Read(sequence);
        }
    }
}
