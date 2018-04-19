// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IonColorDictionary.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   This class maps ions to their proper OxyPlot coloring.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using InformedProteomics.Backend.Data.Spectrometry;
using LcmsSpectator.ViewModels.Data;
using OxyPlot;

namespace LcmsSpectator.PlotModels.ColorDictionaries
{
    /// <summary>
    /// This class maps ions to their proper OxyPlot coloring.
    /// </summary>
    public class IonColorDictionary
    {
        /// <summary>
        /// Dictionary that maps base ion type names to colors for each charge state.
        /// </summary>
        private Dictionary<string, IList<OxyColor>> fragmentColors;

        /// <summary>
        /// Dictionary that maps precursor ion indices to colors.
        /// </summary>
        private Dictionary<int, OxyColor> precursorColors;

        /// <summary>
        /// Initializes a new instance of the <see cref="IonColorDictionary"/> class.
        /// </summary>
        /// <param name="length">Maximum number of colors per base ion type.</param>
        public IonColorDictionary(int length)
        {
            BuildColorDictionary(length);
        }

        /// <summary>
        /// Get a color of fragment ion.
        /// </summary>
        /// <param name="label">LabeledIonViewModel for fragment ion.</param>
        /// <returns>Color of fragment ion.</returns>
        public OxyColor GetColor(LabeledIonViewModel label)
        {
            OxyColor color;
            if (label.IsFragmentIon)
            {
                var index = Math.Min(label.IonType.Charge - 1, fragmentColors[label.IonType.BaseIonType.Symbol].Count - 1);
                color = fragmentColors[label.IonType.BaseIonType.Symbol][index];
            }
            else
            {
                if (precursorColors.ContainsKey(label.Index))
                {
                    color = precursorColors[label.Index];
                }
                else
                {
                    color = label.Index > 0 ? OxyColors.DarkRed : OxyColors.AliceBlue;   
                }
            }

            return color;
        }

        /// <summary>
        /// Get a color of fragment ion.
        /// </summary>
        /// <param name="baseIonType">Base ion type of fragment ion.</param>
        /// <param name="charge">Charge of fragment ion.</param>
        /// <returns>Color of fragment ion.</returns>
        public OxyColor GetColor(BaseIonType baseIonType, int charge)
        {
            var index = Math.Min(charge - 1, fragmentColors[baseIonType.Symbol].Count - 1);
            return fragmentColors[baseIonType.Symbol][index];
        }

        /// <summary>
        /// Get a color of a precursor ion.
        /// </summary>
        /// <param name="precursorIndex">Index of precursor ion.</param>
        /// <returns>Color associated with a precursor ion of the given index.</returns>
        public OxyColor GetColor(int precursorIndex)
        {
            OxyColor color;
            if (precursorColors.ContainsKey(precursorIndex))
            {
                color = precursorColors[precursorIndex];
            }
            else
            {
                color = precursorIndex > 0 ? OxyColors.DarkRed : OxyColors.AliceBlue;
            }

            return color;
        }

        /// <summary>
        /// Build new color dictionary.
        /// </summary>
        /// <param name="length">Maximum number of colors for each base ion type.</param>
        public void BuildColorDictionary(int length)
        {
            const byte ColorMin = 150;
            const byte ColorMax = 255;

            fragmentColors = new Dictionary<string, IList<OxyColor>>();

            var ionAStart = OxyColor.FromRgb(0, ColorMin, 0);
            var ionAEnd = OxyColor.FromRgb(0, ColorMax, 0);
            var ionAColors = OxyPalette.Interpolate(length, ionAEnd, ionAStart);
            fragmentColors.Add(BaseIonType.A.Symbol, ionAColors.Colors);
            fragmentColors.Add(BaseIonType.A.GetDeconvolutedIon().Symbol, ionAColors.Colors);
            fragmentColors.Add(BaseIonType.Ar.Symbol, ionAColors.Colors);
            fragmentColors.Add(BaseIonType.Ar.GetDeconvolutedIon().Symbol, ionAColors.Colors);

            var ionBStart = OxyColor.FromRgb(0, 0, ColorMin);
            var ionBEnd = OxyColor.FromRgb(0, 0, ColorMax);
            var ionBColors = OxyPalette.Interpolate(length, ionBEnd, ionBStart);
            fragmentColors.Add(BaseIonType.B.Symbol, ionBColors.Colors);
            fragmentColors.Add(BaseIonType.B.GetDeconvolutedIon().Symbol, ionBColors.Colors);

            var ionCStart = OxyColor.FromRgb(0, ColorMin, ColorMin);
            var ionCEnd = OxyColor.FromRgb(0, ColorMax, ColorMax);
            var ionCColors = OxyPalette.Interpolate(length, ionCEnd, ionCStart);
            fragmentColors.Add(BaseIonType.C.Symbol, ionCColors.Colors);
            fragmentColors.Add(BaseIonType.C.GetDeconvolutedIon().Symbol, ionCColors.Colors);

            var ionDStart = OxyColor.FromRgb(255, ColorMin / 2, 8);
            var ionDEnd = OxyColor.FromRgb(255, ColorMin, 0);
            var ionDColors = OxyPalette.Interpolate(length, ionDEnd, ionDStart);
            fragmentColors.Add(BaseIonType.D.Symbol, ionDColors.Colors);
            fragmentColors.Add(BaseIonType.D.GetDeconvolutedIon().Symbol, ionDColors.Colors);

            var ionVStart = OxyColor.FromRgb(210, 105, 30);
            var ionVEnd = OxyColor.FromRgb(139, 69, 19);
            var ionVColors = OxyPalette.Interpolate(length, ionVEnd, ionVStart);
            fragmentColors.Add(BaseIonType.V.Symbol, ionVColors.Colors);
            fragmentColors.Add(BaseIonType.V.GetDeconvolutedIon().Symbol, ionVColors.Colors);

            var ionWStart = OxyColor.FromRgb(220, 220, 220);
            var ionWEnd = OxyColor.FromRgb(169, 169, 169);
            var ionWColors = OxyPalette.Interpolate(length, ionWEnd, ionWStart);
            fragmentColors.Add(BaseIonType.W.Symbol, ionWColors.Colors);
            fragmentColors.Add(BaseIonType.W.GetDeconvolutedIon().Symbol, ionWColors.Colors);

            var ionXStart = OxyColors.OliveDrab; //OxyColors.Yellow; //OxyColor.FromRgb(ColorMin, ColorMin, 0);
            var ionXEnd = OxyColors.DarkOliveGreen; //OxyColors.Goldenrod; //OxyColor.FromRgb(ColorMax, ColorMax, 0);
            var ionXColors = OxyPalette.Interpolate(length, ionXEnd, ionXStart);
            fragmentColors.Add(BaseIonType.X.Symbol, ionXColors.Colors);
            fragmentColors.Add(BaseIonType.X.GetDeconvolutedIon().Symbol, ionXColors.Colors);
            fragmentColors.Add(BaseIonType.Xr.Symbol, ionXColors.Colors);
            fragmentColors.Add(BaseIonType.Xr.GetDeconvolutedIon().Symbol, ionXColors.Colors);

            var ionYStart = OxyColor.FromRgb(ColorMin, 0, 0);
            var ionYEnd = OxyColor.FromRgb(ColorMax, 0, 0);
            var ionYColors = OxyPalette.Interpolate(length, ionYEnd, ionYStart);
            fragmentColors.Add(BaseIonType.Y.Symbol, ionYColors.Colors);
            fragmentColors.Add(BaseIonType.Y.GetDeconvolutedIon().Symbol, ionYColors.Colors);

            var ionYM1Start = OxyColor.FromRgb(ColorMin, 0, 0);
            var ionYM1End = OxyColor.FromRgb(ColorMax, 0, 0);
            var ionYM1Colors = OxyPalette.Interpolate(length, ionYM1End, ionYM1Start);
            fragmentColors.Add(BaseIonType.YM1.Symbol, ionYM1Colors.Colors);
            fragmentColors.Add(BaseIonType.YM1.GetDeconvolutedIon().Symbol, ionYM1Colors.Colors);

            var ionZStart = OxyColor.FromRgb(ColorMin, 0, ColorMin);
            var ionZEnd = OxyColor.FromRgb(ColorMax, 0, ColorMax);
            var ionZColors = OxyPalette.Interpolate(length, ionZEnd, ionZStart);
            fragmentColors.Add(BaseIonType.Z.Symbol, ionZColors.Colors);
            fragmentColors.Add(BaseIonType.Z.GetDeconvolutedIon().Symbol, ionZColors.Colors);
            fragmentColors.Add(BaseIonType.Zr.Symbol, ionZColors.Colors);
            fragmentColors.Add(BaseIonType.Zr.GetDeconvolutedIon().Symbol, ionZColors.Colors);

            precursorColors = new Dictionary<int, OxyColor>
            {
                { -1, OxyColors.DarkGray },
                { 0, OxyColors.Purple },
                { 1, OxyColors.Red },
                { 2, OxyColors.Blue },
                { 3, OxyColors.Green },
                { 4, OxyColors.Gold },
                { 5, OxyColors.Brown },
                { 6, OxyColors.Orange },
                { 7, OxyColors.PaleGreen },
                { 8, OxyColors.Turquoise },
                { 9, OxyColors.Olive },
                { 10, OxyColors.Beige },
                { 11, OxyColors.Lime },
                { 12, OxyColors.Salmon },
                { 13, OxyColors.MintCream },
                { 14, OxyColors.SteelBlue },
                { 15, OxyColors.Violet },
                { 16, OxyColors.Blue },
                { 17, OxyColors.Navy },
                { 18, OxyColors.Red },
                { 19, OxyColors.SpringGreen },
                { 20, OxyColors.Gold },
                { 21, OxyColors.DarkOrange }
            };
        }
    }
}
