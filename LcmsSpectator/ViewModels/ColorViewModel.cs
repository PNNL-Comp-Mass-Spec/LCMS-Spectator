// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for single color.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Windows.Media;
    using LcmsSpectator.DialogServices;
    using OxyPlot;
    using ReactiveUI;

    /// <summary>
    /// View model for single color.
    /// </summary>
    public class ColorViewModel : ReactiveObject
    {
        /// <summary>
        /// The selected color.
        /// </summary>
        private Color selectedColor;

        /// <summary>
        /// A value indicating whether this color view model should be removed.
        /// </summary>
        private bool isRemoveRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorViewModel"/> class.
        /// </summary>
        public ColorViewModel()
        {
            this.SelectedColor = new Color { A = 255, R = 0, G = 0, B = 0 };

            var removeCommand = ReactiveCommand.Create();
            removeCommand.Subscribe(_ => this.IsRemoveRequested = true);
            this.RemoveCommand = removeCommand;
        }

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        public Color SelectedColor
        {
            get { return this.selectedColor; }
            set { this.RaiseAndSetIfChanged(ref this.selectedColor, value); }
        }

        /// <summary>
        /// Gets the selected color as an OxyColor.
        /// </summary>
        public OxyColor SelectedOxyColor
        {
            get
            {
                return OxyColor.FromArgb(
                            this.SelectedColor.A,
                            this.SelectedColor.R,
                            this.SelectedColor.G,
                            this.SelectedColor.B);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this color view model should be removed.
        /// </summary>
        public bool IsRemoveRequested
        {
            get { return this.isRemoveRequested; }
            set { this.RaiseAndSetIfChanged(ref this.isRemoveRequested, value); }
        }

        /// <summary>
        /// Gets a command for removing the color.
        /// </summary>
        public IReactiveCommand RemoveCommand { get; private set; }
    }
}
