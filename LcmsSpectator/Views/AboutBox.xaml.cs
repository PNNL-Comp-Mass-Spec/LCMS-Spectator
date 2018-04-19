// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AboutBox.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for AboutBox.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LcmsSpectator.Views
{
    /// <summary>
    /// Interaction logic for AboutBox.xaml
    /// </summary>
    public partial class AboutBox : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutBox"/> class.
        /// </summary>
        public AboutBox()
        {
            InitializeComponent();
            LogoPictureBox.Source = CompanyLogo();
            WindowAboutBox.Title = string.Format("About {0}", AssemblyTitle);
            LabelProductName.Content = AssemblyProduct;
            LabelVersion.Content = string.Format("Version {0}", AssemblyVersion);
            LabelCopyright.Content = AssemblyCopyright;
            LabelCompanyName.Content = AssemblyCompany;
            TextBoxDescription.Text = AssemblyDescription;
        }

        /// <summary>
        /// Gets the assembly title.
        /// </summary>
        public string AssemblyTitle
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != string.Empty)
                    {
                        return titleAttribute.Title;
                    }
                }

                return Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
            }
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Gets the assembly description.
        /// </summary>
        public string AssemblyDescription
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                {
                    return string.Empty;
                }

                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
            }
        }

        /// <summary>
        /// Gets the product name.
        /// </summary>
        public string AssemblyProduct
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                {
                    return string.Empty;
                }

                return ((AssemblyProductAttribute)attributes[0]).Product;
            }
        }

        /// <summary>
        /// Gets the copyright text.
        /// </summary>
        public string AssemblyCopyright
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                {
                    return string.Empty;
                }

                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
            }
        }

        /// <summary>
        /// Gets the company name.
        /// </summary>
        public string AssemblyCompany
        {
            get
            {
                var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                {
                    return string.Empty;
                }

                return ((AssemblyCompanyAttribute)attributes[0]).Company;
            }
        }

        /// <summary>
        /// Get the PNNL company logo.
        /// </summary>
        /// <returns>PNNL company logo image.</returns>
        public ImageSource CompanyLogo()
        {
            ImageSource img = null;
            using (var image = Assembly.GetExecutingAssembly().GetManifestResourceStream("LcmsSpectator.Resources.PNNL_Logo.jpg"))
            {
                if (image != null)
                {
                    var imageDecoder = new JpegBitmapDecoder(image, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
                    img = imageDecoder.Frames[0];
                }
            }

            return img;
        }
    }
}
