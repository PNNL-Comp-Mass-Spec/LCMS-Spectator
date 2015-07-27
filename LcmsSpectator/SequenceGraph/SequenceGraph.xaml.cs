namespace LcmsSpectator.SequenceGraph
{
    using System.Collections.Generic;
    using System.Windows;

    using GraphX.Controls.Models;
    using GraphX.PCL.Common.Enums;
    using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;

    using InformedProteomics.Backend.Data.Sequence;

    using LcmsSpectator.Models;

    /// <summary>
    /// Interaction logic for SequenceGraph.xaml
    /// </summary>
    public partial class SequenceGraph
    {
        public DataGraph Graph { get; private set; }

        public SequenceGraph()
        {
            this.InitializeComponent();
            this.Graph = new DataGraph();
            this.BuildSequenceGraphLogicCore();
            this.SelectedSequence = new Sequence(new List<AminoAcid>());
            this.SelectedVertices = new List<List<DataVertex>>();
            this.SequenceGraphArea.Loaded += this.SequenceGraphArea_Loaded;
            this.SequenceGraphArea.VertexSelected += this.VertexSelectedEvent;
        }

        private void GenerateSequenceGraph()
        {
            var sequenceGraph = this.Protein.SequenceGraph as GraphXSequenceGraph;
            if (this.Protein == null || this.Protein.SequenceGraph == null || sequenceGraph == null)
            {
                return;
            }

            var graph = sequenceGraph.DataGraph;
            this.Graph = graph;
            this.SequenceGraphArea.LogicCore.Graph = graph;
            this.SequenceGraphArea.ShowAllEdgesLabels();
            this.SequenceGraphArea.GenerateGraph(true);
        }

        private void SequenceGraphArea_Loaded(object sender, RoutedEventArgs e)
        {
            this.SequenceGraphArea.ShowAllEdgesLabels();
            this.SequenceGraphArea.GenerateGraph(true);
        }

        private void VertexSelectedEvent(object sender, VertexSelectedEventArgs args)
        {
            var sequenceGraph = this.Protein.SequenceGraph as GraphXSequenceGraph;
            if (sequenceGraph == null) return;
            var graph = (DataGraph) this.SequenceGraphArea.LogicCore.Graph;
            var vertex = (DataVertex) args.VertexControl.Vertex;
            this.SelectedVertices = graph.GetAllSequencePaths(vertex, sequenceGraph.EndPoint);
        }

        private List<List<DataVertex>> SelectedVertices
        {
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

            if (this.SequenceGraphArea.LogicCore != null && this.SequenceGraphArea.LogicCore.Graph != null) logicCore.Graph = this.SequenceGraphArea.LogicCore.Graph;
            this.SequenceGraphArea.LogicCore = logicCore;
            this.SequenceGraphArea.GenerateGraph(true);
        }

        #region SelectedSequence Dependency Property
        public Sequence SelectedSequence
        {
            get { return (Sequence)this.GetValue(SelectedSequenceProperty); }
            set { this.SetValue(SelectedSequenceProperty, value); }
        }

        public static readonly DependencyProperty SelectedSequenceProperty = DependencyProperty.Register(
        "SelectedSequence", typeof(Sequence), typeof(SequenceGraph), new PropertyMetadata(null));
        #endregion

        #region PrSm Dependency Property
        public PrSm PrSm
        {
            get { return (PrSm)this.GetValue(PrSmProperty); }
            set { this.SetValue(PrSmProperty, value); }
        }

        public static readonly DependencyProperty PrSmProperty = DependencyProperty.Register(
            "PrSm", typeof(PrSm), typeof(SequenceGraph),
            new FrameworkPropertyMetadata(OnPrSmPropertyChanged));

        private static void OnPrSmPropertyChanged(DependencyObject source,
            DependencyPropertyChangedEventArgs e)
        {
/*            var sequenceGraph = (SequenceGraph)source;
            var graph = (DataGraph) sequenceGraph.SequenceGraphArea.LogicCore.Graph;
            if (graph == null || graph.VertexCount == 0 || sequenceGraph.PrSm == null) return;
            var endPoint = sequenceGraph.Protein.SequenceGraph.EndPoint;
            if (endPoint == null) return;
            sequenceGraph.SelectedSequence = new Sequence(new List<AminoAcid>());
            var path = graph.GetSequencePath(endPoint, sequenceGraph.PrSm.Sequence);
            var selectedVertices = new List<List<DataVertex>>
            {
                path
            };
            sequenceGraph.SelectedVertices = selectedVertices;*/
//            sequenceGraph.GenerateSequenceGraph();
        }

        #endregion

        #region Protein Dependency Property
        public ProteinId Protein
        {
            get { return (ProteinId) this.GetValue(ProteinProperty); }
            set { this.SetValue(ProteinProperty, value); }
        }

        public static readonly DependencyProperty ProteinProperty = DependencyProperty.Register(
            "Protein", typeof (ProteinId), typeof (SequenceGraph),
            new FrameworkPropertyMetadata(OnProteinPropertyChanged));

        private static void OnProteinPropertyChanged(DependencyObject source,
            DependencyPropertyChangedEventArgs e)
        {
            var sequenceGraph = (SequenceGraph) source;
            sequenceGraph.SelectedSequence = new Sequence(new List<AminoAcid>());
            sequenceGraph.GenerateSequenceGraph();
        }

        #endregion

        private List<List<DataVertex>> _selectedVertices; 
    }
}