using System;
using System.Linq;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using LcmsSpectator.ViewModels.Modifications;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.SequenceViewer
{
    /// <summary>
    /// View model for representing a sequence cleavage on
    /// </summary>
    public class FragmentViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening LCMSSpectator dialogs.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// The selected amino acid to display.
        /// </summary>
        private AminoAcid aminoAcid;

        /// <summary>
        /// The text to display for the modification.
        /// </summary>
        private string modificationSymbol;

        /// <summary>
        /// The index of this fragment within the greater sequence that it is part of.
        /// </summary>
        private int index;

        /// <summary>
        /// The prefix ion selected for the cleavage at this residue.
        /// </summary>
        private FragmentIonViewModel prefixIon;

        /// <summary>
        /// The suffix ion selected for the cleavage at this residue.
        /// </summary>
        private FragmentIonViewModel suffixIon;

        /// <summary>
        /// Initializes new instance of <see cref="FragmentViewModel" />.
        /// </summary>
        /// <param name="aminoAcid">The selected amino acid.</param>
        /// <param name="index">The index of this fragment within the greater sequence that it is part of.</param>
        /// <param name="dialogService">Dialog service for opening LCMSSpectator dialogs.</param>
        public FragmentViewModel(AminoAcid aminoAcid, int index = 0, IMainDialogService dialogService = null)
        {
            AminoAcid = aminoAcid;
            SetModSymbol(aminoAcid as ModifiedAminoAcid);

            this.dialogService = dialogService ?? new MainDialogService();
            SelectModificationCommand = ReactiveCommand.Create();
            SelectModificationCommand.Subscribe(_ => SelectModificationImpl());

            // Update the modification symbol when the amino acid changes.
            this.WhenAnyValue(x => x.AminoAcid).Subscribe(aa => SetModSymbol(aa as ModifiedAminoAcid));
        }

        /// <summary>
        /// Gets a command that opens a dialog that allows the user to change the selected modification.
        /// </summary>
        public ReactiveCommand<object> SelectModificationCommand { get; }

        /// <summary>
        /// Gets the selected amino acid.
        /// </summary>
        public AminoAcid AminoAcid
        {
            get => aminoAcid;
            private set => this.RaiseAndSetIfChanged(ref aminoAcid, value);
        }

        /// <summary>
        /// Gets the index of this fragment within the greater sequence that it is part of.
        /// </summary>
        public int Index
        {
            get => index;
            private set => this.RaiseAndSetIfChanged(ref index, value);
        }

        /// <summary>
        /// Gets or sets the text to display for the modification.
        /// </summary>
        public string ModificationSymbol
        {
            get => modificationSymbol;
            private set => this.RaiseAndSetIfChanged(ref modificationSymbol, value);
        }

        /// <summary>
        /// Gets or sets the prefix ion selected for the cleavage at this residue.
        /// </summary>
        public FragmentIonViewModel PrefixIon
        {
            get => prefixIon;
            set => this.RaiseAndSetIfChanged(ref prefixIon, value);
        }

        /// <summary>
        /// Gets or sets the suffix ion selected for the cleavage at this residue.
        /// </summary>
        public FragmentIonViewModel SuffixIon
        {
            get => suffixIon;
            set => this.RaiseAndSetIfChanged(ref suffixIon, value);
        }

        /// <summary>
        /// Update the <see cref="ModificationSymbol" /> based on the modified amino acid.
        /// </summary>
        /// <param name="modifiedAminoAcid">
        /// The modified amino acid to extract the modification from.
        /// </param>
        private void SetModSymbol(ModifiedAminoAcid modifiedAminoAcid)
        {
            if (modifiedAminoAcid != null)
            {
                var modification = modifiedAminoAcid.Modification;
                ModificationSymbol = modification.Name.Substring(0, Math.Min(2, modification.Name.Length));
            }
        }

        /// <summary>
        /// Implementation for <see cref="SelectModificationCommand" />.
        /// Opens a dialog that allows the user to change the selected modification.
        /// </summary>
        private void SelectModificationImpl()
        {
            var selectModificationViewModel = new SelectModificationViewModel(
                                                                IcParameters.Instance
                                                                            .RegisteredModifications
                                                                            .Select(mod => new ModificationViewModel(mod)));
            dialogService.OpenSelectModificationWindow(selectModificationViewModel);
            if (selectModificationViewModel.Status)
            {
                AminoAcid = new ModifiedAminoAcid(AminoAcid, selectModificationViewModel.SelectedModification.Modification);
            }
        }
    }
}
