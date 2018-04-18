// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColorList.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for ColorList.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views
{
    using System.Windows.Controls;

    /// <summary>
    /// Interaction logic for ColorList.xaml
    /// </summary>
    public partial class ColorList : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColorList"/> class.
        /// </summary>
        public ColorList()
        {
            InitializeComponent();

            SelectPaletteButton.Click += (o, e) =>
            {
                if (SelectPaletteButton.ContextMenu != null)
                    SelectPaletteButton.ContextMenu.IsOpen = true;
            };
        }
    }
}
