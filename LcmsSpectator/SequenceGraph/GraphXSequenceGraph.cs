using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using InformedProteomics.Backend.Data.Enum;
using InformedProteomics.Backend.Data.Sequence;
using InformedProteomics.Backend.Database;

namespace LcmsSpectator.SequenceGraph
{
    public class GraphXSequenceGraph: InformedProteomics.Backend.Data.Sequence.SequenceGraph
    {
        public DataGraph DataGraph { get; private set; }
        public static GraphXSequenceGraph Create(AminoAcidSet aaSet, string annotation, List<SearchModification> mods)
        {
            const char delimiter = (char)FastaDatabase.Delimiter;
            if (annotation == null || !Regex.IsMatch(annotation, @"^[A-Z" + delimiter + @"]\.[A-Z]+\.[A-Z" + delimiter + @"]$")) return null;

            var nTerm = annotation[0] == FastaDatabase.Delimiter
                                  ? AminoAcid.ProteinNTerm
                                  : AminoAcid.PeptideNTerm;
            var cTerm = annotation[annotation.Length - 1] == FastaDatabase.Delimiter
                                  ? AminoAcid.ProteinCTerm
                                  : AminoAcid.PeptideCTerm;

            var sequence = annotation.Substring(2, annotation.Length - 4);
            return new GraphXSequenceGraph(aaSet, nTerm, sequence, cTerm, mods);
        }

        public GraphXSequenceGraph(AminoAcidSet aminoAcidSet, AminoAcid nTerm, string sequence, AminoAcid cTerm, List<SearchModification> mods) :
                                          base(aminoAcidSet, nTerm, sequence, cTerm)
        {
            var nTermMods = (from m in mods
                             where (m.IsFixedModification && m.Location == SequenceLocation.ProteinNTerm || m.Location == SequenceLocation.PeptideNTerm)
                             select m).ToList();
            var cTermMods = (from m in mods
                             where (m.IsFixedModification && m.Location == SequenceLocation.ProteinCTerm || m.Location == SequenceLocation.PeptideCTerm)
                             select m).ToList();

            Modification nTermMod = null;
            foreach (var nMod in nTermMods)
            {
                nTermMod = nMod.Modification;
                if (nMod.TargetResidue == nTerm.Residue) break;
            }
            Modification cTermMod = null;
            foreach (var cMod in cTermMods)
            {
                cTermMod = cMod.Modification;
                if (cMod.TargetResidue == cTerm.Residue) break;
            }
            _nTerminal = null;
            _cTerminal = null;
            if (nTermMod != null) _nTerminal = new ModifiedAminoAcid(new AminoAcid('(', "NTerm", InformedProteomics.Backend.Data.Composition.Composition.Zero), nTermMod);
            if (cTermMod != null) _cTerminal = new ModifiedAminoAcid(new AminoAcid(')', "CTerm", InformedProteomics.Backend.Data.Composition.Composition.Zero), cTermMod);

            DataGraph = new DataGraph();
            BuildGraph();
        }

        public DataVertex EndPoint  { get { return _vertices[0][0]; } }

        private void BuildGraph()
        {
            var sequenceRev = _sequence.Reverse();
            var sequence = sequenceRev.Aggregate("", (current, aa) => current + aa);
            sequence = "\0" + sequence;
            _vertices = new DataVertex[_maxSeqIndex][];
            var mods = AminoAcidSet.GetModificationParams();
            int id = 0;
            int ntermIndex = 0;
            int start = _maxSeqIndex - 2;
            int end = 0;
            int offset = 1;
            if (_nTerminal != null)
            {
                start++;
                sequence += _nTerminal.Residue;
            }
            if (_cTerminal != null)
            {
                end--;
                offset = 0;
                sequence = sequence.Insert(1, _cTerminal.Residue.ToString(CultureInfo.InvariantCulture));
            }
            // create vertices
            for (var si = start; si > end; si--)
            {
                var graphSi = si-offset;
                _vertices[graphSi] = new DataVertex[_graph[si].Length];
                for (var mi = 0; mi < _graph[si].Length; mi++)
                {
                    var node = _graph[si][mi];
                    var mod = mods.GetModificationCombination(node.ModificationCombinationIndex);
                    SetSink(mi);
                    _vertices[graphSi][mi] = new DataVertex
                    {
                        ID = id++,
                        NTermIndex = ntermIndex,
                        ModIndex = mi,
                        PrefixComposition = GetComplementaryComposition(si, mi),
                        SuffixComposition = GetComposition(si, mi),
                        ModificationCombination = mod,
                        Text = ""
                    };
                    var vertex = _vertices[graphSi][mi];
                    DataGraph.AddVertex(vertex);
                }
                ntermIndex++;
            }
            // connect vertices
            for (var si = start; si > (end+1); si--)
            {
                var graphSi = si-offset;
                for (int mi = 0; mi < _graph[si].Length; mi++)
                {
                    var node = _graph[si][mi];
                    var currVertex = _vertices[graphSi][mi];
                    foreach (var nextModIndex in node.GetPrevNodeIndices())
                    {
                        var nextVertex = _vertices[graphSi - 1][nextModIndex];
                        var currVertexMods = currVertex.ModificationCombination.Modifications;
                        var nextVertexMods = nextVertex.ModificationCombination.Modifications;
                        var result = new List<Modification>(currVertexMods);
                        foreach (var mod in nextVertexMods)
                        {
                            if (result.Contains(mod)) result.Remove(mod);
                        }
                        AminoAcid aminoAcid;
                        if (si == start && _nTerminal != null) aminoAcid = _nTerminal;
                        else if (si == end + 2 && _cTerminal != null) aminoAcid = _cTerminal;
                        else aminoAcid = AminoAcidSet.GetAminoAcid(sequence[graphSi]);
                        var modAa = aminoAcid as ModifiedAminoAcid;
                        Modification aaMod = null;
                        if (modAa != null)  aaMod = modAa.Modification;
                        if (aaMod != null) result.Add(aaMod);
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

        private readonly AminoAcid _nTerminal;
        private readonly AminoAcid _cTerminal;
        private DataVertex[][] _vertices;
    }
}
