using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LcmsSpectator.Models;
using LcmsSpectator.ViewModels;

namespace LcmsSpectator.Readers
{
    public class DataSetReader: IDataSetReader
    {

        public Task<DataSetViewModel> OpenDataSet(string rawFilePath, string idFilePath, string featureFilePath, Models.ToolType tool = ToolType.MsPathFinder)
        {
            throw new NotImplementedException();
        }

        public Task<DataSetViewModel> OpenDataSet(DataSetViewModel dsVm, string idFilePath, string featureFilePath, Models.ToolType tool = ToolType.MsPathFinder)
        {
            throw new NotImplementedException();
        }

        public Task<DataSetViewModel> OpenFromDms(DmsLookupViewModel dmsLookUp)
        {
            throw new NotImplementedException();
        }
    }
}
