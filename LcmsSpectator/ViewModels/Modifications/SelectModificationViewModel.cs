namespace LcmsSpectator.ViewModels.Modifications
{
    using System;

    using System.Collections.Generic;

    using System.Reactive.Linq;

    using ReactiveUI;

    /// <summary>
    ///
    /// </summary>
    public class SelectModificationViewModel : WindowViewModel
    {
        /// <summary>
        /// The modification selected from the <see cref="Modifications" /> list.
        /// </summary>
        private ModificationViewModel modificationViewModel;

        /// <summary>
        /// Initializes new instance of the <see cref="SelectModificationViewModel" /> class.
        /// </summary>
        /// <param name="modifications">The modifications to select from.</param>
        public SelectModificationViewModel(IEnumerable<ModificationViewModel> modifications)
        {
            Modifications = new ReactiveList<ModificationViewModel>(modifications);
        }

        /// <summary>
        /// The modifications to select from.
        /// </summary>
        public ReactiveList<ModificationViewModel> Modifications { get; }

        /// <summary>
        /// Gets or sets the modification from the <see cref="Modifications" /> list.
        /// </summary>
        public ModificationViewModel SelectedModification
        {
            get => modificationViewModel;
            set => this.RaiseAndSetIfChanged(ref modificationViewModel, value);
        }

        /// <summary>
        /// Gets an observable that determines whether or not  the Success command is executable.
        /// </summary>
        protected override IObservable<bool> CanSucceed
        {
            get { return this.WhenAnyValue(x => x.SelectedModification).Select(_ => Validate()); }
        }

        /// <summary>
        /// Function that checks whether or not the input to this window is valid.
        /// </summary>
        /// <returns>A value indicating whether the input to this window is valid.</returns>
        protected override bool Validate()
        {
            return SelectedModification != null;
        }
    }
}
