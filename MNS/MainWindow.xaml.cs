using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ModbusRTUSettings CurrentModbusRTUSettings;
        SerialPort SerialPort;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void StartMeasuring_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Получаем ТЕКУЩИЕ НАСТРОЙКИ связи с устройством Modbus
            GetCurrentModbusRTUSettings();

            //СОЗДАНИЕ ТАЙМЕРА который будет запускать метод "Measure()"
            TimerCallback timerCallback = new TimerCallback(Measure); // функция обратного вызова метода Measure()
            Timer timer = new Timer(timerCallback, null, 0, ModbusRTUSettings.PollingInterval); // 
            
        }
        
        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        private void Measure(object state)
        {
            //СТАТУС ПРИБОРА - обращение к регистру статуса "200" - 16 бит
            ModbusRTU modbusMeasuring = new ModbusRTU();
            byte[] MeasuringMessage = modbusMeasuring.BuildModbusMessage(0x09, 0x03, 200, 1);
            modbusMeasuring.SendModbusMessage(MeasuringMessage, SerialPort);
        }

        private void GetCurrentModbusRTUSettings()
        {
            //ТЕКУЩИЕ НАСТРОЙКИ связи с устройством Modbus
            CurrentModbusRTUSettings = ModbusRTUSettings.GetCurrentSettings(ModbusRTUSettings.ModbusRTUSettingsFilePath); // получаем текущие настройки подключения
            SerialPort = new SerialPort(CurrentModbusRTUSettings.PortName, ModbusRTUSettings.BaudRate, ModbusRTUSettings.Parity, ModbusRTUSettings.DataBits, ModbusRTUSettings.StopBits); // конфигурируем COM-порт
        }
    }
}
