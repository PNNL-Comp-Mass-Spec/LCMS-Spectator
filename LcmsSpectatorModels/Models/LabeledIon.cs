using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Models
{
    public class LabeledIon: LabeledData
    {
        public Peak[] Peaks { get; private set; }
        public double CorrelationScore { get; private set; }

        public LabeledIon(int scan, int index, Peak[] peaks, double correlationScore, IonType ionType, bool isFragmentIon=true):
               base(scan, index, ionType, isFragmentIon)
        {
            Peaks = peaks;
            CorrelationScore = correlationScore;
        }
    }
}
