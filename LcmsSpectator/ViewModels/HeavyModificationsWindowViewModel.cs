using System;
using GalaSoft.MvvmLight.Command;
using LcmsSpectatorModels.Config;

namespace LcmsSpectator.ViewModels
{
    public class HeavyModificationsWindowViewModel: ViewModelBase
    {
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; private set; }
        
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }

        public bool Status { get; private set; }

        public event EventHandler ReadyToClose;

        public HeavyModificationsWindowViewModel()
        {
            HeavyModificationsViewModel = new HeavyModificationsViewModel();

            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);

            Status = false;
        }

        public void Save()
        {
            HeavyModificationsViewModel.Save();
            Status = true;
            IcParameters.Instance.Update();
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        public void Cancel()
        {
            Status = false;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }
    }
}
