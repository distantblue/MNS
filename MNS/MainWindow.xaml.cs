using System;
using System.Collections.Generic;
using System.IO; // убрать потом если не будет filestream
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
using System.Windows.Threading;

namespace MNS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ПОЛУЧАЕМ ТЕКУЩИЕ НАСТРОЙКИ связи с устройством Modbus
        ModbusRTUSettings CurrentModbusRTUSettings;

        //СОЗДАНИЕ ОБЪЕКТА ModbusRTU
        ModbusRTU Modbus;

        //Thread MeasurementThread;

        //bool cancelMeasurementThread = true;

        //ПЕРЕМЕННАЯ которая хранит СТАТУС ПРИБОРА
        //ushort SlaveState; 

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MainWindow_Closing(object sender, RoutedEventArgs e)
        {
            //cancelMeasurementThread = true;
            //MeasurementThread.Abort();
        }

        private void StopMeasurement_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //cancelMeasurementThread = true;
            //MeasurementThread.Abort();
        }

        private void StartMeasurement_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CurrentModbusRTUSettings = new ModbusRTUSettings(); //Создаем объект настроек
            CurrentModbusRTUSettings.SettingsFileNotFoundError += this.ShowError; //Подписываемся на событие "не найден файл настроек" 
            CurrentModbusRTUSettings.SettingsFileReadingError += this.ShowError; //Подписываемся на событие "ошибка при чтении файла настроек"

            CurrentModbusRTUSettings.GetCurrentSettings(); //Считываем настройки из файла настроек

            Modbus = new ModbusRTU(CurrentModbusRTUSettings); //Создаем объект ModbusRTU

            Modbus.BadSignalError += this.ShowError; // Подписываемся на событие "Помехи в линии" 
            Modbus.DeviceNotRespondingError += this.ShowError; //Подписываемся на событие "Устройство не отвечает" 
            Modbus.SerialPortOpeningError += this.ShowError; //Подписываемся на событие "Ошибка открытия порта" 

            //ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ ResposeReceived
            Modbus.ResponseReceived += this.ShowRes;


            //СОЗДАНИЕ ПОТОКА в котором будет запускать метод "Measure()"
            //MeasurementThread = new Thread(Measure);

            //MeasurementThread.Start();
            GetSlaveState();
            ..Thread.Sleep()
        }

        private void Measure()
        {
            GetSlaveState();

            //cancelMeasurementThread = false;

            //TimerCallback tm = new TimerCallback(GetSlaveState);
            //Timer timer = new Timer(tm, null, 0, CurrentModbusRTUSettings.PollingInterval*1000);

        }

        private void GetSlaveState()
        {
            Modbus.SendRequestToSlaveDeviceToReceiveData(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 200, 1); //команда (0x03) на чтение 200-го регистра статуса, считываем 1 регистр
        }

        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        private void ShowRes(byte[] buffer)
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI

            if (statusTextBlock.CheckAccess()) // Поток иеет доступ к потоку UI         
            {
                statusTextBlock.Text = BitConverter.ToString(buffer);
            }
            else //Поток не имеет доступ к потоку UI 
            {
                statusTextBlock.Dispatcher.InvokeAsync(() => statusTextBlock.Text = BitConverter.ToString(buffer));
            }

            //this.Dispatcher.BeginInvoke(DispatcherPriority.Send, (ThreadStart)delegate () { statusTextBlock.Text = BitConverter.ToString(buffer); });
            //statusTextBlock.Text = BitConverter.ToString(buffer);
            /*
            using (StreamWriter sw = new StreamWriter(@"Answer.txt", true, System.Text.Encoding.Default))
            {
                string str = "";
                foreach (var item in buffer)
                {
                    str = str + item.ToString();
                }
                sw.WriteLine(str);
            }
            */
        }

        private void SettingsButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void ShowError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Ошибка!");
        }
    }
}
