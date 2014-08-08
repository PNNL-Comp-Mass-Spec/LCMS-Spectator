using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectatorModels.SequenceReaders
{
    public class SequenceReader: ISequenceReader
    {
        public Sequence Read(string sequence)
        {
            if (!sequence.Contains("[")) 
                return Sequence.GetSequenceFromMsGfPlusPeptideStr(sequence);
            var reader = new LcmsSpectatorSequenceReader();
            return reader.Read(sequence);
        }
    }
}
