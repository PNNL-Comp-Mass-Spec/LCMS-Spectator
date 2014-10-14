using System;
using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LcmsSpectator.DialogServices;

namespace LcmsSpectator.ViewModels
{
    public class FilterViewModel: ViewModelBase
    {
        public string Title { get; private set; }
        public string Description { get; private set; }
        public List<string> Values { get; private set; }
        public string SelectedValue { get; set; }
        public RelayCommand FilterCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
        public bool Status { get; private set; }
        public event EventHandler ReadyToClose;
        public Validate Validator { get; private set; }
        public delegate bool Validate(object value);
        public FilterViewModel(string title, string description, string defaultValue, List<string> values, Validate validator, IDialogService dialogService)
        {
            Title = title;
            Description = description;
            Values = values;
            Validator = validator;
            FilterCommand = new RelayCommand(Filter);
            CancelCommand = new RelayCommand(Cancel);
            _defaultValue = defaultValue;
            SelectedValue = defaultValue;
            _dialogService = dialogService;
            Status = false;
        }

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
        private readonly string _defaultValue;
    }
}
