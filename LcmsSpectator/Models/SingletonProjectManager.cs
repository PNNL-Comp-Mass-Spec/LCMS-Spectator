namespace LcmsSpectator.Models
{
    using System;
    using System.Linq;
    using System.Reactive.Linq;
    using InformedProteomics.Backend.Data.Composition;
    using InformedProteomics.Backend.Data.Sequence;
    using InformedProteomics.Backend.Data.Spectrometry;
    using LcmsSpectator.DialogServices;
    using LcmsSpectator.Models.Dataset;
    using LcmsSpectator.Readers;
    using LcmsSpectator.ViewModels.Dataset;
    using LcmsSpectator.ViewModels.Modifications;
    using ReactiveUI;
    
    public class SingletonProjectManager : ReactiveObject
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        private static SingletonProjectManager instance;

        /// <summary>
        /// Ion type factory for application.
        /// </summary>
        private static IonTypeFactory ionTypeFactory;

        /// <summary>
        /// Deconvoluted ion type factory for application.
        /// </summary>
        private static IonTypeFactory deconvolutedIonTypeFactory;

        /// <summary>
        /// The project info for the currently selected project.
        /// </summary>
        private ProjectInfo projectInfo;

        private SingletonProjectManager()
        {
            this.ProjectInfo = new ProjectInfo();
            this.ProjectLoader = new ProjectLoader();
            this.Datasets = new ReactiveList<DatasetViewModel> { ChangeTrackingEnabled = true };

            // Remove dataset when it is ready to close.
            this.Datasets.ItemChanged.Where(x => x.PropertyName == "ReadyToClose")
                         .Where(x => x.Sender.ReadyToClose)
                         .Subscribe(x => this.Datasets.Remove(x.Sender));

            // If dataset is closed, remove it from the project.
            this.Datasets.BeforeItemsRemoved
                         .Where(_ => this.ProjectInfo != null)
                         .Subscribe(ds => this.ProjectInfo.Datasets.Remove(ds.DatasetInfo));

            // If a dataset is opened, add it to the project.
            this.Datasets.BeforeItemsAdded
                         .Where(_ => this.ProjectInfo != null)
                         .Subscribe(ds => this.ProjectInfo.AddandInitDataset(ds.DatasetInfo));
        }

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static SingletonProjectManager Instance
        {
            get { return instance ?? (instance = new SingletonProjectManager()); }
        }

        /// <summary>
        /// Gets the ion type factory for application.
        /// </summary>
        public static IonTypeFactory IonTypeFactory
        {
            get { return ionTypeFactory ?? (ionTypeFactory = new IonTypeFactory(100)); }
        }

        /// <summary>
        /// Gets the de-convoluted ion type factory for application.
        /// </summary>
        public static IonTypeFactory DeconvolutedIonTypeFactory
        {
            get
            {
                return deconvolutedIonTypeFactory ??
                       (deconvolutedIonTypeFactory = IonTypeFactory.GetDeconvolutedIonTypeFactory(BaseIonType.AllBaseIonTypes, NeutralLoss.CommonNeutralLosses));
            }
        }

        /// <summary>
        /// Gets or sets the main dialog service for the application.
        /// </summary>
        public IMainDialogService DialogService { get; set; }

        /// <summary>
        /// Gets or sets the serializer for the project.
        /// </summary>
        public IProjectLoader ProjectLoader { get; set; }

        /// <summary>
        /// Gets the list of dataset view models for the project.
        /// </summary>
        public ReactiveList<DatasetViewModel> Datasets { get; private set; }

        /// <summary>
        /// Gets the project info for the currently selected project.
        /// </summary>
        public ProjectInfo ProjectInfo
        {
            get { return this.projectInfo; }
            private set { this.RaiseAndSetIfChanged(ref this.projectInfo, value); }
        }

        /// <summary>
        /// Deserialize the project info.
        /// </summary>
        /// <param name="filePath">Path of project info file to deserialize.</param>
        public void LoadProject(string filePath)
        {
            var projectLoader = this.ProjectLoader ?? new ProjectLoader();
            this.ProjectInfo = projectLoader.LoadProject(filePath);
        }

        /// <summary>
        /// Serialize the ProjectInfo.
        /// </summary>
        public void SaveProject()
        {
            var projectLoader = this.ProjectLoader ?? new ProjectLoader();
            projectLoader.SaveProject(this.ProjectInfo);
        }

        /// <summary>
        /// Prompt user for modification mass or formula and register it with the application
        /// </summary>
        /// <param name="modificationName">Name of the modification to register</param>
        /// <param name="modificationNameEditable">Should the modification name be editable by the user?</param>
        /// <returns>Whether or not a modification was successfully registered.</returns>
        public bool PromptRegisterModification(string modificationName = null, bool modificationNameEditable = true)
        {
            if (modificationName == null)
            {
                modificationName = string.Empty;
            }

            var customModVm = new CustomModificationViewModel(modificationName, modificationNameEditable, this.DialogService);
            this.DialogService.OpenCustomModification(customModVm);
            if (!customModVm.Status)
            {
                return false;
            }

            if (customModVm.FromFormulaChecked)
            {
                this.RegisterModification(customModVm.ModificationName, customModVm.Composition);
            }
            else if (customModVm.FromMassChecked)
            {
                this.RegisterModification(customModVm.ModificationName, customModVm.Mass);
            }

            return true;
        }

        /// <summary>
        /// Register a new modification with the application given an empirical formula.
        /// </summary>
        /// <param name="modName">Name of modification.</param>
        /// <param name="composition">Empirical formula of modification.</param>
        /// <returns>The modification that was registered with the application.</returns>
        public Modification RegisterModification(string modName, Composition composition)
        {
            var mod = Modification.RegisterAndGetModification(modName, composition);
            this.ProjectInfo.ModificationSettings.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Register a new modification with the application given a delta mass shift.
        /// </summary>
        /// <param name="modName">Name of modification.</param>
        /// <param name="mass">Delta mass of modification.</param>
        /// <returns>The modification that was registered with the application.</returns>
        public Modification RegisterModification(string modName, double mass)
        {
            var mod = Modification.RegisterAndGetModification(modName, mass);
            this.ProjectInfo.ModificationSettings.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Update or register a modification.
        /// </summary>
        /// <param name="modName">The name of the modification.</param>
        /// <param name="composition">The composition of the modification.</param>
        /// <returns>The registered modification.</returns>
        public Modification UpdateOrRegisterModification(string modName, Composition composition)
        {
            var mod = Modification.UpdateAndGetModification(modName, composition);
            var regMod = this.ProjectInfo.ModificationSettings.RegisteredModifications.FirstOrDefault(m => m.Name == modName);
            if (regMod != null)
            {
                this.ProjectInfo.ModificationSettings.RegisteredModifications.Remove(regMod);
            }

            this.ProjectInfo.ModificationSettings.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Update or register a modification.
        /// </summary>
        /// <param name="modName">The name of the modification.</param>
        /// <param name="mass">The mass of the modification.</param>
        /// <returns>The registered modification.</returns>
        public Modification UpdateOrRegisterModification(string modName, double mass)
        {
            var mod = Modification.UpdateAndGetModification(modName, mass);
            var regMod = this.ProjectInfo.ModificationSettings.RegisteredModifications.FirstOrDefault(m => m.Name == modName);
            if (regMod != null)
            {
                this.ProjectInfo.ModificationSettings.RegisteredModifications.Remove(regMod);
            }

            this.ProjectInfo.ModificationSettings.RegisteredModifications.Add(mod);
            return mod;
        }

        /// <summary>
        /// Unregister a modification.
        /// </summary>
        /// <param name="modification">The modification to unregister.</param>
        public void UnregisterModification(Modification modification)
        {
            Modification.UnregisterModification(modification);
            if (this.ProjectInfo.ModificationSettings.RegisteredModifications.Contains(modification))
            {
                this.ProjectInfo.ModificationSettings.RegisteredModifications.Remove(modification);
            }
        }
    }
}
