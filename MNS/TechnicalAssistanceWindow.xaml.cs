using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MNS
{
    /// <summary>
    /// Логика взаимодействия для TechnicalAssistanceWindow.xaml
    /// </summary>
    public partial class TechnicalAssistanceWindow : Window
    {
        public TechnicalAssistanceWindow()
        {
            InitializeComponent();
            Loaded += TechnicalAssistanceWindow_Loaded;
        }

        private void TechnicalAssistanceWindow_Loaded(object sender, RoutedEventArgs e)
        {
            /*
            BitmapImage facebookPicture = new BitmapImage();
            facebookPicture.BeginInit();
            facebookPicture.UriSource = new Uri("https://scontent.fplv1-2.fna.fbcdn.net/v/t1.0-9/107378585_272945113927392_7722781237965778963_o.jpg?_nc_cat=108&ccb=2&_nc_sid=09cbfe&_nc_ohc=oGhvZte8Q_4AX9s0v0k&_nc_ht=scontent.fplv1-2.fna&oh=cc5fee0048bca2aa32cb7e3b5b5df5d7&oe=5FC64134");
            facebookPicture.EndInit(); 
            facebookPicture_image.Source = facebookPicture;
            */
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
