using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectator.Utils;

namespace LcmsSpectator.ViewModels
{
    public class ChargeXicViewModel: ViewModelBase, IXicViewModel
    {
        public ILcMsRun Lcms { get; private set; }
        public RelayCommand CloseCommand { get; private set; }
        public ChargeXicViewModel(IMainDialogService dialogService, ITaskService taskService)
        {
            _dialogService = dialogService;
            _taskService = taskService;

            CloseCommand = new RelayCommand(() =>
            {
                if (_dialogService.ConfirmationBox(String.Format("Are you sure you would like to close {0}?", RawFileName), ""))
                    Messenger.Default.Send(new XicCloseRequest(this));
            });
        }

        public string RawFileName
        {
            get { return _rawFileName; }
            set
            {
                _rawFileName = value;
                RaisePropertyChanged();
            }
        }

        public string RawFilePath
        {
            get { return _rawFilePath; }
            set
            {
                _rawFilePath = value;
                RaisePropertyChanged();
            }
        }

        private IMainDialogService _dialogService;
        private ITaskService _taskService;

        private string _rawFileName;
        private string _rawFilePath;
    }
}
