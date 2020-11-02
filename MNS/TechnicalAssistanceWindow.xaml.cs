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
            BitmapImage facebookPicture = new BitmapImage();
            facebookPicture.BeginInit();
            facebookPicture.UriSource = new Uri("https://m.facebook.com/photo.php?fbid=272945110594059&id=100036357166804&set=a.107164550505450&source=11&refid=17");
            facebookPicture.EndInit(); 
            facebookPicture_image.Source = facebookPicture;
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
