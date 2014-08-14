namespace LcmsSpectatorModels.Readers.SequenceReaders
{
    public interface ISequenceReader
    {
        InformedProteomics.Backend.Data.Sequence.Sequence Read(string sequence);
    }
}
