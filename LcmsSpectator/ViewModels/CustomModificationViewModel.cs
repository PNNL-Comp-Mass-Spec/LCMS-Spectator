using System;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using InformedProteomics.Backend.Data.Composition;

namespace LcmsSpectator.ViewModels
{
    public class CustomModificationViewModel: ViewModelBase
    {
        public bool Status { get; private set; }
        public RelayCommand SaveCommand { get; private set; }
        public RelayCommand CancelCommand { get; private set; }
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
            SaveCommand = new RelayCommand(Save);
            CancelCommand = new RelayCommand(Cancel);
        }

        public CustomModificationViewModel(string modificationName, bool modificationNameEditable, Composition composition): this(modificationName, modificationNameEditable)
        {
            Composition = composition;
        }

        public string ModificationName
        {
            get { return _modificationName; }
            set
            {
                _modificationName = value;
                RaisePropertyChanged();
            }
        }

        public bool ModificationNameReadOnly
        {
            get { return _modificationNameReadOnly; }
            set
            {
                _modificationNameReadOnly = value;
                RaisePropertyChanged();
            }
        }

        public int C
        {
            get { return _c; }
            set
            {
                _c = value;
                RaisePropertyChanged();
            }
        }

        public int H
        {
            get { return _h; }
            set
            {
                _h = value;
                RaisePropertyChanged();
            }
        }

        public int N
        {
            get { return _n; }
            set
            {
                _n = value;
                RaisePropertyChanged();
            }
        }

        public int O
        {
            get { return _o; }
            set
            {
                _o = value;
                RaisePropertyChanged();
            }
        }

        public int S
        {
            get { return _s; }
            set
            {
                _s = value;
                RaisePropertyChanged();
            }
        }

        public int P
        {
            get { return _p; }
            set
            {
                _p = value;
                RaisePropertyChanged();
            }
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
        private bool _modificationNameEditable;
        private int _c;
        private int _h;
        private int _n;
        private int _o;
        private int _s;
        private int _p;
        private bool _modificationNameReadOnly;
    }
}
