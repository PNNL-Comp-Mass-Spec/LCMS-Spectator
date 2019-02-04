using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using InformedProteomics.Backend.MassSpecData;
using LcmsSpectator.Readers;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Data
{
    public class PbfCreationViewModel : ReactiveObject
    {
        private readonly DatasetReaderWrapper wrapper = null;
        private bool isLoading = false;
        private double loadProgressPercent;
        private string loadProgressStatus;
        private readonly ObservableAsPropertyHelper<bool> showCreationInfo = null;

        /// <summary>
        /// Parameterless constructor for design-time data
        /// </summary>
        [Obsolete("WPF design-time use only", true)]
        public PbfCreationViewModel()
        { }

        public PbfCreationViewModel(ILcMsRun lcmsRun)
        {
            LcMs = lcmsRun;
            wrapper = lcmsRun as DatasetReaderWrapper;
            this.WhenAnyValue(x => x.wrapper, x => x.wrapper.IsXicDataAvailable).Subscribe(x => this.RaisePropertyChanged(nameof(XicInfoAvailable)));
            showCreationInfo = this.WhenAnyValue(x => x.XicInfoAvailable, x => x.IsLoading).Select(x => !x.Item1 && !x.Item2).ToProperty(this, x => x.ShowCreationInfo, true);

            CreatePbfCommand = ReactiveCommand.CreateFromTask(CreatePbf);
        }

        public ILcMsRun LcMs { get; }

        public bool XicInfoAvailable => wrapper?.IsXicDataAvailable ?? true;
        public bool ShowCreationInfo => showCreationInfo?.Value ?? true;

        /// <summary>
        /// Gets or sets a value indicating whether this dataset is loading.
        /// </summary>
        public bool IsLoading
        {
            get => isLoading;
            set => this.RaiseAndSetIfChanged(ref isLoading, value);
        }

        /// <summary>
        /// Gets or sets the progress of the loading.
        /// </summary>
        public double LoadProgressPercent
        {
            get => loadProgressPercent;
            set => this.RaiseAndSetIfChanged(ref loadProgressPercent, value);
        }

        /// <summary>
        /// Gets or sets the status message for the loading.
        /// </summary>
        public string LoadProgressStatus
        {
            get => loadProgressStatus;
            set => this.RaiseAndSetIfChanged(ref loadProgressStatus, value);
        }

        public ReactiveCommand<Unit, Unit> CreatePbfCommand { get; }

        private async Task CreatePbf()
        {
            if (wrapper == null)
            {
                return;
            }

            IsLoading = true; // Show animated loading screen
            LoadProgressPercent = 0.0;
            LoadProgressStatus = "Loading...";
            var progress = new Progress<PRISM.ProgressData>(progressData =>
            {
                progressData.UpdateFrequencySeconds = 2;
                if (progressData.ShouldUpdate())
                {
                    LoadProgressPercent = progressData.Percent;
                    LoadProgressStatus = progressData.Status;
                }
            });

            await Task.Delay(20).ConfigureAwait(false);
            await wrapper.GeneratePbfFileIfNeededAsync(progress).ConfigureAwait(false);

            IsLoading = false;
            this.RaisePropertyChanged(nameof(XicInfoAvailable));
        }
    }
}
