// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SequenceGraph.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for SequenceGraph.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.SequenceGraph
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows;

    using GraphX.Controls.Models;
    using GraphX.PCL.Common.Enums;
    using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;

    using InformedProteomics.Backend.Data.Sequence;

    /// <summary>
    /// Interaction logic for SequenceGraph.xaml
    /// </summary>
    public partial class SequenceGraph
    {
        /// <summary>
        /// Converts InformedProteomics sequence graph to GraphX sequence graph.
        /// </summary>
        private GraphXSequenceGraph sequenceGraph;

        /// <summary>
        /// The vertices selected in the sequence graph.
        /// </summary>
        private List<List<DataVertex>> selectedVertices;

        public SequenceGraph()
        {
            InitializeComponent();
            Graph = new DataGraph();
            BuildSequenceGraphLogicCore();
            SelectedSequence = new Sequence(new List<AminoAcid>());
            SelectedVertices = new List<List<DataVertex>>();
            SequenceGraphArea.Loaded += SequenceGraphArea_Loaded;
            SequenceGraphArea.VertexSelected += VertexSelectedEvent;
        }

        public DataGraph Graph { get; private set; }

        /// <summary>
        /// Generate sequence graph from data graph.
        /// </summary>
        private void GenerateSequenceGraph()
        {
            sequenceGraph = GraphXSequenceGraph.Create(
                                    new AminoAcidSet(SearchModifications, MaxDynamicModifications),
                                    string.Empty,
                                    SearchModifications.ToList());
            if (sequenceGraph == null)
            {
                return;
            }

            var graph = sequenceGraph.DataGraph;
            Graph = graph;
            SequenceGraphArea.LogicCore.Graph = graph;
            SequenceGraphArea.ShowAllEdgesLabels();
            SequenceGraphArea.GenerateGraph(true);
        }

        /// <summary>
        /// Event handler that is triggered when the sequence graph has loaded.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void SequenceGraphArea_Loaded(object sender, RoutedEventArgs e)
        {
            SequenceGraphArea.ShowAllEdgesLabels();
            SequenceGraphArea.GenerateGraph(true);
        }

        /// <summary>
        /// Event handler that is triggered when a vertex is clicked.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The event arguments.</param>
        private void VertexSelectedEvent(object sender, VertexSelectedEventArgs args)
        {
            if (!(sequenceGraph is GraphXSequenceGraph seqGraph))
            {
                return;
            }

            var graph = (DataGraph)SequenceGraphArea.LogicCore.Graph;
            var vertex = (DataVertex)args.VertexControl.Vertex;
            SelectedVertices = graph.GetAllSequencePaths(vertex, seqGraph.EndPoint);
        }

        /// <summary>
        /// Sets the vertices selected from the sequence graph.
        /// </summary>
        private List<List<DataVertex>> SelectedVertices
        {
            get => selectedVertices;

            set
            {
/*                if (_selectedVertices != null)
                {
                    foreach (var path in _selectedVertices)
                    {
                        foreach (var node in path)
                        {
                            var vertexControl = SequenceGraphArea.VertexList[node];
                            vertexControl.Background = Brushes.Gainsboro;
                        }
                    }
                }
                _selectedVertices = value;
                foreach (var path in _selectedVertices)
                {
                    foreach (var node in path)
                    {
                        var vertexControl = SequenceGraphArea.VertexList[node];
                        vertexControl.Background = Brushes.Gold;
                    }
                } */
            }
        }

        private void BuildSequenceGraphLogicCore()
        {
            const LayoutAlgorithmTypeEnum algo = LayoutAlgorithmTypeEnum.Tree;
            var logicCore = new LogicCore { DefaultLayoutAlgorithm = algo };

            logicCore.DefaultLayoutAlgorithmParams = logicCore.AlgorithmFactory.CreateLayoutParameters(algo);
            var layoutParams = ((SimpleTreeLayoutParameters)logicCore.DefaultLayoutAlgorithmParams);
            layoutParams.Direction = LayoutDirection.LeftToRight;
            layoutParams.LayerGap = 125;
            layoutParams.VertexGap = 40;

            logicCore.DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.None;
            logicCore.DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.None;
            logicCore.AsyncAlgorithmCompute = false;

            if (SequenceGraphArea.LogicCore?.Graph != null)
            {
                logicCore.Graph = SequenceGraphArea.LogicCore.Graph;
            }

            SequenceGraphArea.LogicCore = logicCore;
            SequenceGraphArea.GenerateGraph(true);
        }

        #region SelectedSequence Dependency Property (Output Property)
        /// <summary>
        /// Gets the sequence selected in the sequence graph.
        /// </summary>
        public Sequence SelectedSequence
        {
            get => (Sequence)GetValue(SelectedSequenceProperty);
            private set => SetValue(SelectedSequenceProperty, value);
        }

        /// <summary>
        /// Dependency property for the sequence selected in the sequence graph.
        /// </summary>
        public static readonly DependencyProperty SelectedSequenceProperty = DependencyProperty.Register(
        "SelectedSequence", typeof(Sequence), typeof(SequenceGraph), new PropertyMetadata(null));
        #endregion

        #region ProteinSequence Dependency Property (Input Property)
        /// <summary>
        /// Gets or sets the protein sequence represented by this sequence graph.
        /// </summary>
        public Sequence ProteinSequence
        {
            get => (Sequence)GetValue(ProteinSequenceProperty);
            set => SetValue(ProteinSequenceProperty, value);
        }

        /// <summary>
        /// Dependency property for the protein sequence represented by this graph.
        /// </summary>
        public static readonly DependencyProperty ProteinSequenceProperty = DependencyProperty.Register(
            "ProteinSequence", typeof(IEnumerable<SearchModification>), typeof(SequenceGraph),
            new FrameworkPropertyMetadata(OnSearchModificationsPropertyChanged));

        /// <summary>
        /// Event handler triggered when the ProteinSequence depedency property is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnProteinSequencePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }
        #endregion

        #region Search Modifications Dependency Property (Input Property)
        /// <summary>
        /// Gets or sets the search modifications.
        /// </summary>
        public IEnumerable<SearchModification> SearchModifications
        {
            get => (IEnumerable<SearchModification>)GetValue(SearchModificationsProperty);
            set => SetValue(SearchModificationsProperty, value);
        }

        /// <summary>
        /// Dependency property for search modifications.
        /// </summary>
        public static readonly DependencyProperty SearchModificationsProperty = DependencyProperty.Register(
            "SearchModifications", typeof(IEnumerable<SearchModification>), typeof(SequenceGraph),
            new FrameworkPropertyMetadata(OnSearchModificationsPropertyChanged));

        /// <summary>Event handler that is triggered when search modifications change.</summary>
        /// <param name="sender">The sending DependencyObject.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnSearchModificationsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }
        #endregion

        #region MaxDynamicModifications Dependency Property (Input Property)
        /// <summary>
        /// Gets or sets the maximum dynamic modifications per sequence.
        /// </summary>
        public int MaxDynamicModifications
        {
            get => (int)GetValue(MaxDynamicModificationsProperty);
            set => SetValue(MaxDynamicModificationsProperty, value);
        }

        /// <summary>
        /// Dependency property for maximum dynamic modifications per sequence.
        /// </summary>
        public static readonly DependencyProperty MaxDynamicModificationsProperty = DependencyProperty.Register(
            "MaxDynamicModifications", typeof(int), typeof(SequenceGraph),
            new FrameworkPropertyMetadata(MaxDynamicModificationsPropertyChanged));

        /// <summary>
        /// Event handler for when MaxDynamicModifications is set.
        /// </summary>
        /// <param name="sender">The sending dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void MaxDynamicModificationsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
        }
        #endregion
    }
}