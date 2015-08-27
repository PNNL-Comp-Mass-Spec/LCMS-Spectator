namespace LcmsSpectator.Config
{
    public class ImageExportSettings
    {
        public ImageExportSettings()
        {
            this.ExportImageDpi = 96;
        }

        /// <summary>
        /// Gets or sets the DPI resolution for image exporting.
        /// </summary>
        public int ExportImageDpi { get; set; } 
    }
}
