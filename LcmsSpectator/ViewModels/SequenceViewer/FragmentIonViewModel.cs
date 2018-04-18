namespace LcmsSpectator.ViewModels.SequenceViewer
{
    using System.Windows.Media;

    using Data;

    using ReactiveUI;

    public class FragmentIonViewModel : ReactiveObject
    {
        public LabeledIonViewModel LabeledIonViewModel
        {
            get;
            set;
        }

        public string ChargeSymbol => LabeledIonViewModel == null || LabeledIonViewModel.IonType.Charge < 2 ?
                                          string.Empty :
                                          string.Format("{0}+", LabeledIonViewModel.IonType.Charge);

        /// <summary>
        /// The color assigned to this fragment ion.
        /// </summary>
        private Brush color;

        /// <summary>
        /// Gets or sets the color assigned to this fragment ion.
        /// </summary>
        public Brush Color
        {
            get => color;
            set => this.RaiseAndSetIfChanged(ref color, value);
        }
    }
}
