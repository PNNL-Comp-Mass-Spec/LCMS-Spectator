using GalaSoft.MvvmLight.Command;
using InformedProteomics.Backend.MassSpecData;

namespace LcmsSpectator.ViewModels
{
    public interface IXicViewModel
    {
        ILcMsRun Lcms { get; }
        RelayCommand CloseCommand { get; }
        string RawFileName { get; }
        string RawFilePath { get; set; }
    }
}
