// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManageModificationsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for managing modifications registered by the application.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using InformedProteomics.Backend.Data.Composition;
using InformedProteomics.Backend.Data.Sequence;
using LcmsSpectator.Config;
using LcmsSpectator.DialogServices;
using ReactiveUI;

namespace LcmsSpectator.ViewModels.Modifications
{
    /// <summary>
    /// View model for managing modifications registered by the application.
    /// </summary>
    public class ManageModificationsViewModel : ReactiveObject
    {
        /// <summary>
        /// Dialog service for opening dialogs from the view model.
        /// </summary>
        private readonly IMainDialogService dialogService;

        /// <summary>
        /// The modification selected from the list of modifications.
        /// </summary>
        private Modification selectedModification;

        /// <summary>
        /// Default constructor to support WPF design-time use
        /// </summary>
        [Obsolete("For WPF Design-time use only.", true)]
        public ManageModificationsViewModel()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManageModificationsViewModel"/> class.
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from the view model.</param>
        public ManageModificationsViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            Modifications = new ReactiveList<Modification>();

            AddCommand = ReactiveCommand.Create(AddImplementation);
            EditCommand = ReactiveCommand.Create(EditImplementation, this.WhenAnyValue(x => x.SelectedModification).Select(m => m != null));
            RemoveCommand = ReactiveCommand.Create(RemoveImplementation, this.WhenAnyValue(x => x.SelectedModification).Select(m => m != null));
        }

        /// <summary>
        /// Gets a command that adds a new modification to the modification list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> AddCommand { get; }

        /// <summary>
        /// Gets a command for editing the selected modification.
        /// </summary>
        public ReactiveCommand<Unit, Unit> EditCommand { get; }

        /// <summary>
        /// Gets a command that removes the selected modification from the modification list.
        /// </summary>
        public ReactiveCommand<Unit, Unit> RemoveCommand { get; }

        /// <summary>
        /// Gets the list of modifications.
        /// </summary>
        public ReactiveList<Modification> Modifications { get; }

        /// <summary>
        /// Gets or sets the modification selected from the list of modifications.
        /// </summary>
        public Modification SelectedModification
        {
            get => selectedModification;
            set => this.RaiseAndSetIfChanged(ref selectedModification, value);
        }

        /// <summary>
        /// Implementation for AddCommand.
        /// Adds a new modification to the modification list.
        /// </summary>
        private void AddImplementation()
        {
            var customModVm = new CustomModificationViewModel(string.Empty, false, dialogService);
            if (dialogService.OpenCustomModification(customModVm))
            {
                Modification modification = null;
                if (customModVm.FromFormulaChecked)
                {
                    modification = IcParameters.Instance.RegisterModification(
                        customModVm.ModificationName,
                        customModVm.Composition);
                }
                else if (customModVm.FromMassChecked)
                {
                    modification = IcParameters.Instance.RegisterModification(
                        customModVm.ModificationName,
                        customModVm.Mass);
                }

                if (modification != null)
                {
                    Modifications.Add(modification);
                }
            }
        }

        /// <summary>
        /// Implementation for EditCommand.
        /// Edits the selected modification.
        /// </summary>
        private void EditImplementation()
        {
            if (SelectedModification == null)
            {
                return;
            }

            // Set the composition or mass for the modification editor
            var customModVm = new CustomModificationViewModel(SelectedModification.Name, true, dialogService);
            if (SelectedModification.Composition is CompositionWithDeltaMass)
            {   // Modification with mass shift
                customModVm.FromFormulaChecked = false;
                customModVm.FromMassChecked = true;
                customModVm.Mass = SelectedModification.Mass;
            }
            else
            {   // Modification with formula
                customModVm.FromMassChecked = false;
                customModVm.FromFormulaChecked = true;
                customModVm.Composition = SelectedModification.Composition;
            }

            if (dialogService.OpenCustomModification(customModVm))
            {
                Modification modification = null;
                if (customModVm.FromFormulaChecked)
                {
                    modification = IcParameters.Instance.UpdateOrRegisterModification(
                        customModVm.ModificationName,
                        customModVm.Composition);
                }
                else if (customModVm.FromMassChecked)
                {
                    modification = IcParameters.Instance.UpdateOrRegisterModification(
                        customModVm.ModificationName,
                        customModVm.Mass);
                }

                if (modification != null)
                {
                    // Replace old modification in the list
                    for (var i = 0; i < Modifications.Count; i++)
                    {
                        if (modification.Name == Modifications[i].Name)
                        {
                            Modifications[i] = modification;
                            SelectedModification = modification;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Implementation for RemoveCommand.
        /// Removes the selected modification from the modification list.
        /// </summary>
        private void RemoveImplementation()
        {
            if (Modifications.Contains(SelectedModification) &&
                dialogService.ConfirmationBox(
                                    string.Format(
                                        "Are you sure you would like to delete {0}?",
                                        SelectedModification.Name),
                                        "Delete Modification"))
            {
                IcParameters.Instance.UnregisterModification(SelectedModification);
                Modifications.Remove(SelectedModification);
                SelectedModification = Modifications.FirstOrDefault();
            }
        }
    }
}
