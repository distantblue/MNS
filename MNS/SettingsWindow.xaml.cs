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
        ModbusRTUSettings CurrentModbusRTUSettings;

        public SettingsWindow()
        {
            InitializeComponent();
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //ПРОВЕРКА ФАЙЛА НАСТРОЕК
            try
            {
                CurrentModbusRTUSettings = ModbusRTUSettings.GetCurrentSettings(ModbusRTUSettings.ModbusRTUSettingsFilePath); // получаем текущие настройки подключения
            }
            catch (FileNotFoundException exception)
            {
                MessageBox.Show("В директории где расположен исполняемый файл программы отсутствует файл настроек ModbusRTUSettings.dat" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
            }
            catch (Exception exception)
            {
                MessageBox.Show("Возникла ошибка при попытке считать настройки подключения программы" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
            }

            //ТЕКУЩИЕ НАСТРОЙКИ
            currentSerialPort_label.Content = CurrentModbusRTUSettings.PortName; // отображаем текущий порт в окне настроек
            currentDeviceAddress_label.Content = "0x"+ModbusRTUSettings.ModbusSlaveAddress.ToString("x"); // отображаем текущий адрес устройства
            currentPollingInterval_label.Content = CurrentModbusRTUSettings.PollingInterval; // отображаем текущий интервал опроса

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
            //ПРОВЕРКА НА ПУСТЫЕ ПОЛЯ НАСТРОЕК
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

            //СОХРАНЕНИЕ НАСТРОЕК
            if (portName_ComboBox.Text != "" && pollingInterval_ComboBox.Text != "")
            {
                ModbusRTUSettings newSettings = new ModbusRTUSettings(portName_ComboBox.Text, int.Parse(pollingInterval_ComboBox.Text));
                try
                {
                    ModbusRTUSettings.SaveSettings(newSettings, ModbusRTUSettings.ModbusRTUSettingsFilePath);
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Возникла ошибка при попытке сохранения настроек подключения устройства" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
                }
                ;
                MessageBox.Show("Настройки успешно сохранены");
                this.Close();
            }
        }

        private void SettingsButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
