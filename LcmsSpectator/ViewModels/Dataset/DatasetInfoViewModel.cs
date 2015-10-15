using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using LcmsSpectator.DialogServices;
using LcmsSpectator.Models.Dataset;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Dataset
{
    public class DatasetInfoViewModel : ReactiveObject
    {
        /// <summary>
        /// The name of the dataset.
        /// </summary>
        private string name;

        /// <summary>
        /// A value that indicates whether this dataset is ready to be closed.
        /// </summary>
        private bool readyToClose;

        public DatasetInfoViewModel(DatasetInfo datasetInfo = null, IDialogService dialogService = null)
        {
            dialogService = dialogService ?? new DialogService();
            this.Files = new ReactiveList<FileInfoViewModel>();

            this.ReadyToClose = false;

            this.RemoveDataset = ReactiveCommand.Create();
            this.RemoveDataset.Select(
                _ =>
                    dialogService.ConfirmationBox($"Are you sure that you would like to close {this.Name}?", "Close"))
                .Subscribe(response => this.ReadyToClose = response);

            if (datasetInfo != null)
            {
                this.Name = datasetInfo.Name;
                this.Files.AddRange(datasetInfo.Files.Select(f => new FileInfoViewModel(f)));
            }
        }

        /// <summary>
        /// Gets or sets the name of the dataset.
        /// </summary>
        public string Name
        {
            get { return this.name; }
            set { this.RaiseAndSetIfChanged(ref this.name, value); }
        }

        /// <summary>
        /// Gets the list of files that are part of this dataset.
        /// </summary>
        public ReactiveList<FileInfoViewModel> Files { get; }

        /// <summary>
        /// Gets or sets a value that indicates whether this dataset is ready to be closed.
        /// </summary>
        public bool ReadyToClose
        {
            get { return this.readyToClose; }
            private set { this.RaiseAndSetIfChanged(ref this.readyToClose, value); }
        }

        /// <summary>
        /// Gets a command that is triggered when this dataset should be removed.
        /// </summary>
        public ReactiveCommand<object> RemoveDataset { get; }

        /// <summary>
        /// Gets the <see cref="DatasetInfo" /> for this view model.
        /// </summary>
        public DatasetInfo DatasetInfo
        {
            get
            {
                return new DatasetInfo(this.Files.Where(fvm => fvm.Selected).Select(fvm => fvm.FileInfo));
            }
        }
    }
}
