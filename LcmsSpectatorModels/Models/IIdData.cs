using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectatorModels.Models
{
    public interface IIdData
    {
        void Add(PrSm data);
        void Remove(PrSm data);
        //void RemovePrSmsFromRawFile(string rawFileName);
        void SetLcmsRun(ILcMsRun lcms, string rawFileName);
        bool Contains(PrSm data);
        PrSm GetHighestScoringPrSm();
    }
}
