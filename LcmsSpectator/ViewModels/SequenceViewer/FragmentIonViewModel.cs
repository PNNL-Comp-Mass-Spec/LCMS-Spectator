namespace LcmsSpectator.ViewModels.SequenceViewer
{
    using System.Windows.Media;

    using LcmsSpectator.ViewModels.Data;

    using ReactiveUI;

    public class FragmentIonViewModel : ReactiveObject
    {
        public LabeledIonViewModel LabeledIonViewModel { get; set; }

        public Color Color { get; set; }
    }
}
