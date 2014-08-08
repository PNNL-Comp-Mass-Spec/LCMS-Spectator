namespace LcmsSpectatorModels.Models
{
    public interface IIdData
    {
        void Add(PrSm data);
        void Remove(PrSm data);
        void RemovePrSmsFromRawFile(string rawFileName);
        bool Contains(PrSm data);
        PrSm GetHighestScoringPrSm();
    }
}
