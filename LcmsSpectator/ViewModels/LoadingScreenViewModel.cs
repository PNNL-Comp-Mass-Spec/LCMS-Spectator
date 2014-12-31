using System.Threading;
using GalaSoft.MvvmLight;
using LcmsSpectator.TaskServices;

namespace LcmsSpectator.ViewModels
{
    public class LoadingScreenViewModel: ViewModelBase
    {
        public LoadingScreenViewModel(ITaskService taskService, string text="Loading\nPlease Wait")
        {
            _taskService = taskService;
            _text = text;
        }

        public string Text
        {
            get { return _text; }
            private set
            {
                _text = value;
                RaisePropertyChanged();
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                if (_isLoading)
                {
                    StartLoading();
                }
                RaisePropertyChanged();
            }
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

            _taskService.Enqueue(() =>
            {
                int index = 0;
                while (IsLoading)
                {
                    Thread.Sleep(750);
                    Text = loadingStrings[index % 4];
                    index++;
                }
            }, true);
        }


        private string _text;
        private bool _isLoading;
        private readonly ITaskService _taskService;
    }
}
