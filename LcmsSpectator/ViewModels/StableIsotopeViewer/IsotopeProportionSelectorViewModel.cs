namespace LcmsSpectator.ViewModels.StableIsotopeViewer
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;

    using InformedProteomics.Backend.Data.Biology;

    using ReactiveUI;

    /// <summary>
    /// This is a view model for editing the proportions of the isotopes for a given element.
    /// </summary>
    public class IsotopeProportionSelectorViewModel : ReactiveObject
    {
        /// <summary>
        /// Initializes new instance of the <see cref="IsotopeProportionSelectorViewModel" /> class.
        /// </summary>
        public IsotopeProportionSelectorViewModel(Atom atom, double[] defaultIsotopeRatios)
        {
            this.Atom = atom;
            this.IsotopeRatios = new ReactiveList<IsotopeProportionViewModel> { ChangeTrackingEnabled = true };

            // When an isotope ratio is selected, deselect all others.
            this.IsotopeRatios.ItemChanged.Where(i => i.PropertyName == "IsSelected")
                .Where(i => i.Sender.IsSelected)
                .Subscribe(
                    i =>
                        {
                            foreach (var ratio in this.IsotopeRatios)
                            {
                                if (i.Sender != ratio)
                                {
                                    ratio.IsSelected = false;
                                }
                            }
                        });

            var nominalMass = atom.NominalMass;
            for (int i = 0; i < defaultIsotopeRatios.Length; i++)
            {
                this.IsotopeRatios.Add(new IsotopeProportionViewModel(i > 0, defaultIsotopeRatios[i])
                {
                    IsSelected = i == 1,    // Automatically select the first isotope that isn't monoisotope
                    IsotopeIndex = i,
                    NominalMass = nominalMass + i,
                });
            }

            // When selected isotope proportion changes, decrease monoisotope
            this.IsotopeRatios.ItemChanged.Where(i => i.PropertyName == "Proportion")
                .Where(i => i.Sender.IsotopeIndex > 0)  // Do not want to do anything if the monoisotope changes,
                .Subscribe(                             // because that can give us an infinite loop.
                    i =>
                        {
                            var isotope = i.Sender;
                            var monoisotope = this.IsotopeRatios.FirstOrDefault(ir => ir.IsotopeIndex == 0);
                            if (monoisotope == null)
                            {
                                return;
                            }

                            // Reduce the monoisotope by the amount the isotope proportion was increased by
                            // If the proportion decreases, this should increase the monoisotope proportion.
                            monoisotope.Proportion = monoisotope.DefaultProportion - isotope.Delta;
                        });

            // Track changing so we know if we can use the default isotope profile predictor.
            this.IsotopeRatios.ItemChanged.Where(i => i.PropertyName == "Proportion")
                .Subscribe(_ => this.ProportionChanged = true);

            this.ProportionChanged = false;
        }

        /// <summary>
        /// Gets the atom that this 
        /// </summary>
        public Atom Atom { get; private set; }

        /// <summary>
        /// Gets the isotope ratio for each isotope index.
        /// </summary>
        public ReactiveList<IsotopeProportionViewModel> IsotopeRatios { get; private set; }

        /// <summary>
        /// Gets a value indicating whether any of the proportions have been changed.
        /// </summary>
        public bool ProportionChanged { get; private set; }

        /// <summary>
        /// Gets an array of selected proprtions.
        /// </summary>
        /// <returns>
        /// Gets an array of isotope proportions where index 0 is the monoisotope.
        /// </returns>
        public double[] GetProportions()
        {
            var proportions = new double[this.IsotopeRatios.Count];
            for (int i = 0; i < proportions.Length; i++)
            {
                proportions[i] = this.IsotopeRatios[i].Proportion;
            }

            return proportions;
        }

        /// <summary>
        /// Reset all isotope proportions back to their original values.
        /// </summary>
        public void Reset()
        {
            foreach (var isotopeRatio in this.IsotopeRatios)
            {
                isotopeRatio.Reset();
            }

            this.ProportionChanged = false;
        }
    }
}
