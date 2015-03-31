using System.Threading.Tasks;
using LcmsSpectator.Models;
using LcmsSpectator.ViewModels;

namespace LcmsSpectator.Readers
{
    interface IDataSetReader
    {
        Task<DataSetViewModel> OpenDataSet(string rawFilePath, string idFilePath, string featureFilePath, ToolType tool=ToolType.MsPathFinder);
        Task<DataSetViewModel> OpenDataSet(DataSetViewModel dsVm, string idFilePath, string featureFilePath, ToolType tool=ToolType.MsPathFinder);
        Task<DataSetViewModel> OpenFromDms(DmsLookupViewModel dmsLookUp);
    }
}
