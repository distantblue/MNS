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
        ModbusRTU modbus;// = new ModbusRTU(CurrentModbusRTUSettings);

        //ПЕРЕМЕННАЯ котора будет хранить СТАТУС ПРИБОРА
        ushort SlaveState; 
        
        //ModbusRTU.ModbusHandler += Method(); //; //ПОДПИСЫВАЕМСЯ НА СОБЫТИЕ

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            CurrentModbusRTUSettings = ModbusRTUSettings.GetCurrentSettings(ModbusRTUSettings.ModbusRTUSettingsFilePath);
            modbus = new ModbusRTU(CurrentModbusRTUSettings);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void StartMeasuring_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            //ПОЛУЧАЕМ СТАТУС ПРИБОРА если он подключен
            GetSlaveState();

            //СОЗДАНИЕ ТАЙМЕРА который будет запускать метод "Measure()"
            TimerCallback timerCallback = new TimerCallback(Measure); //функция обратного вызова метода Measure()
            Timer timer = new Timer(timerCallback, null, 0, CurrentModbusRTUSettings.PollingInterval * 1000);
        }

        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        public void SendCommand(byte functionCode, ushort register, ushort quantityOfRegisters)
        {
            byte[] slaveStateMessage = modbus.BuildModbusMessage(ModbusRTUSettings.ModbusSlaveAddress, functionCode, register, quantityOfRegisters); //ФОРМИРОВАНИЕ СООБЩЕНИЯ
            modbus.SendModbusMessage(slaveStateMessage); //ОТПРАВКА ЗАПРОСА
        }

        private void GetSlaveState()
        {
            //ushort SlaveState = 0xFFFF; //начальная инициализация значением 11111111 11111111;
            SendCommand(0x03, 200, 1); //команда (0x03) на чтение 200-го регистра статуса, считываем 1 регистр
            //return SlaveState;
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
