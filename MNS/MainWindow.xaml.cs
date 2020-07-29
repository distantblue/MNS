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
            Settings CurrentSettings = Settings.GetCurrentSettings(Settings.SettingsFilePath); // получаем текущие настройки подключения
            SerialPort serialPort = new SerialPort(CurrentSettings.PortName, Settings.BaudRate, Settings.Parity, Settings.DataBits, Settings.StopBits); // конфигурируем COM-порт

            ModbusRTU modbusMeasuring = new ModbusRTU();
            byte[] MeasuringMessage = modbusMeasuring.BuildModbusMessage(0x09, 0x03, 200, 1);
            modbusMeasuring.SendModbusMessage(MeasuringMessage, serialPort);
        }
        
        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }
    }
}
