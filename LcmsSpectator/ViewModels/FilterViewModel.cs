using System;
using System.Collections.Generic;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class FilterViewModel: ReactiveObject
    {
        public FilterViewModel(string title, string description, string defaultValue, List<string> values, Validate validator, IDialogService dialogService)
        {
            Title = title;
            Description = description;
            Values = values;
            Validator = validator;
            var filterCommand = ReactiveCommand.Create();
            filterCommand.Subscribe(_ => Filter());
            FilterCommand = filterCommand;
            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => Cancel());
            CancelCommand = cancelCommand;
            SelectedValue = defaultValue;
            _dialogService = dialogService;
            Status = false;
        }

        public string Title { get; private set; }
        public string Description { get; private set; }
        public List<string> Values { get; private set; }
        public string SelectedValue { get; set; }
        public IReactiveCommand FilterCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }
        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;
        public Validate Validator { get; private set; }
        public delegate bool Validate(object value);
        
        private void Filter()
        {
            if (Validator(SelectedValue))
            {
                Status = true;
                if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);   
            }
            else
            {
                _dialogService.MessageBox("Invalid filter value.");
            }
        }

        private void Cancel()
        {
            Status = false;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);  
        }

        private readonly IDialogService _dialogService;
    }
}
