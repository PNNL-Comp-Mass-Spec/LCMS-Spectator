// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportImageViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting dimensions and resolution of an exported image.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels.FileSelectors
{
    using System;
    using System.Reactive.Linq;
    using LcmsSpectator.DialogServices;
    using OxyPlot;
    using OxyPlot.Wpf;
    using ReactiveUI;

    /// <summary>
    /// View model for selecting dimensions and resolution of an exported image.
    /// </summary>
    public class ExportImageViewModel : WindowViewModel
    {
        /// <summary>
        /// Dialog service for opening dialogs from view model.
        /// </summary>
        private readonly IDialogService dialogService;

        /// <summary>
        /// The plot model to export.
        /// </summary>
        private readonly PlotModel plotModel;

        /// <summary>
        /// The file path to export to.
        /// </summary>
        private string filePath;

        /// <summary>
        /// The height of the exported image.
        /// </summary>
        private int height;

        /// <summary>
        /// The width of the exported image.
        /// </summary>
        private int width;

        /// <summary>
        /// The DPI of the exported image.
        /// </summary>
        private int dpi;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExportImageViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from view model.</param>
        /// <param name="height">Default height of the image.</param>
        /// <param name="width">Default width of the image.</param>
        /// <param name="dpi">Default DPI of the image.</param>
        /// <param name="plotModel">Plot model to export</param>
        public ExportImageViewModel(IDialogService dialogService, int height = 0, int width = 0, int dpi = 0, PlotModel plotModel = null)
        {
            this.dialogService = dialogService;
            this.Height = height;
            this.Width = width;
            this.Dpi = dpi;
            this.plotModel = plotModel;

            var exportCommand = ReactiveCommand.Create(
                                    this.WhenAnyValue(x => x.FilePath, x => x.Height, x => x.Width, x => x.Dpi)
                                        .Select(
                                            x => !string.IsNullOrWhiteSpace(x.Item1) &&
                                            x.Item2 >= 0 && x.Item3 >= 0 && x.Item4 >= 0));
            exportCommand.Subscribe(_ => this.SuccessCommand.Execute(null));
            this.ExportCommand = exportCommand;

            var browseFilesCommand = ReactiveCommand.Create();
            browseFilesCommand.Select(_ => this.dialogService.SaveFile(".png", @"Png Files (*.png)|*.png"))
                .Subscribe(filePath => this.FilePath = filePath);
            this.BrowseFilesCommand = browseFilesCommand;

            this.SuccessCommand.Where(_ => this.plotModel != null).Subscribe(_ => this.ExportPlotModel(this.plotModel));
        }

        /// <summary>
        /// Gets or sets the file path to export to.
        /// </summary>
        public string FilePath
        {
            get { return this.filePath; }
            set { this.RaiseAndSetIfChanged(ref this.filePath, value); }
        }

        /// <summary>
        /// Gets or sets the height of the exported image.
        /// </summary>
        public int Height
        {
            get { return this.height; }
            set { this.RaiseAndSetIfChanged(ref this.height, value); }
        }

        /// <summary>
        /// Gets or sets the width of the exported image.
        /// </summary>
        public int Width
        {
            get { return this.width; }
            set { this.RaiseAndSetIfChanged(ref this.width, value); }
        }

        /// <summary>
        /// Gets or sets the DPI of the exported image.
        /// </summary>
        public int Dpi
        {
            get { return this.dpi; }
            set { this.RaiseAndSetIfChanged(ref this.dpi, value); }
        }

        /// <summary>
        /// Gets a command that browses image file paths.
        /// </summary>
        public IReactiveCommand BrowseFilesCommand { get; private set; }

        /// <summary>
        /// Gets a command that triggers the export.
        /// </summary>
        public IReactiveCommand ExportCommand { get; private set; }

        /// <summary>
        /// Exports a plot model to an image with the given settings.
        /// </summary>
        /// <param name="pm">The plot model to export.</param>
        public void ExportPlotModel(PlotModel pm)
        {
            PngExporter.Export(
                            pm,
                            this.FilePath,
                            this.Width,
                            this.Height,
                            OxyColors.White,
                            this.Dpi);
        }

        /// <summary>
        /// Gets an observable that determines whether or not  the Success command is executable.
        /// </summary>
        protected override IObservable<bool> CanSucceed
        {
            get { return this.WhenAnyValue(x => x.FilePath).Select(_ => this.Validate()); }
        }

        /// <summary>
        /// Function that checks whether or not the input to this window is valid.
        /// </summary>
        /// <returns>A value indicating whether the input to this window is valid.</returns>
        protected override bool Validate()
        {
            if (string.IsNullOrWhiteSpace(this.FilePath))
            {
                this.dialogService.MessageBox("Invalid file path.");
                return false;
            }

            return true;
        }
    }
}
