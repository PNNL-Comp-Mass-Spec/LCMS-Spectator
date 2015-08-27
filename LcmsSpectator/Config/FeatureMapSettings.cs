using System.Collections.Generic;
using System.Windows.Media;

namespace LcmsSpectator.Config
{
    public class FeatureMapSettings
    {
        public FeatureMapSettings()
        {
            this.FeatureColors = new List<Color> { Colors.Black, Colors.Blue, Colors.Orange, Colors.Red };
            this.IdColors = new List<Color>
            {
                Colors.LightGreen, Colors.LightBlue, Colors.Turquoise, Colors.Olive,
                Colors.Brown, Colors.Cyan, Colors.Gray, Colors.Pink, Colors.LightSeaGreen,
                Colors.Beige
            };

            this.Ms2ScanColor = Colors.OliveDrab;
        }

        /// <summary>
        /// Gets or sets the colors of features in FeatureMap.
        /// </summary>
        public List<Color> FeatureColors { get; set; }

        /// <summary>
        /// Gets or sets the ID points in FeatureMap.
        /// </summary>
        public List<Color> IdColors { get; set; }

        /// <summary>
        /// Gets or sets the MS/MS scans in FeatureMap.
        /// </summary>
        public Color Ms2ScanColor { get; set; }
    }
}
