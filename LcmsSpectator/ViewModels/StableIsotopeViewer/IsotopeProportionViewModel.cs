namespace LcmsSpectator.ViewModels.StableIsotopeViewer
{
    using System;

    using System.Reactive.Linq;

    using ReactiveUI;

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
            this.DefaultProportion = defaultProportion;
            this.Proportion = defaultProportion;
            this.IsSelectable = isSelectable;

            // When this isotope is deselected, change proportion back to original value.
            this.WhenAnyValue(x => x.IsSelected)
                .Where(isSelected => !isSelected)
                .Subscribe(_ => this.Reset());
        }

        /// <summary>
        /// Gets a value indicating whether this isotope can be selected.
        /// </summary>
        public bool IsSelectable { get; private set; }

        /// <summary>
        /// Gets or sets the isotope index. 0 is the monoisotope.
        /// </summary>
        public int IsotopeIndex
        {
            get { return this.isotopeIndex; }
            set { this.RaiseAndSetIfChanged(ref this.isotopeIndex, value); }
        }

        /// <summary>
        /// Gets or sets the nominal mass for the isotope.
        /// </summary>
        public int NominalMass
        {
            get { return this.nominalMass; }
            set { this.RaiseAndSetIfChanged(ref this.nominalMass, value); }
        }

        /// <summary>
        /// Gets the default and minimum proportion.
        /// </summary>
        public double DefaultProportion { get; private set; }

        /// <summary>
        /// Gets or sets the proportion of this isotope of all isotopes of the element.
        /// </summary>
        public double Proportion
        {
            get { return this.proportion; }
            set { this.RaiseAndSetIfChanged(ref this.proportion, value); } 
        }

        /// <summary>
        /// Gets or sets a value indicating whether this isotope has been selected for manipulation.
        /// </summary>
        public bool IsSelected
        {
            get { return this.isSelected; }
            set { this.RaiseAndSetIfChanged(ref this.isSelected, value); }
        }

        /// <summary>
        /// Gets the different between the proportion and the original, default proportion.
        /// </summary>
        public double Delta { get { return this.Proportion - this.DefaultProportion; } }

        /// <summary>
        /// Reset the proportion back to its default value.
        /// </summary>
        public void Reset()
        {
            this.Proportion = this.DefaultProportion;
        }
    }
}
