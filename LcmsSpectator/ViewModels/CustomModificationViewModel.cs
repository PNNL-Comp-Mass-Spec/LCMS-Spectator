using System;
using InformedProteomics.Backend.Data.Composition;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class CustomModificationViewModel: ReactiveObject
    {
        public bool Status { get; private set; }
        public IReactiveCommand SaveCommand { get; private set; }
        public IReactiveCommand CancelCommand { get; private set; }
        public event EventHandler ReadyToClose;
        public CustomModificationViewModel(string modificationName, bool modificationNameReadOnly)
        {
            ModificationName = modificationName;
            ModificationNameReadOnly = modificationNameReadOnly;
            C = 0;
            H = 0;
            N = 0;
            O = 0;
            S = 0;
            P = 0;
            Status = false;
            var saveCommand = ReactiveCommand.Create();
            saveCommand.Subscribe(_ => Save());
            SaveCommand = saveCommand;

            var cancelCommand = ReactiveCommand.Create();
            cancelCommand.Subscribe(_ => Cancel());
            CancelCommand = cancelCommand;
        }

        public CustomModificationViewModel(string modificationName, bool modificationNameEditable, Composition composition): this(modificationName, modificationNameEditable)
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

        private void Save()
        {
            Status = true;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

        private void Cancel()
        {
            Status = false;
            if (ReadyToClose != null) ReadyToClose(this, EventArgs.Empty);
        }

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
