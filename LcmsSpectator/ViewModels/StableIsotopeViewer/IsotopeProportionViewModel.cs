using System;
using System.Reactive.Linq;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.StableIsotopeViewer
{
    /// <summary>
    /// View model for editing the proportion for a single isotope.
    /// </summary>
    public class IsotopeProportionViewModel : ReactiveObject
    {
        /// <summary>
        /// The isotope index. 0 is the monoisotope.
        /// </summary>
        private int isotopeIndex;

        /// <summary>
        /// The nominal mass for the isotope.
        /// </summary>
        private int nominalMass;

        /// <summary>
        /// The proportion of this isotope of all isotopes of the element.
        /// </summary>
        private double proportion;

        /// <summary>
        /// A value indicating whether this isotope has been selected for manipulation.
        /// </summary>
        private bool isSelected;

        /// <summary>
        /// Initializes an instance of the <see cref="IsotopeProportionViewModel" /> class.
        /// </summary>
        /// <param name="isSelectable">A value indicating whether this isotope can be selected.</param>
        /// <param name="defaultProportion"></param>
        public IsotopeProportionViewModel(bool isSelectable, double defaultProportion)
        {
            DefaultProportion = defaultProportion;
            Proportion = defaultProportion;
            IsSelectable = isSelectable;

            // When this isotope is deselected, change proportion back to original value.
            this.WhenAnyValue(x => x.IsSelected)
                .Where(isSelected => !isSelected)
                .Subscribe(_ => Reset());
        }

        /// <summary>
        /// Gets a value indicating whether this isotope can be selected.
        /// </summary>
        public bool IsSelectable { get; }

        /// <summary>
        /// Gets or sets the isotope index. 0 is the monoisotope.
        /// </summary>
        public int IsotopeIndex
        {
            get => isotopeIndex;
            set => this.RaiseAndSetIfChanged(ref isotopeIndex, value);
        }

        /// <summary>
        /// Gets or sets the nominal mass for the isotope.
        /// </summary>
        public int NominalMass
        {
            get => nominalMass;
            set => this.RaiseAndSetIfChanged(ref nominalMass, value);
        }

        /// <summary>
        /// Gets the default and minimum proportion.
        /// </summary>
        public double DefaultProportion { get; }

        /// <summary>
        /// Gets or sets the proportion of this isotope of all isotopes of the element.
        /// </summary>
        public double Proportion
        {
            get => proportion;
            set => this.RaiseAndSetIfChanged(ref proportion, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this isotope has been selected for manipulation.
        /// </summary>
        public bool IsSelected
        {
            get => isSelected;
            set => this.RaiseAndSetIfChanged(ref isSelected, value);
        }

        /// <summary>
        /// Gets the different between the proportion and the original, default proportion.
        /// </summary>
        public double Delta => Proportion - DefaultProportion;

        /// <summary>
        /// Reset the proportion back to its default value.
        /// </summary>
        public void Reset()
        {
            Proportion = DefaultProportion;
        }
    }
}
