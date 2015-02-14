using System;
using LcmsSpectator.Config;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class HeavyModificationsWindowViewModel: ReactiveObject
    {
        public HeavyModificationsViewModel HeavyModificationsViewModel { get; private set; }
        
        public IReactiveCommand SaveCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }

        public bool Status { get; private set; }

        public event EventHandler ReadyToClose;

        public HeavyModificationsWindowViewModel()
        {
            HeavyModificationsViewModel = new HeavyModificationsViewModel();

            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => Save());
            SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => Cancel());
            CancelCommand = cancelCommand;

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
