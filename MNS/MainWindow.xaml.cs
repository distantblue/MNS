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
        //ПОЛУЧАЕМ ТЕКУЩИЕ НАСТРОЙКИ связи с устройством Modbus
        ModbusRTUSettings CurrentModbusRTUSettings;

        //СОЗДАНИЕ ОБЪЕКТА ModbusRTU
        ModbusRTU Modbus;

        //ПЕРЕМЕННАЯ которая хранит СТАТУС ПРИБОРА
        //ushort SlaveState; 

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            ModbusRTUSettings.SettingsFileNotFoundError += this.ShowSettingsError; // Подписываемся на событие "не найден файл настроек"
            ModbusRTUSettings.SettingsFileReadingError += this.ShowSettingsError; // Подписываемся на событие "ошибка при чтении файла настроек"

            CurrentModbusRTUSettings = new ModbusRTUSettings(); // Создаем объект настроек
            Modbus = new ModbusRTU(CurrentModbusRTUSettings); // Создаем объект ModbusRTU
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void StartMeasuring_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ RespodReceived
            Modbus.ResponseReceived += this.ShowRes;

            Modbus.SendRequestToSlaveDeviceToReceiveData(0x09, 0x03, 200, 1);
            
            //СОЗДАНИЕ ТАЙМЕРА который будет запускать метод "Measure()"
            //TimerCallback timerCallback = new TimerCallback(Measure); //функция обратного вызова метода Measure()
            //Timer timer = new Timer(timerCallback, null, 0, CurrentModbusRTUSettings.PollingInterval * 1000);
        }

        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        /*

        public void SendCommand(byte functionCode, ushort register, ushort quantityOfRegisters)
        {
            byte[] slaveStateMessage = modbus.BuildModbusMessage(ModbusRTUSettings.ModbusSlaveAddress, functionCode, register, quantityOfRegisters); //ФОРМИРОВАНИЕ СООБЩЕНИЯ
            modbus.SendModbusMessage(slaveStateMessage); //ОТПРАВКА ЗАПРОСА
        }
        */

        private void GetSlaveState()
        {
            //ushort SlaveState = 0xFFFF; //начальная инициализация значением 11111111 11111111;
            Modbus.SendRequestToSlaveDeviceToReceiveData(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 200, 1); //команда (0x03) на чтение 200-го регистра статуса, считываем 1 регистр
            //return SlaveState;
        }

        private void ShowRes(byte[] buffer)
        {
            Console.WriteLine("Received modbus message from the slave device:");
            foreach (var item in buffer)
            {
                Console.WriteLine(item);
            }
        }

        private void ShowSettingsError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Ошибка!");
        }

        private void Measure(object obj)
        {
            //СТАТУС ПРИБОРА - обращение к регистру статуса "200" - 16 бит
            //ModbusRTU modbusMeasuring = new ModbusRTU();
            //byte[] MeasuringMessage = modbusMeasuring.BuildModbusMessage(ModbusRTUSettings.ModbusSlaveAddress, 0x03, 200, 1);
            //modbusMeasuring.SendModbusMessage(MeasuringMessage);

        }
    }
}
