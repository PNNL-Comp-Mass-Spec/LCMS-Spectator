using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectatorModels.Models;

namespace LcmsSpectator.ViewModels
{
    public class ScanViewModel: ViewModelBase
    {
        public RelayCommand ClearFiltersCommand { get; private set; }
        public ScanViewModel(IMainDialogService dialogService, ITaskService taskService, List<PrSm> data)
        {
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            _taskService = taskService;
            _dialogService = dialogService;
            _filters = new Dictionary<string, string> { { "QValue", "0.01" } };
            _previousFilters = new Dictionary<string, string>
            {
                { "Sequence", "" },
                { "Protein", ""},
                { "PrecursorMz", ""},
                { "Charge", ""},
                { "Score", ""},
                { "QValue", "0.01" },
                { "RawFile", "" }
            };
            _qValueFilterChecked = true;
            Data = data;
            FilteredData = data;
            _taskService.Enqueue(FilterData);
        }

        public void AddFilter(string filterName, string value)
        {
            if (!_filters.ContainsKey(filterName))
                _filters.Add(filterName, value);
            else _filters[filterName] = value;
            _taskService.Enqueue(FilterData);
        }

        public void ClearFilters()
        {
            _sequenceFilterChecked = false; RaisePropertyChanged("SequenceFilterChecked");
            _proteinFilterChecked = false; RaisePropertyChanged("ProteinFilterChecked");
            _precursorMzFilterChecked = false; RaisePropertyChanged("PrecursorMzFilterChecked");
            _chargeFilterChecked = false; RaisePropertyChanged("ChargeFilterChecked");
            _scoreFilterChecked = false; RaisePropertyChanged("ScoreFilterChecked");
            _qValueFilterChecked = false; RaisePropertyChanged("QValueFilterChecked");
            _rawFileFilterChecked = false; RaisePropertyChanged("RawFileFilterChecked");
            _hideUnidentifiedScans = false; RaisePropertyChanged("HideUnidentifiedScans");
            _filters.Clear();
            _taskService.Enqueue(FilterData);
        }

        public List<PrSm> Data
        {
            get { return _data; }
            set
            {
                _data = value;
                _taskService.Enqueue(FilterData);
            }
        }

        public List<PrSm> FilteredData
        {
            get { return _filteredData; }
            private set
            {
                _filteredData = value;
                RaisePropertyChanged();
            }
        } 

        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set
            {
                _selectedPrSm = value;
                SelectedPrSmViewModel.Instance.PrSm = _selectedPrSm;
            }
        }

        #region FilterProperties

        public bool HideUnidentifiedScans
        {
            get { return _hideUnidentifiedScans; }
            set
            {
                _hideUnidentifiedScans = value;
                _taskService.Enqueue(FilterData);
                RaisePropertyChanged();
            }
        }

        public bool SequenceFilterChecked
        {
            get { return _sequenceFilterChecked; }
            set
            {
                _sequenceFilterChecked = value;
                if (_sequenceFilterChecked)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("Sequence", out defaultValue);
                    var sequences = (from prsm in Data where prsm.SequenceText.Length > 0 select prsm.SequenceText).Distinct().ToList();
                    var filterBoxVm = new FilterViewModel("Filter by Sequence", "Enter Sequence to filter by:", defaultValue, sequences, o=>true, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("Sequence", filterBoxVm.SelectedValue);
                        _previousFilters["Sequence"] = filterBoxVm.SelectedValue;
                    }
                    else _sequenceFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("Sequence")) _filters.Remove("Sequence");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }

        public bool ProteinFilterChecked
        {
            get { return _proteinFilterChecked; }
            set
            {
                _proteinFilterChecked = value;
                if (_proteinFilterChecked)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("Protein", out defaultValue);
                    var proteins = (from prsm in Data where prsm.ProteinName.Length > 0 select prsm.ProteinName).Distinct().ToList();
                    var filterBoxVm = new FilterViewModel("Filter by Protein", "Enter Sequence to filter by:", defaultValue, proteins, o=>true, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("Protein", filterBoxVm.SelectedValue);
                        _previousFilters["Protein"] = filterBoxVm.SelectedValue;
                    }
                    else _proteinFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("Protein")) _filters.Remove("Protein");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }

        public bool PrecursorMzFilterChecked
        {
            get { return _precursorMzFilterChecked; }
            set
            {
                _precursorMzFilterChecked = value;
                if (_precursorMzFilterChecked)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("PrecursorMz", out defaultValue);
                    FilterViewModel.Validate validator = o =>
                    {
                        double conv;
                        var str = o as string;
                        return Double.TryParse(str, out conv);
                    };
                    var filterBoxVm = new FilterViewModel("Filter by Precursor M/Z", "Enter minimum Precursor M/Z to display:", defaultValue, new List<string>(), validator, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("PrecursorMz", filterBoxVm.SelectedValue);
                        _previousFilters["PrecursorMz"] = filterBoxVm.SelectedValue;
                    }
                    else _precursorMzFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("PrecursorMz")) _filters.Remove("PrecursorMz");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }

        public bool ChargeFilterChecked
        {
            get { return _chargeFilterChecked; }
            set
            {
                _chargeFilterChecked = value;
                if (_chargeFilterChecked)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("Charge", out defaultValue);
                    FilterViewModel.Validate validator = o =>
                    {
                        int conv;
                        var str = o as string;
                        return Int32.TryParse(str, out conv);
                    };
                    var charges = (from prsm in Data where prsm.Charge > 0 select prsm.Charge.ToString(CultureInfo.InvariantCulture)).Distinct().ToList();
                    var filterBoxVm = new FilterViewModel("Filter by Charge", "Enter Charge to filter by:", defaultValue, charges, validator, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("Charge", filterBoxVm.SelectedValue);
                        _previousFilters["Charge"] = filterBoxVm.SelectedValue;
                    }
                    else _chargeFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("Charge")) _filters.Remove("Charge");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }

        public bool ScoreFilterChecked
        {
            get { return _scoreFilterChecked; }
            set
            {
                _scoreFilterChecked = value;
                if (_scoreFilterChecked)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("Score", out defaultValue);
                    FilterViewModel.Validate validator = o =>
                    {
                        double conv;
                        var str = o as string;
                        return Double.TryParse(str, out conv);
                    };
                    var filterBoxVm = new FilterViewModel("Filter by Score", "Enter minimum score to display:", defaultValue, new List<string>(), validator, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("Score", filterBoxVm.SelectedValue);
                        _previousFilters["Score"] = filterBoxVm.SelectedValue;
                    }
                    else _scoreFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("Score")) _filters.Remove("Score");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }

        public bool QValueFilterChecked
        {
            get { return _qValueFilterChecked; }
            set
            {
                _qValueFilterChecked = value;
                if (_qValueFilterChecked)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("QValue", out defaultValue);
                    FilterViewModel.Validate validator = o =>
                    {
                        double conv;
                        var str = o as string;
                        return Double.TryParse(str, out conv);
                    };
                    var filterBoxVm = new FilterViewModel("Filter by QValue", "Enter minimum QValue to display:", defaultValue, new List<string>(), validator, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("QValue", filterBoxVm.SelectedValue);
                        _previousFilters["QValue"] = filterBoxVm.SelectedValue;
                    }
                    else _qValueFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("QValue")) _filters.Remove("QValue");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }

        public bool RawFileFilterChecked
        {
            get { return _rawFileFilterChecked; }
            set
            {
                _rawFileFilterChecked = value;
                if (_rawFileFilterChecked)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("RawFile", out defaultValue);
                    var rawFiles = (from prsm in Data where prsm.RawFileName.Length > 0 select prsm.RawFileName).Distinct().ToList();
                    var filterBoxVm = new FilterViewModel("Filter by Sequence", "Enter Sequence to filter by:", defaultValue, rawFiles, o => true, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("RawFile", filterBoxVm.SelectedValue);
                        _previousFilters["RawFile"] = filterBoxVm.SelectedValue;
                    }
                    else _rawFileFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("RawFile")) _filters.Remove("RawFile");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }
        #endregion

        private void FilterData()
        {
            var filtered = Data;
            foreach (var filter in _filters)
            {
                switch (filter.Key)
                {
                    case "Sequence":
                        filtered = filtered.Where(datum => datum.SequenceText.StartsWith(filter.Value)).ToList();
                        break;
                    case "Protein":
                        filtered = filtered.Where(datum => datum.ProteinName.StartsWith(filter.Value)).ToList();
                        break;
                    case "PrecursorMz":
                        filtered = filtered.Where(datum => datum.PrecursorMz >= Convert.ToDouble(filter.Value)).ToList();
                        break;
                    case "Charge":
                        filtered = filtered.Where(datum => datum.Charge == Convert.ToInt32(filter.Value)).ToList();
                        break;
                    case "Score":
                        var filVal = Convert.ToDouble(filter.Value);
                        filtered = filtered.Where(datum => ((datum.UseGolfScoring && datum.Score <= filVal)
                                                         || (!datum.UseGolfScoring && datum.Score >= filVal))).ToList();
                        break;
                    case "QValue":
                        filtered = filtered.Where(datum => datum.QValue <= Convert.ToDouble(filter.Value)).ToList();
                        break;
                    case "RawFile":
                        filtered = filtered.Where(datum => datum.RawFileName.StartsWith(filter.Value)).ToList();
                        break;
                }
            }
            if (HideUnidentifiedScans) filtered = filtered.Where(datum => !datum.Score.Equals(Double.NaN)).ToList();
            FilteredData = filtered;
        }

        private readonly IMainDialogService _dialogService;
        private readonly ITaskService _taskService;
        private readonly Dictionary<string, string> _filters;
        private readonly Dictionary<string, string> _previousFilters; 
        private bool _sequenceFilterChecked;
        private bool _proteinFilterChecked;
        private List<PrSm> _data;
        private PrSm _selectedPrSm;
        private List<PrSm> _filteredData;
        private bool _precursorMzFilterChecked;
        private bool _chargeFilterChecked;
        private bool _scoreFilterChecked;
        private bool _qValueFilterChecked;
        private bool _rawFileFilterChecked;
        private bool _hideUnidentifiedScans;
    }
}
