﻿namespace LcmsSpectator.Readers.SequenceReaders
{
    public interface ISequenceReader
    {
        InformedProteomics.Backend.Data.Sequence.Sequence Read(string sequence, bool trim=false);
    }
}
