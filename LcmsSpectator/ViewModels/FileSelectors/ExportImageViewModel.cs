// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExportImageViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for selecting dimensions and resolution of an exported image.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using LcmsSpectator.DialogServices;
using OxyPlot;
using OxyPlot.Wpf;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.FileSelectors
{
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
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public ExportImageViewModel()
        {
        }

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
            Height = height;
            Width = width;
            Dpi = dpi;

            ExportCommand = ReactiveCommand.CreateCombined(new [] { SuccessCommand },
                                    this.WhenAnyValue(x => x.FilePath, x => x.Height, x => x.Width, x => x.Dpi)
                                        .Select(
                                            x => !string.IsNullOrWhiteSpace(x.Item1) &&
                                            x.Item2 >= 0 && x.Item3 >= 0 && x.Item4 >= 0));

            BrowseFilesCommand = ReactiveCommand.Create(() => FilePath = this.dialogService.SaveFile(".png", "Png Files (*.png)|*.png"));

            SuccessCommand.Where(_ => plotModel != null).Subscribe(_ => ExportPlotModel(plotModel));
        }

        /// <summary>
        /// Gets or sets the file path to export to.
        /// </summary>
        public string FilePath
        {
            get => filePath;
            set => this.RaiseAndSetIfChanged(ref filePath, value);
        }

        /// <summary>
        /// Gets or sets the height of the exported image.
        /// </summary>
        public int Height
        {
            get => height;
            set => this.RaiseAndSetIfChanged(ref height, value);
        }

        /// <summary>
        /// Gets or sets the width of the exported image.
        /// </summary>
        public int Width
        {
            get => width;
            set => this.RaiseAndSetIfChanged(ref width, value);
        }

        /// <summary>
        /// Gets or sets the DPI of the exported image.
        /// </summary>
        public int Dpi
        {
            get => dpi;
            set => this.RaiseAndSetIfChanged(ref dpi, value);
        }

        /// <summary>
        /// Gets a command that browses image file paths.
        /// </summary>
        public ReactiveCommand<Unit, string> BrowseFilesCommand { get; }

        /// <summary>
        /// Gets a command that triggers the export.
        /// </summary>
        public CombinedReactiveCommand<Unit, Unit> ExportCommand { get; }

        /// <summary>
        /// Exports a plot model to an image with the given settings.
        /// </summary>
        /// <param name="pm">The plot model to export.</param>
        public void ExportPlotModel(PlotModel pm)
        {
            // OxyPlot 2.0 syntax
            //PngExporter.Export(
            //                pm,
            //                FilePath,
            //                Width,
            //                Height,
            //                OxyColors.White,
            //                Dpi);

            // OxyPlot 2.1 syntax
            pm.Background = OxyColors.White;

            var exporter = new PngExporter
            {
                Width = width,
                Height = height,
                Resolution = dpi
            };

            exporter.Export(pm, new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read));
        }

        /// <summary>
        /// Gets an observable that determines whether or not  the Success command is executable.
        /// </summary>
        protected override IObservable<bool> CanSucceed
        {
            get { return this.WhenAnyValue(x => x.FilePath).Select(_ => Validate()); }
        }

        /// <summary>
        /// Function that checks whether or not the input to this window is valid.
        /// </summary>
        /// <returns>A value indicating whether the input to this window is valid.</returns>
        protected override bool Validate()
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                dialogService.MessageBox("Invalid file path.");
                return false;
            }

            return true;
        }
    }
}
