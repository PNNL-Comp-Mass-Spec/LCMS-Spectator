using System.Collections;
using System.Collections.Generic;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectatorModels.Models;

namespace LcmsSpectatorModels.Readers
{
    public interface IIdFileReader
    {
        IdentificationTree Read(IEnumerable<string> modIgnoreList=null);
    }
}
