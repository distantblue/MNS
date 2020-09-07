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

        // СТАТУС ПРИБОРА
        ushort SlaveState;

        // СХЕМА ЗАМЕЩЕНИЯ
        string EquivalentCircuit;

        // НАЛИЧИЕ ИНТЕГРИРОВАНИЯ
        string Integration;

        // НАЛИЧИЕ УСРЕДНЕНИЯ
        string Averaging;

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

            // Создаем функцию обратного вызова по таймеру
            TimerCallback tm = new TimerCallback(GetSlaveState);
            Timer timer = new Timer(tm, null, 0, CurrentModbusRTUSettings.PollingInterval * 1000);
        }

        private void GetSlaveState(object obj)
        {
            //ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ ResposeReceived
            Modbus.ResponseReceived += this.IdentifyStatus;

            Modbus.SendRequestToSlaveDeviceToReceiveData(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 200, 1); //команда (0x03) на чтение 200-го регистра статуса, считываем 1 регистр
        }

        private void IdentifyStatus(byte[] buffer)
        {
            //ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.IdentifyStatus;

            // Получаем 16 битное значение состояния прибора
            this.SlaveState = BitConverter.ToUInt16(new byte[2] { buffer[3], buffer[4] }, 0);

            // Узнаем схему замещения
            ushort equivalentCircuit = (ushort)(SlaveState & 0x8); // Накладываем битовую маску 00000000 00001000 чтобы получить значение 4го бита
            switch (equivalentCircuit)
            {
                case 8:
                    this.EquivalentCircuit = "Посл.";
                    break;
                case 0:
                    this.EquivalentCircuit = "Парал.";
                    break;
            }
            // Узнаем наличие интегрирования
            ushort integrationValue = (ushort)(SlaveState & 0x10); // Накладываем битовую маску 00000000 00010000 чтобы получить значение 5го бита 
            switch (integrationValue)
            {
                case 16:
                    this.Integration = "Вкл.";
                    break;
                case 0:
                    this.Integration = "Выкл.";
                    break;
            }
            // Узнаем наличие усреднения
            ushort averagingValue = (ushort)(SlaveState & 0x200); // Накладываем битовую маску 00000010 00000000 чтобы получить значение 9го бита 
            switch (averagingValue)
            {
                case 512:
                    this.Averaging = "Вкл.";
                    break;
                case 0:
                    this.Averaging = "Выкл.";
                    break;
            }
            // Узнаем основной индицируемы канал
            switch ((ushort)(SlaveState >> 14))
            {
                case 0: // канал R
                    break;
                case 1: // канал L
                    break;
                case 2: // канал C
                    break;
                case 3: // канал M
                    break;
            }

        }

        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }



        /*
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
            
        }
        */

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
