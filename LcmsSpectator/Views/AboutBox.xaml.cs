// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AboutBox.xaml.cs" company="Pacific Northwest National Laboratory">
//   2015 Pacific Northwest National Laboratory
// </copyright>
// <author>Christopher Wilkins</author>
// <summary>
//   Interaction logic for AboutBox.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace LcmsSpectator.Views
{
    using System.IO;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    
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
            this.InitializeComponent();
            this.LogoPictureBox.Source = this.CompanyLogo();
            this.WindowAboutBox.Title = string.Format("About {0}", this.AssemblyTitle);
            this.LabelProductName.Content = this.AssemblyProduct;
            this.LabelVersion.Content = string.Format("Version {0}", this.AssemblyVersion);
            this.LabelCopyright.Content = this.AssemblyCopyright;
            this.LabelCompanyName.Content = this.AssemblyCompany;
            this.TextBoxDescription.Text = this.AssemblyDescription;
        }

        /// <summary>
        /// Gets the assembly title.
        /// </summary>
        public string AssemblyTitle
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
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
        public string AssemblyVersion
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }

        /// <summary>
        /// Gets the assembly description.
        /// </summary>
        public string AssemblyDescription
        {
            get
            {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
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
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
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
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
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
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
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
            using (Stream image = Assembly.GetExecutingAssembly().GetManifestResourceStream("LcmsSpectator.Resources.PNNL_Logo.jpg"))
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
