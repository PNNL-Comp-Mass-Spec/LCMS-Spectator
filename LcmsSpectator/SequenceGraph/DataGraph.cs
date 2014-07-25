using System;
using System.Collections.Generic;
using System.Linq;
using GraphX;
using GraphX.Logic;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using QuickGraph;

namespace LcmsSpectator.SequenceGraph
{
    //Graph data class
    public class DataGraph : BidirectionalGraph<DataVertex, DataEdge>
    {
        public List<DataVertex> GetSequencePath(DataVertex end, Sequence sequence)
        {
            var vertices = new List<DataVertex>();
            var revSequence = new Sequence(sequence);
            revSequence.Reverse();
            var vertex = end;
            for (int i = 0; i < revSequence.Count; i++)
            {
                if (vertex == null) break;
                vertices.Add(vertex);
                IEnumerable<DataEdge> inEdges;
                if (TryGetInEdges(vertex, out inEdges) && inEdges != null)
                {
                    var edges = inEdges.ToList();
                    if (edges.Count == 0) break;
                    DataVertex newVertex = null;
                    var modaa = revSequence[i] as ModifiedAminoAcid;
                    foreach (var edge in edges)
                    {
                        var modEdgeaa = edge.AminoAcid as ModifiedAminoAcid;
                        if ((modaa != null && modEdgeaa != null &&
                            modEdgeaa.Residue == modaa.Residue && 
                            modEdgeaa.Modification.Equals(modaa.Modification)))
                        {
                            newVertex = edge.GetOtherVertex(vertex);
                        }
                        else if (modaa == null && modEdgeaa == null && 
                                 edge.AminoAcid.Residue == revSequence[i].Residue)
                        {
                            newVertex = edge.GetOtherVertex(vertex);
                        }
                    }
                    vertex = newVertex;
                }
            }
            return vertices;
        }

        public List<List<DataVertex>> GetAllSequencePaths(DataVertex start, DataVertex end)
        {
            var sequences = new List<List<DataVertex>> { new List<DataVertex>() };
            sequences[0].Add(start);
            GetAllSequencePathsRec(start, end, sequences, 0);
            return sequences;
        }

        private void GetAllSequencePathsRec(DataVertex start, DataVertex end, List<List<DataVertex>> sequences,
                                            int sequenceIndex)
        {
            IEnumerable<DataEdge> edges;
            TryGetOutEdges(start, out edges);
            if (edges == null)          // reached end
            {
                sequences[sequenceIndex].Add(start);
                return;
            }
            var edgeList = edges.ToList();
            if (edgeList.Count == 0 || start.ID == end.ID)  // reached end
            {
                sequences[sequenceIndex].Add(start);
            }
            else if (edgeList.Count == 1)   // straight path
            {
                var newVertex = edgeList[0].GetOtherVertex(start);
                if (newVertex != null)
                {
                    sequences[sequenceIndex].Add(newVertex);
                    GetAllSequencePathsRec(newVertex, end, sequences, sequenceIndex);
                }
            }
            else    // branch
            {
                foreach (var edge in edgeList)
                {
                    var newVertex = edge.GetOtherVertex(start);
                    if (newVertex == null) continue;
                    sequences[sequenceIndex].Add(newVertex);
                    sequences.Add(new List<DataVertex>());
                    var newIndex = sequences.Count - 1;
                    foreach (var vertex in sequences[sequenceIndex]) sequences[newIndex].Add(vertex);
                    GetAllSequencePathsRec(newVertex, end, sequences, newIndex);
                }
            }
        }
    }

    //Logic core class
    public class LogicCore : GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }

    //Vertex data object
    public class DataVertex : VertexBase
    {
        public DataVertex()
        {
            Text = "";
        }
        public string Text { get; set; }
        public int NTermIndex { get; set; }
        public int ModIndex { get; set; }

        public Composition PrefixComposition { get; set; }
        public Composition SuffixComposition { get; set; }
        public ModificationCombination ModificationCombination { get; set; }

        public override string ToString()
        {
            return Text;
        }
    }

    //Edge data object
    public class DataEdge : EdgeBase<DataVertex>
    {
        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {

            Modifications = ModificationCombination.NoModification;
        }

        public DataEdge()
            : base(null, null, 1)
        {
            Modifications = ModificationCombination.NoModification;
            SequenceIndex = 0;
        }

        public AminoAcid AminoAcid { get; set; }

        public ModificationCombination Modifications
        {
            get { return _modifications; }
            set
            {
                _modifications = value;
                if (_modifications.Modifications.Count > 0)
                    AminoAcid = new ModifiedAminoAcid(AminoAcid, _modifications.Modifications.LastOrDefault());
            }
        }

        public int SequenceIndex { get; set; }

        public string Text
        {
            get
            {
                if (AminoAcid == null) return "";
                var text =
                    (Modifications.GetNumModifications() == 0)
                        ? String.Format("{0}", AminoAcid.Residue)
                        : String.Format("{0}", Modifications);
                return text;
            }
        }

        public override string ToString()
        {
            return Text;
        }

        private ModificationCombination _modifications;
    }
}
