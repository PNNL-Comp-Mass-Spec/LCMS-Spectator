using InformedProteomics.Backend.Data.Spectrometry;

namespace LcmsSpectatorModels.Models
{
    public class XicRetPoint: XicPoint
    {
        public double RetentionTime { get; private set; }
        public XicRetPoint(int scanNum, double retentionTime, double mz, double intensity) : base(scanNum, mz, intensity)
        {
            RetentionTime = retentionTime;
        }
    }
}
