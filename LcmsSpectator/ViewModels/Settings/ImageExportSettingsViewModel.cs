namespace LcmsSpectator.ViewModels.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using LcmsSpectator.Config;
    using ReactiveUI;
    
    public class ImageExportSettingsViewModel : ReactiveObject
    {
        /// <summary>
        /// The DPI resolution for image exporting.
        /// </summary>
        private int exportImageDpi;

        public ImageExportSettingsViewModel(ImageExportSettings imageExportSettings)
        {
            this.ExportImageDpi = imageExportSettings.ExportImageDpi;
        }

        /// <summary>
        /// Gets the ImageExportSettings for this view model.
        /// </summary>
        public ImageExportSettings ImageExportSettings
        {
            get
            {
                return new ImageExportSettings
                {
                    ExportImageDpi = this.ExportImageDpi
                };
            }
        }

        /// <summary>
        /// Gets or sets the DPI resolution for image exporting.
        /// </summary>
        public int ExportImageDpi
        {
            get { return this.exportImageDpi; }
            set { this.RaiseAndSetIfChanged(ref this.exportImageDpi, value); }
        } 
    }
}
