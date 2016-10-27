namespace LcmsSpectator.ViewModels.Data
{
    using System;
    
    using ReactiveUI;

    /// <summary>
    /// This class is a view model for items that are displayed in a list view.
    /// </summary>
    /// <typeparam name="T">This should be a view model for the item to display in the collection.</typeparam>
    public class ListItemViewModel<T> : ReactiveObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemViewModel{T}" /> class.
        /// </summary>
        /// <param name="item">The item that is part of the list.</param>
        public ListItemViewModel(T item)
        {
            this.Item = item;

            this.RemoveCommand = ReactiveCommand.Create();
            this.RemoveCommand.Subscribe(_ => this.ShouldBeRemoved = true);
        }

        /// <summary>
        /// Gets the item from the list.
        /// </summary>
        public T Item { get; private set; }

        /// <summary>
        /// Gets a command that marks this item for removal from its containing list.
        /// </summary>
        public ReactiveCommand<object> RemoveCommand { get; private set; }

        /// <summary>
        /// A value indicating whether this item should be removed from its containing list.
        /// </summary>
        private bool shouldBeRemoved;

        /// <summary>
        /// Gets a value indicating whether this item should be removed from its containing list.
        /// </summary>
        public bool ShouldBeRemoved
        {
            get { return this.shouldBeRemoved; }
            private set { this.RaiseAndSetIfChanged(ref this.shouldBeRemoved, value); }
        }
    }
}
