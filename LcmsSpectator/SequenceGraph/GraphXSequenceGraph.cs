// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProteinId.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Class for building a data graph from an InformedProteomics SequenceGraph.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Database;

namespace LcmsSpectator.SequenceGraph
{
    /// <summary>
    /// Class for building a data graph from an InformedProteomics SequenceGraph.
    /// </summary>
    public class GraphXSequenceGraph : InformedProteomics.Backend.Data.Sequence.SequenceGraph
    {
        /// <summary>
        /// AminoAcid on n-terminus of sequence.
        /// </summary>
        private readonly AminoAcid nterminal;

        /// <summary>
        /// AminoAcid on c-terminus of sequence.
        /// </summary>
        private readonly AminoAcid cterminal;

        /// <summary>
        /// Set of data vertices for the GraphX data graph.
        /// </summary>
        private DataVertex[][] vertices;

        /// <summary>Initializes a new instance of the <see cref="GraphXSequenceGraph"/> class.</summary>
        /// <param name="aminoAcidSet">The amino acid set.</param>
        /// <param name="nterm">The n-terminal residue of the sequence.</param>
        /// <param name="sequence">The sequence.</param>
        /// <param name="cterm">The c-terminal residue of the sequence.</param>
        /// <param name="mods">The search modification to apply to the sequence.</param>
        protected GraphXSequenceGraph(AminoAcidSet aminoAcidSet, AminoAcid nterm, string sequence, AminoAcid cterm, IEnumerable<SearchModification> mods) :
                                          base(aminoAcidSet, nterm, sequence, cterm)
        {
            var modList = mods.ToList();

            var ntermMods = (from m in modList
                             where (m.IsFixedModification && (m.Location == SequenceLocation.ProteinNTerm || m.Location == SequenceLocation.PeptideNTerm))
                             select m).ToList();

            var ctermMods = (from m in modList
                             where (m.IsFixedModification && (m.Location == SequenceLocation.ProteinCTerm || m.Location == SequenceLocation.PeptideCTerm))
                             select m).ToList();

            Modification ntermMod = null;
            foreach (var nmod in ntermMods)
            {
                ntermMod = nmod.Modification;
                if (nmod.TargetResidue == nterm.Residue)
                {
                    break;
                }
            }

            Modification ctermMod = null;
            foreach (var cmod in ctermMods)
            {
                ctermMod = cmod.Modification;
                if (cmod.TargetResidue == cterm.Residue)
                {
                    break;
                }
            }

            nterminal = null;
            cterminal = null;
            if (ntermMod != null)
            {
                nterminal = new ModifiedAminoAcid(new AminoAcid('(', "NTerm", Composition.Zero), ntermMod);
            }

            if (ctermMod != null)
            {
                cterminal = new ModifiedAminoAcid(new AminoAcid(')', "CTerm", Composition.Zero), ctermMod);
            }

            DataGraph = new DataGraph();
            BuildGraph();
        }

        /// <summary>
        /// Gets the data graph for the given sequence graph.
        /// </summary>
        public DataGraph DataGraph { get; }

        /// <summary>
        /// Gets the end point of the entire sequence graph.
        /// </summary>
        public DataVertex EndPoint => vertices[0][0];

        /// <summary>
        /// Create an instance of a GraphXSequenceGraph for a particular sequence and set of search modifications.
        /// </summary>
        /// <param name="aminoAcidSet">The amino acid set.</param>
        /// <param name="annotation">Annotation (n-terminal, sequence, c-terminal).</param>
        /// <param name="mods">The search modification to apply to the sequence</param>
        /// <returns>Constructed GraphXSequenceGraph.</returns>
        public static GraphXSequenceGraph Create(AminoAcidSet aminoAcidSet, string annotation, IEnumerable<SearchModification> mods)
        {
            const char Delimiter = (char)FastaDatabaseConstants.Delimiter;
            if (annotation == null
                || !Regex.IsMatch(annotation, "^[A-Z" + Delimiter + @"]\.[A-Z]+\.[A-Z" + Delimiter + "]$"))
            {
                return null;
            }

            var nterm = annotation[0] == FastaDatabaseConstants.Delimiter
                                  ? AminoAcid.ProteinNTerm
                                  : AminoAcid.PeptideNTerm;
            var cterm = annotation[annotation.Length - 1] == FastaDatabaseConstants.Delimiter
                                  ? AminoAcid.ProteinCTerm
                                  : AminoAcid.PeptideCTerm;

            var sequence = annotation.Substring(2, annotation.Length - 4);
            return new GraphXSequenceGraph(aminoAcidSet, nterm, sequence, cterm, mods);
        }

        /// <summary>
        /// Build the data graph from a sequence graph.
        /// </summary>
        private void BuildGraph()
        {
            var sequenceRev = _sequence.Reverse();
            var sequence = sequenceRev.Aggregate(string.Empty, (current, aa) => current + aa);
            sequence = "\0" + sequence;
            vertices = new DataVertex[_maxSeqIndex][];
            var mods = AminoAcidSet.GetModificationParams();
            var id = 0;
            var ntermIndex = 0;
            var start = _maxSeqIndex - 2;
            var end = 0;
            var offset = 1;
            if (nterminal != null)
            {
                start++;
                sequence += nterminal.Residue;
            }

            if (cterminal != null)
            {
                end--;
                offset = 0;
                sequence = sequence.Insert(1, cterminal.Residue.ToString(CultureInfo.InvariantCulture));
            }

            // create vertices
            for (var si = start; si > end; si--)
            {
                var graphSi = si - offset;
                vertices[graphSi] = new DataVertex[_graph[si].Length];
                for (var mi = 0; mi < _graph[si].Length; mi++)
                {
                    var node = _graph[si][mi];
                    var mod = mods.GetModificationCombination(node.ModificationCombinationIndex);
                    SetSink(mi);
                    vertices[graphSi][mi] = new DataVertex
                    {
                        ID = id++,
                        NTermIndex = ntermIndex,
                        ModIndex = mi,
                        PrefixComposition = GetComplementaryComposition(si, mi),
                        SuffixComposition = GetComposition(si, mi),
                        ModificationCombination = mod,
                        Text = string.Empty
                    };
                    var vertex = vertices[graphSi][mi];
                    DataGraph.AddVertex(vertex);
                }

                ntermIndex++;
            }

            // connect vertices
            for (var si = start; si > (end + 1); si--)
            {
                var graphSi = si - offset;
                for (var mi = 0; mi < _graph[si].Length; mi++)
                {
                    var node = _graph[si][mi];
                    var currVertex = vertices[graphSi][mi];
                    foreach (var nextModIndex in node.GetPrevNodeIndices())
                    {
                        var nextVertex = vertices[graphSi - 1][nextModIndex];
                        var currVertexMods = currVertex.ModificationCombination.Modifications;
                        var nextVertexMods = nextVertex.ModificationCombination.Modifications;
                        var result = new List<Modification>(currVertexMods);
                        foreach (var mod in nextVertexMods)
                        {
                            if (result.Contains(mod))
                            {
                                result.Remove(mod);
                            }
                        }

                        AminoAcid aminoAcid;
                        if (si == start && nterminal != null)
                        {
                            aminoAcid = nterminal;
                        }
                        else if (si == end + 2 && cterminal != null)
                        {
                            aminoAcid = cterminal;
                        }
                        else
                        {
                            aminoAcid = AminoAcidSet.GetAminoAcid(sequence[graphSi]);
                        }

                        var modAa = aminoAcid as ModifiedAminoAcid;
                        Modification aminoAcidMod = null;
                        if (modAa != null)
                        {
                            aminoAcidMod = modAa.Modification;
                        }

                        if (aminoAcidMod != null)
                        {
                            result.Add(aminoAcidMod);
                        }

                        var edgeModifications = new ModificationCombination(result);
                        var edge = new DataEdge(currVertex, nextVertex)
                        {
                            AminoAcid = aminoAcid,
                            SequenceIndex = graphSi,
                            Modifications = edgeModifications
                        };

                        DataGraph.AddEdge(edge);
                    }
                }
            }
        }
    }
}
