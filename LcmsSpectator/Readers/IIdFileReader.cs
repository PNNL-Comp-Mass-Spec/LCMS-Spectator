using System.Collections.Generic;
using System.Threading.Tasks;
using LcmsSpectator.Models;

namespace LcmsSpectator.Readers
{
    public interface IIdFileReader
    {
        IdentificationTree Read(IEnumerable<string> modIgnoreList = null);
        Task<IdentificationTree> ReadAsync(IEnumerable<string> modIgnoreList=null);
    }
}
