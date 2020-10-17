using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Логика взаимодействия для AboutAppWindow.xaml
    /// </summary>
    public partial class AboutAppWindow : Window
    {
        public AboutAppWindow()
        {
            InitializeComponent();
            Loaded += AboutAppWindow_Loaded;
        }

        private void AboutAppWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ОТОБРАЖАЕМ ИНФОРМАЦИЮ О СБОРКЕ - ASSEMBLY INFO
            assemblyTitle_label.Content = ((AssemblyTitleAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyTitleAttribute), false)).Title;
            assemblyVersion_label.Content = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            assemblyDescription_label.Content = ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyDescriptionAttribute), false)).Description;
            assemblyProduct_label.Content = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute), false)).Product;
            assemblyCompany_label.Content = ((AssemblyCompanyAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCompanyAttribute), false)).Company;
            assemblyCopyright_label.Content = ((AssemblyCopyrightAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false)).Copyright;
        }
    }
}
