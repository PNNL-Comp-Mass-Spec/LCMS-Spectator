using System.Collections.Generic;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectator.SequenceGraph
{
    public class SequencePath: List<DataVertex>
    {
        public Sequence Sequence { get; private set; }

        public SequencePath()
        {
            Sequence = new Sequence(new List<AminoAcid>());
        }

        public SequencePath(IEnumerable<DataVertex> vertices, IEnumerable<DataEdge> edges)
        {
            AddRange(vertices);
            GenerateSequence(edges);
        }

        public void Add(DataVertex vertex, DataEdge inEdge)
        {
            Add(vertex);
            Sequence.Add(inEdge.AminoAcid);
        }

        private void GenerateSequence(IEnumerable<DataEdge> edges)
        {
            var aminoAcids = edges.Select(edge => edge.AminoAcid).ToList();
            Sequence = new Sequence(aminoAcids);
        }
    }
}
