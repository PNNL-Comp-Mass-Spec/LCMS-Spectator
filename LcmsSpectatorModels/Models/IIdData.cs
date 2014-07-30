namespace LcmsSpectatorModels.Models
{
    public interface IIdData
    {
        void Add(PrSm data);
        void Remove(PrSm data);
        bool Contains(PrSm data);
        PrSm GetHighestScoringPrSm();
    }
}
