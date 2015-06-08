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
            this.InitializeComponent();

            this.SelectPaletteButton.Click += (o, e) =>
                {
                    this.SelectPaletteButton.ContextMenu.IsOpen = true;
                };
        }
    }
}
