using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using LcmsSpectator.DialogServices;
using LcmsSpectator.TaskServices;
using LcmsSpectatorModels.Models;

namespace LcmsSpectator.ViewModels
{
    public class ScanViewModel: ViewModelBase
    {
        public RelayCommand ClearFiltersCommand { get; private set; }
        public ScanViewModel(IMainDialogService dialogService, ITaskService taskService, List<PrSm> data, IMessenger messenger)
        {
            MessengerInstance = messenger;
            ClearFiltersCommand = new RelayCommand(ClearFilters);
            _taskService = taskService;
            _dialogService = dialogService;
            _filters = new Dictionary<string, string>();
            _previousFilters = new Dictionary<string, string>
            {
                {"Sequence", ""},
                {"Protein", ""},
                {"Mass", ""},
                {"PrecursorMz", ""},
                {"Charge", ""},
                {"Score", ""},
                {"QValue", "0.01"},
                {"RawFile", ""}
            };
            _actualIds = false;
            _firstActualIds = false;
            _data = data;
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
            _precursorMzFilterChecked = false; RaisePropertyChanged("MassFilterChecked");
            _precursorMzFilterChecked = false; RaisePropertyChanged("PrecursorMzFilterChecked");
            _chargeFilterChecked = false; RaisePropertyChanged("ChargeFilterChecked");
            _scoreFilterChecked = false; RaisePropertyChanged("ScoreFilterChecked");
            _qValueFilterChecked = false; RaisePropertyChanged("QValueFilterChecked");
            _rawFileFilterChecked = false; RaisePropertyChanged("RawFileFilterChecked");
            _hideUnidentifiedScans = false; RaisePropertyChanged("HideUnidentifiedScans");
            _filters.Clear();
            _taskService.Enqueue(FilterData);
        }

        /// <summary>
        /// Remove PrSms from IDTree that are associated with raw file
        /// </summary>
        /// <param name="rawFileName">Name of raw file</param>
        public void RemovePrSmsFromRawFile(string rawFileName)
        {
            var newData = _data.Where(prsm => prsm.RawFileName != rawFileName).ToList();
            var max = (newData.Count > 0) ? newData[0] : null;
            foreach (var item in newData) if (item.CompareTo(max) > 0) max = item;
            SelectedPrSm = max;
            _data = newData;
            _taskService.Enqueue(FilterData);
        }

        public List<PrSm> Data
        {
            get { return new List<PrSm>(_data); }
        }

        public List<PrSm> FilteredData
        {
            get { return _filteredData; }
            private set
            {
                var oldData = _filteredData;
                _filteredData = value;
                RaisePropertyChanged("FilteredData", oldData, _filteredData, true);
            }
        }

        public List<ProteinId> FilteredProteins
        {
            get { return _filteredProteins; }
            private set
            {
                _filteredProteins = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Object selected in Treeview. Uses weak typing because each level TreeView is a different data type.
        /// </summary>
        public object TreeViewSelectedItem
        {
            get { return _treeViewSelectedItem; }
            set
            {
                if (value != null)
                {
                    _treeViewSelectedItem = value;
                    if (_treeViewSelectedItem is PrSm)
                    {
                        var selectedPrSm = _treeViewSelectedItem as PrSm;
                        SelectedPrSm = selectedPrSm;
                    }
                    else
                    {
                        var selected = (IIdData)_treeViewSelectedItem;
                        if (selected == null) return;
                        var highest = selected.GetHighestScoringPrSm();
                        SelectedPrSm = highest;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        public PrSm SelectedPrSm
        {
            get { return _selectedPrSm; }
            set
            {
                var oldValue = _selectedPrSm;
                _selectedPrSm = value;
                RaisePropertyChanged("SelectedPrSm", oldValue, _selectedPrSm, true);
            }
        }

        public void AddIds(IEnumerable<PrSm> ids)
        {
            var scanMap = _data.ToDictionary(prsm => prsm.Scan);
            foreach (var prsm in ids)
            {
                if (scanMap.ContainsKey(prsm.Scan))
                {
                    scanMap[prsm.Scan] = prsm;
                }
                else scanMap.Add(prsm.Scan, prsm); 
            }
            _data = scanMap.Values.ToList();
            UpdateActualIds();
            // QValue filter should be set to 0.01 by default for Ids.
            // Should show highest scoring id first time ids are added
            if (_actualIds && !_firstActualIds && _data.Count > 0)
            {
                _firstActualIds = false;
                _qValueFilterChecked = true; RaisePropertyChanged("QValueFilterChecked");
                if (!_filters.ContainsKey("QValue")) _filters.Add("QValue", "0.01");

                var highestScoringPrsm = _data[0];
                foreach (var prsm in _data)
                {
                    if (prsm.CompareTo(highestScoringPrsm) >= 0) highestScoringPrsm = prsm;
                }
                SelectedPrSm = highestScoringPrsm;
            }
            _taskService.Enqueue(FilterData);
        }

        public void ClearIds()
        {
            _data = new List<PrSm>();
            UpdateActualIds();
            _taskService.Enqueue(FilterData);
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
            get { return _sequenceFilterChecked && _actualIds; }
            set
            {
                _sequenceFilterChecked = value;
                if (_sequenceFilterChecked && _actualIds)
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
            get { return _proteinFilterChecked && _actualIds; }
            set
            {
                _proteinFilterChecked = value;
                if (_proteinFilterChecked && _actualIds)
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

        public bool MassFilterChecked
        {
            get { return _massFilterChecked && _actualIds; }
            set
            {
                _massFilterChecked = value;
                if (_massFilterChecked && _actualIds)
                {
                    string defaultValue;
                    _previousFilters.TryGetValue("Mass", out defaultValue);
                    FilterViewModel.Validate validator = o =>
                    {
                        double conv;
                        var str = o as string;
                        return Double.TryParse(str, out conv);
                    };
                    var filterBoxVm = new FilterViewModel("Filter by Mass", "Enter minimum Mass to display:", defaultValue, new List<string>(), validator, _dialogService);
                    if (_dialogService.FilterBox(filterBoxVm))
                    {
                        AddFilter("Mass", filterBoxVm.SelectedValue);
                        _previousFilters["Mass"] = filterBoxVm.SelectedValue;
                    }
                    else _precursorMzFilterChecked = false;
                }
                else
                {
                    if (_filters.ContainsKey("Mass")) _filters.Remove("Mass");
                    _taskService.Enqueue(FilterData);
                }
                RaisePropertyChanged();
            }
        }

        public bool PrecursorMzFilterChecked
        {
            get { return _precursorMzFilterChecked && _actualIds; }
            set
            {
                _precursorMzFilterChecked = value;
                if (_precursorMzFilterChecked && _actualIds)
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
            get { return _chargeFilterChecked && _actualIds; }
            set
            {
                _chargeFilterChecked = value;
                if (_chargeFilterChecked && _actualIds)
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
                    charges.Sort();
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
            get { return _scoreFilterChecked && _actualIds; }
            set
            {
                _scoreFilterChecked = value;
                if (_scoreFilterChecked && _actualIds)
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
            get { return _qValueFilterChecked && _actualIds; }
            set
            {
                _qValueFilterChecked = value;
                if (_qValueFilterChecked && _actualIds)
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
            var filtered = _data;
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
                    case "Mass":
                        filtered = filtered.Where(datum => datum.Mass >= Convert.ToDouble(filter.Value)).ToList();
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
            var filteredIds = new IdentificationTree(FilteredData);
            FilteredProteins = filteredIds.ProteinIds.ToList();
        }

        private void UpdateActualIds()
        {
            _actualIds = _data.Any(id => id.Sequence.Count > 0);
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
        private List<ProteinId> _filteredProteins;
        private bool _massFilterChecked;
        private bool _precursorMzFilterChecked;
        private bool _chargeFilterChecked;
        private bool _scoreFilterChecked;
        private bool _qValueFilterChecked;
        private bool _rawFileFilterChecked;
        private bool _hideUnidentifiedScans;
        private bool _actualIds;
        private bool _firstActualIds;
        private object _treeViewSelectedItem;
    }
}
