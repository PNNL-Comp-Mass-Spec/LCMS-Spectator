namespace LcmsSpectatorModels.Models
{
    public interface IIdData
    {
        void Add(PrSm data);
        bool Contains(PrSm data);
        PrSm GetHighestScoringPrSm();
    }
}
