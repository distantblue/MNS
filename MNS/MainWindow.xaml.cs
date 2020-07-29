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
        //public static string SettingsFilePath = @"Settings.dat";
        //public static ushort DeviceAddress = 0x9;

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
            //ТЕКУЩИЕ НАСТРОЙКИ COM порта
            ModbusRTUSettings CurrentSettings = ModbusRTUSettings.GetCurrentSettings(ModbusRTUSettings.SettingsFilePath); // получаем текущие настройки подключения
            SerialPort serialPort = new SerialPort(CurrentSettings.PortName, ModbusRTUSettings.BaudRate, ModbusRTUSettings.Parity, ModbusRTUSettings.DataBits, ModbusRTUSettings.StopBits); // конфигурируем COM-порт

            //СОЗДАНИЕ ТАЙМЕРА который будет запускать метод "Measure()"
            TimerCallback tm = new TimerCallback(Measure); // функция обратного вызова метода Measure()
            Timer timer = new Timer(tm, null, 0, 2000); // 
            
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
            modbusMeasuring.SendModbusMessage(MeasuringMessage, serialPort);
        }
    }
}
