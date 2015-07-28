// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProteinId.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Class for building a data graph from an InformedProteomics SequenceGraph.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.SequenceGraph
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Enum;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Database;

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
            var ntermMods = (from m in mods
                             where (m.IsFixedModification && (m.Location == SequenceLocation.ProteinNTerm || m.Location == SequenceLocation.PeptideNTerm))
                             select m).ToList();
            var ctermMods = (from m in mods
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

            this.nterminal = null;
            this.cterminal = null;
            if (ntermMod != null)
            {
                this.nterminal = new ModifiedAminoAcid(new AminoAcid('(', "NTerm", Composition.Zero), ntermMod);
            }

            if (ctermMod != null)
            {
                this.cterminal = new ModifiedAminoAcid(new AminoAcid(')', "CTerm", Composition.Zero), ctermMod);
            }

            this.DataGraph = new DataGraph();
            this.BuildGraph();
        }

        /// <summary>
        /// Gets the data graph for the given sequence graph.
        /// </summary>
        public DataGraph DataGraph { get; private set; }

        /// <summary>
        /// Gets the end point of the entire sequence graph.
        /// </summary>
        public DataVertex EndPoint
        {
            get { return this.vertices[0][0]; }
        }

        /// <summary>
        /// Create an instance of a GraphXSequenceGraph for a particular sequence and set of search modifications.
        /// </summary>
        /// <param name="aminoAcidSet">The amino acid set.</param>
        /// <param name="annotation">Annotation (n-terminal, sequence, c-terminal).</param>
        /// <param name="mods">The search modification to apply to the sequence</param>
        /// <returns>Constructed GraphXSequenceGraph.</returns>
        public static GraphXSequenceGraph Create(AminoAcidSet aminoAcidSet, string annotation, IEnumerable<SearchModification> mods)
        {
            const char Delimiter = (char)FastaDatabase.Delimiter;
            if (annotation == null
                || !Regex.IsMatch(annotation, @"^[A-Z" + Delimiter + @"]\.[A-Z]+\.[A-Z" + Delimiter + @"]$"))
            {
                return null;
            }

            var nterm = annotation[0] == FastaDatabase.Delimiter
                                  ? AminoAcid.ProteinNTerm
                                  : AminoAcid.PeptideNTerm;
            var cterm = annotation[annotation.Length - 1] == FastaDatabase.Delimiter
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
            var sequenceRev = this._sequence.Reverse();
            var sequence = sequenceRev.Aggregate(string.Empty, (current, aa) => current + aa);
            sequence = "\0" + sequence;
            this.vertices = new DataVertex[this._maxSeqIndex][];
            var mods = this.AminoAcidSet.GetModificationParams();
            int id = 0;
            int ntermIndex = 0;
            int start = this._maxSeqIndex - 2;
            int end = 0;
            int offset = 1;
            if (this.nterminal != null)
            {
                start++;
                sequence += this.nterminal.Residue;
            }

            if (this.cterminal != null)
            {
                end--;
                offset = 0;
                sequence = sequence.Insert(1, this.cterminal.Residue.ToString(CultureInfo.InvariantCulture));
            }

            // create vertices
            for (var si = start; si > end; si--)
            {
                var graphSi = si - offset;
                this.vertices[graphSi] = new DataVertex[this._graph[si].Length];
                for (var mi = 0; mi < this._graph[si].Length; mi++)
                {
                    var node = this._graph[si][mi];
                    var mod = mods.GetModificationCombination(node.ModificationCombinationIndex);
                    this.SetSink(mi);
                    this.vertices[graphSi][mi] = new DataVertex
                    {
                        ID = id++,
                        NTermIndex = ntermIndex,
                        ModIndex = mi,
                        PrefixComposition = this.GetComplementaryComposition(si, mi),
                        SuffixComposition = this.GetComposition(si, mi),
                        ModificationCombination = mod,
                        Text = string.Empty
                    };
                    var vertex = this.vertices[graphSi][mi];
                    this.DataGraph.AddVertex(vertex);
                }

                ntermIndex++;
            }

            // connect vertices
            for (var si = start; si > (end + 1); si--)
            {
                var graphSi = si - offset;
                for (int mi = 0; mi < this._graph[si].Length; mi++)
                {
                    var node = this._graph[si][mi];
                    var currVertex = this.vertices[graphSi][mi];
                    foreach (var nextModIndex in node.GetPrevNodeIndices())
                    {
                        var nextVertex = this.vertices[graphSi - 1][nextModIndex];
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
                        if (si == start && this.nterminal != null)
                        {
                            aminoAcid = this.nterminal;
                        }
                        else if (si == end + 2 && this.cterminal != null)
                        {
                            aminoAcid = this.cterminal;
                        }
                        else
                        {
                            aminoAcid = this.AminoAcidSet.GetAminoAcid(sequence[graphSi]);
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

                        this.DataGraph.AddEdge(edge);
                    }
                }
            }
        }
    }
}
