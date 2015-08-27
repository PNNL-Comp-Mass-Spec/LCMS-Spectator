using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LcmsSpectator.Models.Dataset;
using ReactiveUI;
using FileInfo = LcmsSpectator.Models.Dataset.FileInfo;

namespace LcmsSpectator.ViewModels.Dataset
{
    public class FileInfoViewModel : ReactiveObject
    {
        /// <summary>
        /// The path to the file.
        /// </summary>
        private string filePath;

        /// <summary>
        /// The type of the file.
        /// </summary>
        private FileTypes fileType;

        /// <summary>
        /// A value indicating whether this file is selected.
        /// </summary>
        private bool selected;

        public FileInfoViewModel(FileInfo fileInfo)
        {
            this.FilePath = fileInfo.FilePath;
            this.Selected = true;
        }

        /// <summary>
        /// Gets or sets the path to the file.
        /// </summary>
        public string FilePath
        {
            get { return this.filePath; }
            set { this.RaiseAndSetIfChanged(ref this.filePath, value); }
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string FileName
        {
            get { return Path.GetFileNameWithoutExtension(this.FilePath); }
        }

        /// <summary>
        /// Gets the type of the file.
        /// </summary>
        public FileTypes FileType
        {
            get { return this.fileType; }
            set { this.RaiseAndSetIfChanged(ref this.fileType, value); }
        }
       
        /// <summary>
        /// Gets or sets a value indicating whether this file is selected.
        /// </summary>
        public bool Selected
        {
            get { return this.selected; }
            set { this.RaiseAndSetIfChanged(ref this.selected, value); }
        }

        /// <summary>
        /// Gets the FileInfo model for this view model.
        /// </summary>
        public FileInfo FileInfo
        {
            get
            {
                return FileInfo.GetFileInfo(this.FilePath);
            }
        }
    }
}
