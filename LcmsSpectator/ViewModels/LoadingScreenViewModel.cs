using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace LcmsSpectator.ViewModels
{
    public class LoadingScreenViewModel: ReactiveObject
    {
        public LoadingScreenViewModel()
        {
            this.WhenAnyValue(x => x.IsLoading)
                .Where(isLoading => isLoading)
                .Subscribe(_ => StartLoading());
        }

        private string _text;
        public string Text
        {
            get { return _text; }
            private set { this.RaiseAndSetIfChanged(ref _text, value); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get { return _isLoading; }
            set { this.RaiseAndSetIfChanged(ref _isLoading, value); }
        }

        public void StartLoading()
        {
            string[] loadingStrings =
			{
				"Loading\nPlease Wait",
				"Loading.\nPlease Wait",
				"Loading..\nPlease Wait",
				"Loading...\nPlease Wait"
			};

            Task.Run(() =>
            {
                int index = 0;
                while (IsLoading)
                {
                    Thread.Sleep(750);
                    Text = loadingStrings[index % 4];
                    index++;
                }
            });
        }
    }
}
