using System;
using InformedProteomics.Backend.Data.Composition;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class CustomModificationViewModel: ReactiveObject
    {
        public bool Status { get; private set; }
        public IReactiveCommand SaveCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }
        public event EventHandler ReadyToClose;
        public CustomModificationViewModel(string modificationName, bool modificationNameReadOnly, IDialogService dialogService)
        {
            _dialogService = dialogService;
            ModificationName = modificationName;
            ModificationNameReadOnly = modificationNameReadOnly;
            C = 0;
            H = 0;
            N = 0;
            O = 0;
            S = 0;
            P = 0;
            Status = false;

            FromFormulaChecked = true;
            FromMassChecked = false;

            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => Save());
            SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => Cancel());
            CancelCommand = cancelCommand;
        }

        public CustomModificationViewModel(string modificationName, bool modificationNameEditable, Composition composition, IDialogService dialogService): this(modificationName, modificationNameEditable, dialogService)
        {
            Composition = composition;
        }

        public string ModificationName
        {
            get { return _modificationName; }
            set { this.RaiseAndSetIfChanged(ref _modificationName, value); }
        }

        public bool ModificationNameReadOnly
        {
            get { return _modificationNameReadOnly; }
            set { this.RaiseAndSetIfChanged(ref _modificationNameReadOnly, value); }
        }

        public int C
        {
            get { return _c; }
            set { this.RaiseAndSetIfChanged(ref _c, value); }
        }

        public int H
        {
            get { return _h; }
            set { this.RaiseAndSetIfChanged(ref _h, value); }
        }

        public int N
        {
            get { return _n; }
            set { this.RaiseAndSetIfChanged(ref _n, value); }
        }

        public int O
        {
            get { return _o; }
            set { this.RaiseAndSetIfChanged(ref _o, value); }
        }

        public int S
        {
            get { return _s; }
            set { this.RaiseAndSetIfChanged(ref _s, value); }
        }

        public int P
        {
            get { return _p; }
            set { this.RaiseAndSetIfChanged(ref _p, value); }
        }

        public Composition Composition
        {
            get
            {
                return new Composition(C, H, N, O, S);
            }
            set
            {
                C = value.C;
                H = value.H;
                N = value.N;
                O = value.O;
                S = value.S;
            }
        }

        private string _massStr;
        public string MassStr
        {
             get { return _massStr; }
             set { this.RaiseAndSetIfChanged(ref _massStr, value); }
        }

        private double _mass;
        public double Mass
        {
            get { return _mass; }
            set { this.RaiseAndSetIfChanged(ref _mass, value); }
        }

        private bool _fromFormulaChecked;
        public bool FromFormulaChecked
        {
            get { return _fromFormulaChecked; }
            set { this.RaiseAndSetIfChanged(ref _fromFormulaChecked, value); }
        }

        private bool _fromMassChecked;
        public bool FromMassChecked
        {
            get { return _fromMassChecked; }
            set { this.RaiseAndSetIfChanged(ref _fromMassChecked, value); }
        }

        private void Save()
        {
            double mass = 0.0;
            if (FromMassChecked && (String.IsNullOrEmpty(_massStr) || !Double.TryParse(_massStr, out mass)))
            {
                _dialogService.MessageBox(String.Format("Invalid mass: {0}", _massStr));
                return;
            }

            _mass = mass;

            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            Status = false;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        private readonly IDialogService _dialogService;

        private string _modificationName;
        private int _c;
        private int _h;
        private int _n;
        private int _o;
        private int _s;
        private int _p;
        private bool _modificationNameReadOnly;
    }
}
