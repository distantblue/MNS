using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
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
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //ТЕКУЩИЕ НАСТРОЙКИ
            Settings CurrentSettings = null;
            try
            {
                CurrentSettings = Settings.GetCurrentSettings(Settings.SettingsFilePath); // получаем текущие настройки подключения
            }
            catch (FileNotFoundException exception)
            {
                MessageBox.Show("В директории где расположен исполняемый файл программы отсутствует файл настроек Settings.dat!" + "\n\n" + "Exception message: " + exception.Message);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Возникла ошибка при попытке считать настройки подключения программы!" + "\n\n" + "Exception message: " + exception.Message);
            }
            currentSerialPort_label.Content = CurrentSettings.PortName; // отображаем текущий порт в окне настроек
            currentDeviceAddress_label.Content = "0x"+Settings.ModbusSlaveAddress.ToString("x"); // отображаем текущий адрес устройства
            currentPollingInterval_label.Content = CurrentSettings.PollingInterval; // отображаем текущий интервал опроса

            //ЗАПОЛНЕНИЕ НАСТРОЕК ДЛЯ ВОЗМОЖНОСТИ ИЗМЕНЕНИЯ
            string[] serialPortNames = SerialPort.GetPortNames(); // получаем массив доступных COM-портов на ПК
            int[] pollingIntervalRange = new int[180]; // получаем максимальный интервал опроса в 180 секунд
            for (int i = 0; i < 180; i++)
            {
                pollingIntervalRange[i] = i + 1;
            }
            portName_ComboBox.ItemsSource = serialPortNames; // заполняем ComboBox доступными COM портами 
            pollingInterval_ComboBox.ItemsSource = pollingIntervalRange; // заполняем ComboBox от 1 до 180
        }

        private void SettingsButtonSave_Click(object sender, RoutedEventArgs e)
        {
            if (portName_ComboBox.Text == "" && pollingInterval_ComboBox.Text == "")
            {
                MessageBox.Show("Вы не выбрали COM-порт и интервал опроса устройства");
            }
            if (portName_ComboBox.Text == "" && pollingInterval_ComboBox.Text != "")
            {
                MessageBox.Show("Вы не выбрали COM-порт");
            }
            if (pollingInterval_ComboBox.Text == "" && portName_ComboBox.Text != "")
            {
                MessageBox.Show("Вы не выбрали интервал опроса устройства");
            }
            if (portName_ComboBox.Text != "" && pollingInterval_ComboBox.Text != "")
            {
                Settings newSettings = new Settings(portName_ComboBox.Text, int.Parse(pollingInterval_ComboBox.Text));
                try
                {
                    Settings.SaveSettings(newSettings, Settings.SettingsFilePath);
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Возникла ошибка при попытке сохранения настроек подключения устройства!" + "\n\n" + "Exception message: " + exception.Message);
                }
                ;
                MessageBox.Show("Настройки успешно сохранены!");
                this.Close();
            }
        }

        private void SettingsButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
