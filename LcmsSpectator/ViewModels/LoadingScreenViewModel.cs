// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LoadingScreenViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model that displays animated loading screen text.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using System.Reactive.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ReactiveUI;

    /// <summary>
    /// View model that displays animated loading screen text.
    /// </summary>
    public class LoadingScreenViewModel : ReactiveObject
    {
        /// <summary>
        /// The animated loading screen text.
        /// </summary>
        private string text;

        /// <summary>
        /// A value indicating whether or not the loading screen should be animating its text.
        /// </summary>
        private bool isLoading;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadingScreenViewModel"/> class.
        /// </summary>
        public LoadingScreenViewModel()
        {
            ////this.WhenAnyValue(x => x.IsLoading)
            ////    .Where(isLoading => isLoading)
            ////    .Subscribe(_ => this.StartLoading());
        }

        /// <summary>
        /// Gets the animated loading screen text.
        /// </summary>
        public string Text
        {
            get { return this.text; }
            private set { this.RaiseAndSetIfChanged(ref this.text, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the loading screen should be animating its text.
        /// </summary>
        public bool IsLoading
        {
            get { return this.isLoading; }
            set { this.RaiseAndSetIfChanged(ref this.isLoading, value); }
        }

        /// <summary>
        /// Starts and runs the thread that animates the loading screen text.
        /// </summary>
        ////private void StartLoading()
        ////{
        ////    string[] loadingStrings =
        ////    {
        ////        "Loading\nPlease Wait",
        ////        "Loading.\nPlease Wait",
        ////        "Loading..\nPlease Wait",
        ////        "Loading...\nPlease Wait"
        ////    };

        ////    Task.Run(() =>
        ////    {
        ////        int index = 0;
        ////        while (IsLoading)
        ////        {
        ////            Thread.Sleep(750);
        ////            Text = loadingStrings[index % 4];
        ////            index++;
        ////        }
        ////    });
        ////}
    }
}
