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
        // ТЕКУЩИЕ НАСТРОЙКИ связи с устройством Modbus
        ModbusRTUSettings CurrentModbusRTUSettings;

        // ОБЪЯВЛЕНИЕ ОБЪЕКТА ModbusRTU
        ModbusRTU Modbus;

        // ТАЙМЕР ПО КОТОРОМУ ВЫЗЫВАЕТСЯ ИЗМЕРЕНИЕ
        Timer Timer;

        // СТРОКА СОСТОЯНИЯ
        string[] ConsoleText;

        // СТАТУС ПРИБОРА
        ushort SlaveState;

        // СХЕМА ЗАМЕЩЕНИЯ
        string EquivalentCircuit;

        // НАЛИЧИЕ ИНТЕГРИРОВАНИЯ
        string Integration;

        // НАЛИЧИЕ УСРЕДНЕНИЯ
        string Averaging;

        // ИНТЕРВАЛ ДИАПАЗОНА ИЗМЕРЕНИЯ
        string FixedMeasInterval;

        // Сопротивление
        float Resistance;

        // Тангенс R
        float tg_R;

        // Частота
        float Frequency;

        // Индуктивность
        float Inductance;

        // Тангенс L
        float tg_L;

        // Емкость
        float Сapacity;

        // Тангенс С
        float tg_C;

        // Взаимоиндуктивность
        float MutualInductance;

        // Тангенс M
        float tg_M;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            ConsoleText = new string[10];
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MainWindow_Closing(object sender, RoutedEventArgs e)
        {
            Timer.Change(Timeout.Infinite, 0); // Приостанавливаем измерение
            Modbus.Close(); // Закрываем COM порт 
        }

        private void StopMeasurement_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Modbus != null)
            {
                // ОТПИСЫВАЕМСЯ ОТ ОБРАБОТЧИКОВ СОБЫТИЯ ResposeReceived
                Modbus.ResponseReceived -= this.IdentifyStatus;
                Modbus.ResponseReceived -= this.Get_R;
                Modbus.ResponseReceived -= this.Get_L;
                Modbus.ResponseReceived -= this.Get_C;
                Modbus.ResponseReceived -= this.Get_M;
                Modbus.ResponseReceived -= this.Get_F;
                Modbus.ResponseReceived -= this.Get_tgR;
                Modbus.ResponseReceived -= this.Get_tgL;
                Modbus.ResponseReceived -= this.Get_tgC;
                Modbus.ResponseReceived -= this.Get_tgM;
                Modbus.ResponseReceived -= this.DisplayMessageInConsole;
                Modbus.RequestSent -= this.DisplayMessageInConsole;
                Modbus.CRC_Error -= this.ProcessMissedResult;
                Modbus.SlaveError -= this.ProcessMissedResult;
                Modbus.DeviceNotRespondingError -= this.ProcessMissedResult;

                // ПРИОСТАНАВЛИВАЕМ ТАЙМЕР
                Timer.Change(Timeout.Infinite, 0); // Приостанавливаем вызов метода GetSlaveState
                Thread.Sleep(200);

                Modbus.Close(); // Закрываем COM порт
                Modbus = null; // Ссылка в null
            }
        }

        private void StartMeasurement_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (Modbus == null)
            {
                CurrentModbusRTUSettings = new ModbusRTUSettings(); // Создаем объект настроек
                CurrentModbusRTUSettings.SettingsFileNotFoundError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "не найден файл настроек" 
                CurrentModbusRTUSettings.SettingsFileReadingError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "ошибка при чтении файла настроек"

                CurrentModbusRTUSettings.GetCurrentSettings(); // Считываем настройки из файла настроек

                Modbus = new ModbusRTU(CurrentModbusRTUSettings); // Создаем объект ModbusRTU

                //Modbus.DeviceNotRespondingError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "Устройство не отвечает" 
                Modbus.SerialPortOpeningError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "Ошибка открытия порта"
                Modbus.RequestSent += this.DisplayMessageInConsole; // Подписываемся на обработчик события "Отправлена команда"
                Modbus.ResponseReceived += this.DisplayMessageInConsole; // Подписываемся на обработчик события "Получен ответ"
                Modbus.CRC_Error += this.ProcessMissedResult;
                Modbus.SlaveError += this.ProcessMissedResult;
                Modbus.DeviceNotRespondingError += this.ProcessMissedResult;

                // Создаем функцию обратного вызова по таймеру
                Timer = new Timer(new TimerCallback(GetSlaveState), null, 0, CurrentModbusRTUSettings.PollingInterval * 1000);
            }
        }

        private void GetSlaveState(object obj)
        {
            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.IdentifyStatus;

            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 200, 1, 2); // Команда (0x03) на чтение 200-го регистра статуса, считываем 1 регистр 16 бит
        }

        private void IdentifyStatus(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.IdentifyStatus;

            // Получаем 16 битное значение состояния прибора
            this.SlaveState = BitConverter.ToUInt16(new byte[2] { buffer[4], buffer[3] }, 0);

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
            // Узнаем фиксирован ли интервал диапазона измерения
            ushort fixedMeasIntervalValue = (ushort)(SlaveState & 0x100); // Накладываем битовую маску 00000001 00000000 чтобы получить значение 9го бита 
            switch (fixedMeasIntervalValue)
            {
                case 256:
                    this.FixedMeasInterval = "Да";
                    break;
                case 0:
                    this.FixedMeasInterval = "Нет";
                    break;
            }
            // Узнаем основной индицируемы канал
            ushort chanelValue = (ushort)(SlaveState >> 14);
            switch (chanelValue)
            {
                // Канал R
                case 0:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_R;

                    // Отправляем запрос на чтение регистров R 
                    Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 104, 1, 4);
                    break;

                // Канал L
                case 1:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_L;

                    // Отправляем запрос на чтение регистров R 
                    Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 108, 1, 4);
                    break;

                // Канал C
                case 2:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_C;

                    // Отправляем запрос на чтение регистров R 
                    Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 112, 1, 4);
                    break;

                // Канал M
                case 3:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_M;

                    // Отправляем запрос на чтение регистров R 
                    Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 116, 1, 4);
                    break;
            }
        }

        private void Get_R(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_R;

            // Получаем значение сопротивления
            this.Resistance = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // Отображаем значение
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (statusTextBlock.CheckAccess())
            {
                R_textBlock.Text = Resistance.ToString();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                statusTextBlock.Dispatcher.InvokeAsync(() => R_textBlock.Text = Resistance.ToString());
            }

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_tgR;

            // Отправляем запрос на чтение регистра tgR
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 106, 1, 4);
        }

        private void Get_tgR(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgR;

            // Получаем значение tgR
            this.tg_R = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_F;

            // Отправляем запрос на чтение регистра F
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 120, 1, 4);
        }

        private void Get_F(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_F;

            // Получаем значение F
            this.Frequency = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);
        }

        private void Get_L(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_L;

            // Получаем значение индуктивности
            this.Inductance = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_tgL;

            // Отправляем запрос на чтение регистра tgL
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 110, 1, 4);
        }

        private void Get_tgL(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgL;

            // Получаем значение tgL
            this.tg_L = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_F;
        }

        private void Get_C(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_C;

            // Получаем значение емкости
            this.Сapacity = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_tgC;
        }

        private void Get_tgC(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgC;

            // Получаем значение емкости
            this.tg_C = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_F;
        }

        private void Get_M(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_M;

            // Получаем значение взаимоиндуктивности
            this.MutualInductance = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_tgM;
        }

        private void Get_tgM(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgM;

            // Получаем значение емкости
            this.tg_M = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_F;
        }

        private void Settings_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        private void SettingsButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DisplayErrorOccurred(string errorMessage)
        {
            Timer.Change(Timeout.Infinite, 0); // Приостанавливаем измерение

            // ОТПИСЫВАЕМСЯ ОТ ОБРАБОТЧИКОВ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.IdentifyStatus;
            Modbus.ResponseReceived -= this.Get_R;
            Modbus.ResponseReceived -= this.Get_L;
            Modbus.ResponseReceived -= this.Get_C;
            Modbus.ResponseReceived -= this.Get_M;
            Modbus.ResponseReceived -= this.Get_F;
            Modbus.ResponseReceived -= this.Get_tgR;
            Modbus.ResponseReceived -= this.Get_tgL;
            Modbus.ResponseReceived -= this.Get_tgC;
            Modbus.ResponseReceived -= this.Get_tgM;

            Modbus.ResponseReceived -= this.DisplayMessageInConsole;
            Modbus.RequestSent -= this.DisplayMessageInConsole;
            Modbus.CRC_Error -= this.ProcessMissedResult;
            Modbus.SlaveError -= this.ProcessMissedResult;
            Modbus.DeviceNotRespondingError -= this.ProcessMissedResult;
            //Modbus.DeviceNotRespondingError -= this.DisplayErrorOccurred;

            Modbus.Close(); // Закрываем COM порт

            MessageBox.Show(errorMessage, "Ошибка!");
        }

        private void DisplayMessageInConsole(byte[] message)
        {
            //byte[] message
            for (int i = 0; i < ConsoleText.Length - 1; i++)
            {
                ConsoleText[i] = ConsoleText[i + 1];
            }
            ConsoleText[ConsoleText.Length - 1] = $"    {DateTime.UtcNow}    --->    " + $"{BitConverter.ToString(message)}"; // Запись в последний элемент массива

            string displStr = "";
            foreach (var item in ConsoleText)
            {
                if (item != null)
                {
                    displStr += $"\n{item}";
                }
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (statusTextBlock.CheckAccess())
            {
                statusTextBlock.Text = displStr;
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                statusTextBlock.Dispatcher.InvokeAsync(() => statusTextBlock.Text = displStr);
            }
        }

        private void ProcessMissedResult(string errorMessage)
        {
            // ПРИОСТАНАВЛИВАЕМ ТАЙМЕР
            Timer.Change(Timeout.Infinite, 0); // Приостанавливаем вызов метода GetSlaveState

            // ОТПИСЫВАЕМСЯ ОТ ОБРАБОТЧИКОВ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.IdentifyStatus;
            Modbus.ResponseReceived -= this.Get_R;
            Modbus.ResponseReceived -= this.Get_L;
            Modbus.ResponseReceived -= this.Get_C;
            Modbus.ResponseReceived -= this.Get_M;
            Modbus.ResponseReceived -= this.Get_F;
            Modbus.ResponseReceived -= this.Get_tgR;
            Modbus.ResponseReceived -= this.Get_tgL;
            Modbus.ResponseReceived -= this.Get_tgC;
            Modbus.ResponseReceived -= this.Get_tgM;

            // ВКЛЮЧАЕМ ТАЙМЕР
            Timer.Change(100, CurrentModbusRTUSettings.PollingInterval * 1000); // Возобновляем вызов метода GetSlaveState
        }
    }
}
