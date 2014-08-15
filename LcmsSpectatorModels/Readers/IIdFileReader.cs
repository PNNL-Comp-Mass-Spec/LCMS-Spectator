using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Readers
{
    public interface IIdFileReader
    {
        IdentificationTree Read(RtLcMsRun lcms, string rawFileName);
    }
}
