using System.Collections.Generic;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;

namespace LcmsSpectatorModels.Utils
{
    public class Utils
    {
        public static List<Composition> GetCompositions(Sequence sequence, bool prefix)
        {
            var compositions = new List<Composition>();
            for (int i = 1; i < sequence.Count; i++)
            {
                compositions.Add(prefix
                    ? sequence.GetComposition(0, i)
                    : sequence.GetComposition(i, sequence.Count));
            }
            if (!prefix) compositions.Reverse();
            return compositions;
        }
    }
}
