using System;
using System.Windows.Input;

namespace LcmsSpectator.ViewModels
{
    public class DelegateCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public DelegateCommand(Action actionToTake, bool canExecute=true)
        {
            _canExecute = canExecute;
            _action = actionToTake;
        }
        public bool CanExecute(object parameter)
        {
            return _canExecute;
        }

        public bool Executable
        {
            get { return _canExecute; }
            set
            {
                _canExecute = value;
                if (CanExecuteChanged != null)
                    CanExecuteChanged(this, null);
            }
        }

        public void Execute(object parameter)
        {
            _action();
        }

        private bool _canExecute;
        private readonly Action _action;
    }
}
