using System.Collections.Generic;
using LcmsSpectator.Models;

namespace LcmsSpectator.Readers
{
    public interface IIdFileReader
    {
        IdentificationTree Read(IEnumerable<string> modIgnoreList=null);
    }
}
