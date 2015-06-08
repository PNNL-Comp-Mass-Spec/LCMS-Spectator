// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorListViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for list of ColorViewModels.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reactive.Linq;
    using System.Windows.Media;
    using OxyPlot;
    using ReactiveUI;

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
            this.ColorViewModels = new ReactiveList<ColorViewModel> { ChangeTrackingEnabled = true };
            this.Colors = this.ColorViewModels.CreateDerivedCollection(colorViewModel => colorViewModel.SelectedColor);
            this.ColorPalettes = new ReactiveList<ColorPaletteViewModel> { ChangeTrackingEnabled = true };

            this.ColorPalettes.Add(new ColorPaletteViewModel
            {
                Title = "Standard",
                Colors = new[]
                {
                    System.Windows.Media.Colors.Black, System.Windows.Media.Colors.Blue,
                    System.Windows.Media.Colors.Orange, System.Windows.Media.Colors.Red
                }
            });
            this.ColorPalettes.Add(new ColorPaletteViewModel
            {
                Title = "Blue-Red",
                Colors = new[]
                {
                    System.Windows.Media.Colors.Blue,
                    System.Windows.Media.Colors.Red
                }
            });
            this.ColorPalettes.Add(new ColorPaletteViewModel
            {
                Title = "Single Color (Red)",
                Colors = new[]
                {
                    new Color { A = 255, R = 255, G = 200, B = 200 }, 
                    System.Windows.Media.Colors.Red
                }
            });
            this.ColorPalettes.Add(new ColorPaletteViewModel
            {
                Title = "Single Color (Green)",
                Colors = new[]
                                            {
                                                new Color { A = 255, R = 200, G = 255, B = 200 }, 
                                                System.Windows.Media.Colors.Green
                                            }
            });
            this.ColorPalettes.Add(new ColorPaletteViewModel
            {
                Title = "Single Color (Blue)",
                Colors = new[]
                                            {
                                                new Color { A = 255, R = 200, G = 200, B = 255 }, 
                                                System.Windows.Media.Colors.Blue
                                            }
            });

            this.ColorViewModels.ItemChanged.Where(x => x.PropertyName == "IsRemoveRequested")
                .Select(x => x.Sender)
                .Where(sender => sender.IsRemoveRequested)
                .Subscribe(color => this.ColorViewModels.Remove(color));

            this.ColorPalettes.ItemChanged.Where(x => x.PropertyName == "IsSelected")
                .Select(x => x.Sender)
                .Where(sender => sender.IsSelected)
                .Subscribe(
                    palette =>
                        {
                            this.SetColors(palette.Colors);
                            palette.IsSelected = false;
                        });

            var addColorCommand = ReactiveCommand.Create();
            addColorCommand.Subscribe(_ => this.ColorViewModels.Add(new ColorViewModel()));
            this.AddColorCommand = addColorCommand;
        }

        /// <summary>
        /// Gets a list of selected colors.
        /// </summary>
        public ReactiveList<ColorViewModel> ColorViewModels { get; private set; }

        /// <summary>
        /// Gets a list of color palettes.
        /// </summary>
        public ReactiveList<ColorPaletteViewModel> ColorPalettes { get; private set; }

        /// <summary>
        /// Gets a derived collection of the actual colors in the Color view models.
        /// </summary>
        public IReactiveDerivedList<Color> Colors { get; private set; }  

        /// <summary>
        /// Gets a command that adds a color to the color list
        /// </summary>
        public IReactiveCommand AddColorCommand { get; private set; }

        /// <summary>
        /// Get raw colors as OxyPlot OxyColors.
        /// </summary>
        /// <returns>Array of OxyColors.</returns>
        public OxyColor[] GetOxyColors()
        {
            return this.ColorViewModels.Select(colorViewModel => colorViewModel.SelectedOxyColor).ToArray();
        }

        /// <summary>
        /// Set the colors.
        /// </summary>
        /// <param name="colors">Colors to set.</param>
        private void SetColors(IEnumerable<Color> colors)
        {
            this.ColorViewModels.Clear();
            foreach (var color in colors.Select(color => new ColorViewModel { SelectedColor = color }))
            {
                this.ColorViewModels.Add(color);
            }
        }
    }
}
