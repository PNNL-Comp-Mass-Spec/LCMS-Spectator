using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectator.Models
{
    public class LabeledIonPeaks: LabeledIon
    {
        public Peak[] Peaks { get; private set; }
        public double CorrelationScore { get; private set; }

        public LabeledIonPeaks(Composition composition, int index, Peak[] peaks, double correlationScore, IonType ionType, bool isFragmentIon=true):
               base(composition, index, ionType, isFragmentIon)
        {
            Peaks = peaks;
            CorrelationScore = correlationScore;
        }
    }
}
