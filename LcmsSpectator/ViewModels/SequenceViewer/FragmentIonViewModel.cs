namespace LcmsSpectator.ViewModels.SequenceViewer
{
    using System.Windows.Media;

    using LcmsSpectator.ViewModels.Data;

    using ReactiveUI;

    public class FragmentIonViewModel : ReactiveObject
    {
        public LabeledIonViewModel LabeledIonViewModel
        {
            get;
            set;
        }

        public string ChargeSymbol
        {
            get
            { 
                return this.LabeledIonViewModel == null || this.LabeledIonViewModel.IonType.Charge < 2 ? 
                       string.Empty : 
                       string.Format("{0}+", this.LabeledIonViewModel.IonType.Charge);
            }
        }

        /// <summary>
        /// The color assigned to this fragment ion.
        /// </summary>
        private Brush color;

        /// <summary>
        /// Gets or sets the color assigned to this fragment ion.
        /// </summary>
        public Brush Color
        {
            get { return this.color; }
            set { this.RaiseAndSetIfChanged(ref this.color, value); }
        }
    }
}
