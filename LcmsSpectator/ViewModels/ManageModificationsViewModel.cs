// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ManageModificationsViewModel.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   View model for managing modifications registered by the application.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.ViewModels
{
    using System;
    using InformedProteomics.Backend.Data.Sequence;
    using LcmsSpectator.Config;
    using LcmsSpectator.DialogServices;
    using ReactiveUI;

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
        /// Initializes a new instance of the <see cref="ManageModificationsViewModel"/> class. 
        /// </summary>
        /// <param name="dialogService">Dialog service for opening dialogs from the view model.</param>
        public ManageModificationsViewModel(IMainDialogService dialogService)
        {
            this.dialogService = dialogService;
            this.Modifications = new ReactiveList<Modification>();

            var addCommand = ReactiveCommand.Create();
            addCommand.Subscribe(_ => this.AddImplementation());
            this.AddCommand = addCommand;

            var editCommand = ReactiveCommand.Create();
            editCommand.Subscribe(_ => this.EditImplementation());
            this.EditCommand = editCommand;

            var removeCommand = ReactiveCommand.Create();
            removeCommand.Subscribe(_ => this.RemoveImplementation());
            this.RemoveCommand = removeCommand;
        }

        /// <summary>
        /// Gets a command that adds a new modification to the modification list.
        /// </summary>
        public IReactiveCommand AddCommand { get; private set; }

        /// <summary>
        /// Gets a command for editing the selected modification.
        /// </summary>
        public IReactiveCommand EditCommand { get; private set; }

        /// <summary>
        /// Gets a command that removes the selected modification from the modification list.
        /// </summary>
        public IReactiveCommand RemoveCommand { get; private set; }

        /// <summary>
        /// Gets the list of modifications.
        /// </summary>
        public ReactiveList<Modification> Modifications { get; private set; }

        /// <summary>
        /// Gets or sets the modification selected from the list of modifications.
        /// </summary>
        public Modification SelectedModification
        {
            get { return this.selectedModification; }
            set { this.RaiseAndSetIfChanged(ref this.selectedModification, value); }
        }

        /// <summary>
        /// Implementation for AddCommand.
        /// Adds a new modification to the modification list.
        /// </summary>
        private void AddImplementation()
        {
            var customModVm = new CustomModificationViewModel(string.Empty, false, this.dialogService);
            if (this.dialogService.OpenCustomModification(customModVm))
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
                    this.Modifications.Add(modification);
                }
            }
        }

        /// <summary>
        /// Implementation for EditCommand.
        /// Edits the selected modification.
        /// </summary>
        private void EditImplementation()
        {
            if (this.SelectedModification == null)
            {
                return;
            }

            // Set the composition or mass for the modification editor
            var customModVm = new CustomModificationViewModel(this.SelectedModification.Name, true, this.dialogService);
            if (this.SelectedModification.Composition != null)
            {
                customModVm.FromFormulaChecked = true;
                customModVm.Composition = this.SelectedModification.Composition;
            }
            else
            {
                customModVm.FromMassChecked = true;
                customModVm.Mass = this.SelectedModification.Mass;
            }

            if (this.dialogService.OpenCustomModification(customModVm))
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
                    for (int i = 0; i < this.Modifications.Count; i++)
                    {
                        if (modification.Name == this.Modifications[i].Name)
                        {
                            this.Modifications[i] = modification;
                            this.SelectedModification = modification;
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
            if (this.SelectedModification != null && this.Modifications.Contains(this.SelectedModification))
            {
                IcParameters.Instance.UnregisterModification(this.SelectedModification);
                this.Modifications.Remove(this.SelectedModification);
            }
        }
    }
}
