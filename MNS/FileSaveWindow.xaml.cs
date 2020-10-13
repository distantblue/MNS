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
            this.Close();
        }

        private void SaveDataFile_button_Click(object sender, RoutedEventArgs e)
        {
            // СОЗДАНИЕ ОТНОСИТЕЛЬНОГО ПУТИ СОХРАНЕНИЯ ФАЙЛА
            StringBuilder stringBuilder = new StringBuilder();
            StringBuilder pathStringBuilder = new StringBuilder();
            pathStringBuilder.Append(Directory.GetCurrentDirectory());
            pathStringBuilder.Append(@"\");
            pathStringBuilder.Append(DataManager.DataDirectoryName);
            pathStringBuilder.Append(@"\");
            pathStringBuilder.Append(DataManager.DataFileName);
            pathStringBuilder.Append("_");
            pathStringBuilder.Append(DateTime.Now.ToString(("dd_MM_yyyy_hh-mmtt")));
            string filePath = pathStringBuilder.ToString();

            // КОНФИГУРИРОВАНИЕ SaveFileDialog

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Сохранение массива измерянных данных";
            saveFileDialog.FileName = $"{filePath}";
            saveFileDialog.InitialDirectory = $"{filePath}";
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.DefaultExt = "csv";
            saveFileDialog.AddExtension = true;
            if (saveFileDialog.ShowDialog() == true)
            {
                // ОСТАНОВКА ИЗМЕРЕНИЯ
                MainWindow.Stop_measurement();

                // КОПИРОВАНИЕ ФАЙЛА "Data.csv" ИЗ ПАПКИ "Temp"
                File.Copy($"{DataManager.TempDirectoryName}" + @"\" + $"{DataManager.TempDataFileName}" + "." + $"{DataManager.DataFileExt}", saveFileDialog.FileName, true);

                // ЗАКРЫТИЕ ОКНА "FileSaveWindow"
                this.Close();
                MainWindow.DataToSaveExists = false;
            }
            else
            {
                this.Close();
            }
        }
    }
    
}
