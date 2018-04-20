// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorListViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for list of ColorViewModels.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Wpf;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    /// <summary>
    /// View model for list of ColorViewModels.
    /// </summary>
    public class ColorListViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorListViewModel"/> class.
        /// </summary>
        public ColorListViewModel()
        {
            ColorViewModels = new ReactiveList<ColorViewModel> { ChangeTrackingEnabled = true };
            Colors = ColorViewModels.CreateDerivedCollection(colorViewModel => colorViewModel.SelectedColor);
            ColorPalettes = new ReactiveList<ColorPaletteViewModel> { ChangeTrackingEnabled = true };

            ColorPalettes.Add(new ColorPaletteViewModel(new[]
                {
                    System.Windows.Media.Colors.Black, System.Windows.Media.Colors.Blue,
                    System.Windows.Media.Colors.Orange, System.Windows.Media.Colors.Red
                }) { Title = "Standard" });
            ColorPalettes.Add(new ColorPaletteViewModel(new[]
                {
                    System.Windows.Media.Colors.Blue,
                    System.Windows.Media.Colors.Red
                }) { Title = "Blue-Red" });
            ColorPalettes.Add(new ColorPaletteViewModel(new[]
                {
                    new Color { A = 255, R = 255, G = 200, B = 200 },
                    System.Windows.Media.Colors.Red
                }) { Title = "Reds" });
            ColorPalettes.Add(new ColorPaletteViewModel(new[]
                {
                    new Color { A = 255, R = 200, G = 255, B = 200 },
                    System.Windows.Media.Colors.Green
                }) { Title = "Greens" });
            ColorPalettes.Add(new ColorPaletteViewModel(new[]
                {
                    new Color { A = 255, R = 200, G = 200, B = 255 },
                    System.Windows.Media.Colors.Blue
                }) { Title = "Blues" });

            ColorViewModels.ItemChanged.Where(x => x.PropertyName == "IsRemoveRequested")
                .Select(x => x.Sender)
                .Where(sender => sender.IsRemoveRequested)
                .Subscribe(color => ColorViewModels.Remove(color));

            ColorPalettes.ItemChanged.Where(x => x.PropertyName == "IsSelected")
                .Select(x => x.Sender)
                .Where(sender => sender.IsSelected)
                .Subscribe(
                    palette =>
                        {
                            SetColors(palette.Colors);
                            palette.IsSelected = false;
                        });

            AddColorCommand = ReactiveCommand.Create(() => ColorViewModels.Add(new ColorViewModel()));
        }

        /// <summary>
        /// Gets a list of selected colors.
        /// </summary>
        public ReactiveList<ColorViewModel> ColorViewModels { get; }

        /// <summary>
        /// Gets a list of color palettes.
        /// </summary>
        public ReactiveList<ColorPaletteViewModel> ColorPalettes { get; }

        /// <summary>
        /// Gets a derived collection of the actual colors in the Color view models.
        /// </summary>
        public IReactiveDerivedList<Color> Colors { get; }

        /// <summary>
        /// Gets a command that adds a color to the color list
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddColorCommand { get; }

        /// <summary>
        /// Get raw colors as OxyPlot OxyColors.
        /// </summary>
        /// <returns>Array of OxyColors.</returns>
        public OxyColor[] GetOxyColors()
        {
            return ColorViewModels.Select(colorViewModel => colorViewModel.SelectedColor.ToOxyColor()).ToArray();
        }

        /// <summary>
        /// Set the colors.
        /// </summary>
        /// <param name="colors">Colors to set.</param>
        private void SetColors(IEnumerable<Color> colors)
        {
            ColorViewModels.Clear();
            foreach (var color in colors.Select(color => new ColorViewModel { SelectedColor = color }))
            {
                ColorViewModels.Add(color);
            }
        }
    }
}
