// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProteinColorDictionary.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maps proteins to an OxyPlot coloring.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.PlotModels.ColorDicionaries
{
    using System;
    using System.Collections.Generic;
    using OxyPlot;

    /// <summary>
    /// This class maps proteins to an OxyPlot coloring.
    /// </summary>
    public class ProteinColorDictionary
    {
        /// <summary>
        /// The maximum number of colors in this color dictionary.
        /// </summary>
        public const int MaxColors = 10;

        /// <summary>
        /// The dictionary mapping protein names to color indices.
        /// </summary>
        private readonly Dictionary<string, int> colorDictionary;

        /// <summary>
        /// Current protein index.
        /// </summary>
        private int protIndex;

        /// <summary>
        /// Color index offset.
        /// </summary>
        private int offset;

        /// <summary>
        /// Color offset is multiplied by 1/multiplier.
        /// </summary>
        private int multiplier;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProteinColorDictionary"/> class.
        /// </summary>
        /// <param name="numColors">The maximum number of colors for OxyPalette.</param>
        public ProteinColorDictionary(int numColors = 5000)
        {
            this.colorDictionary = new Dictionary<string, int> { { string.Empty, 0 } };
            this.protIndex = 0;
            this.offset = 0;
            this.multiplier = 2;

            this.OxyPalette = OxyPalette.Interpolate(
                                numColors,
                                OxyColors.LightGreen,
                                OxyColors.LightBlue,
                                OxyColors.Turquoise,
                                OxyColors.Olive,
                                OxyColors.Brown,
                                OxyColors.Cyan,
                                OxyColors.Gray,
                                OxyColors.Pink,
                                OxyColors.LightSeaGreen,
                                OxyColors.Beige);
        }

        /// <summary>
        /// Gets the palette of OxyColors used to get colors from.
        /// </summary>
        public OxyPalette OxyPalette { get; private set; }

        /// <summary>
        /// Get an OxyColor from the palette for a specific protein.
        /// </summary>
        /// <param name="proteinName">The name of the protein.</param>
        /// <returns>The OxyColor for the given protein.</returns>
        public OxyColor GetColor(string proteinName)
        {
            return this.OxyPalette.Colors[this.GetColorCode(proteinName)];
        }

        /// <summary>
        /// Get a color code for the palette for a specific protein.
        /// </summary>
        /// <param name="proteinName">The name of the protein.</param>
        /// <returns>The index of the OxyColor from the OxyPalette for the given protein.</returns>
        public int GetColorCode(string proteinName)
        {
            if (!this.colorDictionary.ContainsKey(proteinName))
            {
                int colorIndex;
                do
                {   // do not select the same color as unid color.
                    var r = this.protIndex++ % MaxColors;
                    colorIndex = Math.Min((r * (this.OxyPalette.Colors.Count / MaxColors)) + this.offset, this.OxyPalette.Colors.Count - 1);
                }
                while (colorIndex == 0);

                this.colorDictionary.Add(proteinName, colorIndex);

                if (this.protIndex >= MaxColors)
                {
                    // When we've used up all the primary colors, use the colors midway in between
                    this.offset = OxyPalette.Colors.Count / (this.multiplier * MaxColors);
                    this.multiplier *= 2;
                }
            }

            return this.colorDictionary[proteinName];
        }
    }
}
