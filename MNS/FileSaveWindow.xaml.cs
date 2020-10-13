using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
        MainWindow MainWindow;

        public FileSaveWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.MainWindow = mainWindow;

            // ДОБАВЛЯЕМ ОБРАБОТЧИКИ СОБЫТИЙ 
            this.Closing += FileSaveWindow_Closing; // При закрытии окна
        }
        
        private void FileSaveWindow_Closing(object sender, CancelEventArgs e)
        {

        }
        
        private void CancelSavingDataFile_button_Click(object sender, RoutedEventArgs e)
        {
            
            MainWindow.Close_program();
            DataManager.ClearTempDirectory();
            this.Close();
        }

        private void SaveDataFile_button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            //saveFileDialog.Filter = "*.csv";

            StringBuilder stringBuilder = new StringBuilder();
            StringBuilder pathStringBuilder= new StringBuilder();
            pathStringBuilder.Append(Directory.GetCurrentDirectory());
            pathStringBuilder.Append(@"\");
            pathStringBuilder.Append(DataManager.DataFilePath);
            pathStringBuilder.Append(@"\");
            pathStringBuilder.Append(DataManager.DataFileName);
            pathStringBuilder.Append("_");
            pathStringBuilder.Append(DateTime.UtcNow.ToString(("MM_dd_yyyy_h-mmtt")));

            string filePath = pathStringBuilder.ToString();
            saveFileDialog.Title = "Сохранение массива измерянных данных";
            saveFileDialog.FileName = $"{filePath}";
            saveFileDialog.ShowDialog();

        }
    }
}
