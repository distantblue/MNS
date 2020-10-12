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
            this.Loaded += FileSaveWindow_Loaded; // Загружено и отрисовано окно
            this.Closing += FileSaveWindow_Closing; // При закрытии окна
            this.Closed += FileSaveWindow_Closed; // Окно закрыто
            this.Unloaded += FileSaveWindow_Unloaded; // Окно закрыто и освобождены все ресурсы
        }

        private void FileSaveWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void FileSaveWindow_Closing(object sender, CancelEventArgs e)
        {

        }

        private void FileSaveWindow_Closed(object sender, EventArgs e)
        {

        }

        private void FileSaveWindow_Unloaded(object sender, RoutedEventArgs e)
        {

        }

        private void CancelSavingDataFile_button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            MainWindow.Close_program();
            DataManager.ClearTempDirectory();
        }

        private void SaveDataFile_button_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            StringBuilder stringBuilder = new StringBuilder();
            StringBuilder pathStringBuilder= new StringBuilder();
            pathStringBuilder.Append(Directory.GetCurrentDirectory());
            pathStringBuilder.Append(@"\");
            pathStringBuilder.Append(DataManager.DataFileName);

            string filePath = pathStringBuilder.ToString();
            saveFileDialog.Title = "Сохранение массива измерянных данных";
            saveFileDialog.FileName = $"{filePath}";
            saveFileDialog.ShowDialog();

        }
    }
}
