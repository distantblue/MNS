using System;
using System.Collections.Generic;
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
            MailAddress mailAddress = new MailAddress("metrology@protonmail.com","Oleksii Stennik");
            email_label.Content = mailAddress.ToString();

            Uri uri = new Uri("www.facebook.com/user");
            facebook_label.Content = uri.ToString();

            BitmapImage facebookPicture = new BitmapImage();
            facebookPicture.BeginInit();
            facebookPicture.UriSource = new Uri("www.facebook.com/usericon.jpg");
            facebookPicture.EndInit(); 
            facebookPicture_image.Source = facebookPicture;
        }
    }
}
