using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Config
{
    public interface IIdFileReader
    {
        IdentificationTree Read(LcMsRun lcms, string rawFileName);
    }
}
