// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DataGraph.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Data model for GraphX graph.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.SequenceGraph
{
    using System.Collections.Generic;
    using System.Linq;

    using GraphX.PCL.Common.Models;
    using GraphX.PCL.Logic.Models;

    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;

    using QuickGraph;

    /// <summary>
    /// Data model for GraphX graph.
    /// </summary>
    public class DataGraph : BidirectionalGraph<DataVertex, DataEdge>
    {
        /// <summary>
        /// Gets the vertices associated for a path corresponding to a sequence in the graph.
        /// </summary>
        /// <param name="end">The end node in the graph (where the search starts).</param>
        /// <param name="sequence">The sequence to find in the graph.</param>
        /// <returns>A list of <see cref="DataVertex" />s.</returns>
        public List<DataVertex> GetSequencePath(DataVertex end, Sequence sequence)
        {
            var vertices = new List<DataVertex>();
            var revSequence = new Sequence(sequence);
            revSequence.Reverse();
            var vertex = end;
            foreach (var residue in revSequence)
            {
                if (vertex == null)
                {
                    break;
                }

                vertices.Add(vertex);
                if (TryGetInEdges(vertex, out var inEdges) && inEdges != null)
                {
                    var edges = inEdges.ToList();
                    if (edges.Count == 0)
                    {
                        break;
                    }

                    DataVertex newVertex = null;
                    var modaa = residue as ModifiedAminoAcid;
                    foreach (var edge in edges)
                    {
                        var modEdgeaa = edge.AminoAcid as ModifiedAminoAcid;
                        if (modaa != null && modEdgeaa != null &&
                            modEdgeaa.Residue == modaa.Residue &&
                            modEdgeaa.Modification.Equals(modaa.Modification))
                        {
                            newVertex = edge.GetOtherVertex(vertex);
                        }
                        else if (modaa == null && modEdgeaa == null &&
                                 edge.AminoAcid.Residue == residue.Residue)
                        {
                            newVertex = edge.GetOtherVertex(vertex);
                        }
                    }

                    vertex = newVertex;
                }
            }

            return vertices;
        }

        /// <summary>
        /// Get all possible paths between two nodes.
        /// </summary>
        /// <param name="start">The start node.</param>
        /// <param name="end">The end node.</param>
        /// <returns>List of paths, where each path is a list of vertices.</returns>
        public List<List<DataVertex>> GetAllSequencePaths(DataVertex start, DataVertex end)
        {
            var sequences = new List<List<DataVertex>> { new List<DataVertex>() };
            sequences[0].Add(start);
            GetAllSequencePathsRec(start, end, sequences, 0);
            return sequences;
        }

        /// <summary>
        /// Recursively get all possible paths between two nodes.
        /// </summary>
        /// <param name="start">The start node.</param>
        /// <param name="end">The end node.</param>
        /// <param name="sequences">List of paths, where each path is a list of vertices.</param>
        /// <param name="sequenceIndex">Distance from start node.</param>
        private void GetAllSequencePathsRec(DataVertex start, DataVertex end, IList<List<DataVertex>> sequences, int sequenceIndex)
        {
            TryGetOutEdges(start, out var edges);
            if (edges == null)
            {   // reached end
                sequences[sequenceIndex].Add(start);
                return;
            }

            var edgeList = edges.ToList();
            if (edgeList.Count == 0 || start.ID == end.ID)
            {   // reached end
                sequences[sequenceIndex].Add(start);
            }
            else if (edgeList.Count == 1)
            {   // straight path
                var newVertex = edgeList[0].GetOtherVertex(start);
                if (newVertex != null)
                {
                    sequences[sequenceIndex].Add(newVertex);
                    GetAllSequencePathsRec(newVertex, end, sequences, sequenceIndex);
                }
            }
            else
            {   // branch
                foreach (var edge in edgeList)
                {
                    var newVertex = edge.GetOtherVertex(start);
                    if (newVertex == null)
                    {
                        continue;
                    }

                    sequences[sequenceIndex].Add(newVertex);
                    sequences.Add(new List<DataVertex>());
                    var newIndex = sequences.Count - 1;
                    foreach (var vertex in sequences[sequenceIndex])
                    {
                        sequences[newIndex].Add(vertex);
                    }

                    GetAllSequencePathsRec(newVertex, end, sequences, newIndex);
                }
            }
        }
    }

    /// <summary>
    /// Concrete GraphX LogicCore class.
    /// </summary>
    public class LogicCore : GXLogicCore<DataVertex, DataEdge, BidirectionalGraph<DataVertex, DataEdge>> { }

    /// <summary>
    /// Data model for a vertex in the graph. Represents a .
    /// </summary>
    public class DataVertex : VertexBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataVertex"/> class.
        /// </summary>
        public DataVertex()
        {
            Text = string.Empty;
        }

        /// <summary>
        /// Gets the text label for the node.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the distance from the n-terminus for this node.
        /// </summary>
        public int NTermIndex { get; set; }

        /// <summary>
        /// Gets or sets the modification index in the modification combinations.
        /// </summary>
        public int ModIndex { get; set; }

        /// <summary>
        /// Gets or sets the chemical formula for this node for prefix fragments.
        /// </summary>
        public Composition PrefixComposition { get; set; }

        /// <summary>
        /// Gets or sets the chemical formula for this node for suffix fragments.
        /// </summary>
        public Composition SuffixComposition { get; set; }

        /// <summary>
        /// Gets or sets the modification combination for this node.
        /// </summary>
        public ModificationCombination ModificationCombination { get; set; }

        /// <summary>
        /// Gets the string representation of the node. (Node label text)
        /// </summary>
        /// <returns>The string.</returns>
        public override string ToString()
        {
            return Text;
        }
    }

    /// <summary>
    /// Data model for a directed edge in the graph. Represents a single amino acid and 0 to many PTMs.
    /// </summary>
    public class DataEdge : EdgeBase<DataVertex>
    {
        /// <summary>
        /// Post-translational modifications associated with this edge.
        /// </summary>
        private ModificationCombination modifications;

        /// <summary>Initializes a new instance of the <see cref="DataEdge"/> class.</summary>
        /// <param name="source">The source node.</param>
        /// <param name="target">The target node.</param>
        /// <param name="weight">The edge weight.</param>
        public DataEdge(DataVertex source, DataVertex target, double weight = 1)
            : base(source, target, weight)
        {
            Modifications = ModificationCombination.NoModification;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataEdge"/> class.
        /// </summary>
        public DataEdge()
            : base(null, null, 1)
        {
            Modifications = ModificationCombination.NoModification;
            SequenceIndex = 0;
        }

        /// <summary>
        /// Gets or sets the <see cref="AminoAcidSet" /> associated with this edge.
        /// </summary>
        public AminoAcid AminoAcid { get; set; }

        /// <summary>
        /// Gets or sets the post-translational modifications associated with this edge.
        /// </summary>
        public ModificationCombination Modifications
        {
            get => modifications;

            set
            {
                modifications = value;
                if (modifications.Modifications.Count > 0)
                {
                    AminoAcid = new ModifiedAminoAcid(AminoAcid, modifications.Modifications.LastOrDefault());
                }
            }
        }

        /// <summary>
        /// Gets or sets the index in the sequence graph in this
        /// </summary>
        public int SequenceIndex { get; set; }

        /// <summary>
        /// Gets the text label for the edge.
        /// </summary>
        public string Text
        {
            get
            {
                if (AminoAcid == null)
                {
                    return string.Empty;
                }

                var text =
                    (Modifications.GetNumModifications() == 0)
                        ? string.Format("{0}", AminoAcid.Residue)
                        : string.Format("{0}", Modifications);
                return text;
            }
        }

        /// <summary>
        /// Gets the string representation of the edge. (Edge label text)
        /// </summary>
        /// <returns>The string.</returns>
        public override string ToString()
        {
            return Text;
        }
    }
}
