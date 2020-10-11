using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Логика взаимодействия для FileSaveWindow.xaml
    /// </summary>
    public partial class FileSaveWindow : Window
    {
        public delegate void SaveFileHandler();
        public event SaveFileHandler SavingCanceled;
        public event SaveFileHandler SavingProcess;

        public FileSaveWindow()
        {
            InitializeComponent();

        }

        private void CancelSavingDataFile_button_Click(object sender, RoutedEventArgs e)
        {
            DataManager.ClearTempDirectory();
            SavingCanceled?.Invoke();
        }
    }
}
