using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        // НАЛИЧИЕ ФИКСИРОВАНОГО ИНТЕРВАЛА ДИАПАЗОНА ИЗМЕРЕНИЯ
        string FixedMeasInterval;

        // НОМЕР КАНАЛА ИЗМЕРЕНИЯ
        ushort ChanalNumber;

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
        float Capacity;

        // Тангенс С
        float tg_C;

        // Взаимоиндуктивность
        float MutualInductance;

        // Тангенс M
        float tg_M;

        // Флаг основного индицируемого канала
        int ChanalFlag;

        // Порядковый номер ыизмерения
        int DataRowNumber;

        // Набор данных одиночного измерения (строка)
        string DataRow;

        // Флаг - данные для сохранения существуют
        public bool DataToSaveExists;
        
        public MainWindow()
        {
            InitializeComponent();

            // ДОБАВЛЯЕМ ОБРАБОТЧИКИ СОБЫТИЙ 
            this.Loaded += MainWindow_Loaded; // Загружено и отрисовано окно
            this.Closing += MainWindow_Closing; // При закрытии окна
            this.Closed += MainWindow_Closed; // Окно закрыто
            this.Unloaded += MainWindow_Unloaded; // Окно закрыто и освобождены все ресурсы

            // ИНИЦИАЛИЗИРУЕМ ПЕРЕМЕННЫЕ
            this.ConsoleText = new string[16];
            this.DataRowNumber = 0;
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ОТОБРАЖАЕМ ПУСТЫЕ РЕЗУЛЬТАТЫ ИЗМЕРЕНИЙ
            DisplayInactiveMesResults();

            // УДАЛЯЕМ ФАЙЛ ДАННЫХ С ПРЕДЫДУЩЕГО ЗАПУСКА ПРОГРАММЫ 
            DataManager.ClearTempDirectory();
            DataManager.CreateNewDataFile();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (DataToSaveExists == true) 
            {
                e.Cancel = true; // Запрет закрытия окна

                // Открытие диалогового окна
                VisualEffects.ApplyBlurEffect(this);
                FileSaveWindow fileSaveWindow = new FileSaveWindow(this);
                fileSaveWindow.ShowDialog();
                VisualEffects.ClearBlurEffect(this);
                if (DataToSaveExists == false)
                {
                    e.Cancel = false;
                }
            }
            else
            {
                Stop_measurement();

                e.Cancel = false; // Разрешение на закрытие окна
            }
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {

        }

        private void MainWindow_Unloaded(object sender, EventArgs e)
        {

        }

        private void StopMeasurement_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Stop_measurement();
        }

        private void StartMeasurement_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Start_measurement();
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

            // ПОЛУЧАЕМ 16 БИТРОЕ ЗНАЧЕНИЕ СОСТОЯНИЯ ПРИБОРА
            this.SlaveState = BitConverter.ToUInt16(new byte[2] { buffer[4], buffer[3] }, 0);

            // УЗНАЕМ СХЕМУ ЗАМЕЩЕНИЯ ПРИБОРА
            ushort equivalentCircuit = (ushort)(SlaveState & 0x8); // Накладываем битовую маску 00000000 00001000 чтобы получить значение 4го бита
            switch (equivalentCircuit)
            {
                case 8:
                    this.EquivalentCircuit = "S";
                    break;
                case 0:
                    this.EquivalentCircuit = "P";
                    break;
            }
            // Отображаем результат
            DisplayEquivalentCircuitResult();

            // УЗНАЕМ НАЛИЧИЕ ИНТЕГРИРОВАНИЯ
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
            // Отображаем результат
            DisplayIntegrationResult();

            // УЗНАЕМ НАЛИЧИЕ УСРЕДНЕНИЯ
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
            // Отображаем результат
            DisplayAveragingResult();

            // УЗНАЕМ ФИКСИРОВАН ЛИ ИНТЕРВАЛ ДИАПАЗОНА ИЗМЕРЕНИЯ
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
            // Отображаем результат
            DisplayFixedMeasIntervalResult();

            // УЗНАЕМ НОМЕР КАНАЛА ИЗМЕРЕНИЯ
            ushort chanalNumber = (ushort)(SlaveState & 0x7); // Накладываем битовую маску 00000000 00000111 чтобы получить значение первых 3х битов
            switch (chanalNumber)
            {
                case 0:
                    this.ChanalNumber = 0;
                    break;
                case 1:
                    this.ChanalNumber = 1;
                    break;
                case 2:
                    this.ChanalNumber = 2;
                    break;
                case 3:
                    this.ChanalNumber = 3;
                    break;
                case 4:
                    this.ChanalNumber = 4;
                    break;
                case 5:
                    this.ChanalNumber = 5;
                    break;
                case 6:
                    this.ChanalNumber = 6;
                    break;
                case 7:
                    this.ChanalNumber = 7;
                    break;
            }
            // Отображаем результат
            DisplayChanalNumber();

            // УЗНАЕМ ОСНОВНОЙ ИНДИЦИРУЕМЫЙ КАНАЛ
            ushort chanalValue = (ushort)(SlaveState >> 14);
            switch (chanalValue)
            {
                // Канал R
                case 0:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_R;

                    // Включаем флаг основного индицируемого канала
                    this.ChanalFlag = 0;

                    // Отправляем запрос на чтение регистров R 
                    Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 104, 1, 4);
                    break;

                // Канал L
                case 1:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_L;

                    // Включаем флаг основного индицируемого канала
                    this.ChanalFlag = 1;

                    // Отправляем запрос на чтение регистров R 
                    Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 108, 1, 4);
                    break;

                // Канал C
                case 2:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_C;

                    // Включаем флаг основного индицируемого канала
                    this.ChanalFlag = 2;

                    // Отправляем запрос на чтение регистров R 
                    Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 112, 1, 4);
                    break;

                // Канал M
                case 3:
                    // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
                    Modbus.ResponseReceived += this.Get_M;

                    // Включаем флаг основного индицируемого канала
                    this.ChanalFlag = 3;

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

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ R
            Display_R();

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

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ tgR
            Display_tgR();

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

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ F
            Display_F();

            DataRowNumber++; // Увеличиваем значение счетчика порядкового номера измерения

            // СОСТАВЛЯЕМ И ЗАПИСЫВАЕМ СТРОКУ ДАННЫХ В ФАЙЛ
            DataManager.SaveDataRow(CreateDataRow());

            // Указываем что данные появились
            if (DataToSaveExists == false)
            {
                DataToSaveExists = true;
            }

        }

        private void Get_L(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_L;

            // Получаем значение индуктивности
            this.Inductance = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ L
            Display_L();

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

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ tgL
            Display_tgL();

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_F;
        }

        private void Get_C(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_C;

            // Получаем значение емкости
            this.Capacity = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ C
            Display_C();

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_tgC;
        }

        private void Get_tgC(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgC;

            // Получаем значение емкости
            this.tg_C = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ tgC
            Display_tgC();

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_F;
        }

        private void Get_M(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_M;

            // Получаем значение взаимоиндуктивности
            this.MutualInductance = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ М
            Display_M();

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_tgM;
        }

        private void Get_tgM(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgM;

            // Получаем значение емкости
            this.tg_M = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ tgМ
            Display_tgM();

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

            Modbus.ResponseReceived -= this.DisplayResponseMessageInConsole;
            Modbus.RequestSent -= this.DisplayRequestMessageInConsole;
            Modbus.CRC_Error -= this.ProcessMissedResult;
            Modbus.SlaveError -= this.ProcessMissedResult;
            Modbus.DeviceNotRespondingError -= this.ProcessMissedResult;
            //Modbus.DeviceNotRespondingError -= this.DisplayErrorOccurred;

            Modbus.Close(); // Закрываем COM порт

            MessageBox.Show(errorMessage, "Ошибка!");
        }
        
        private void DisplayRequestMessageInConsole(byte[] message)
        {
            for (int i = 0; i < ConsoleText.Length - 1; i++)
            {
                ConsoleText[i] = ConsoleText[i + 1];
            }
            ConsoleText[ConsoleText.Length - 1] = $"    {DateTime.UtcNow}    REQUEST      --->    " + $"{BitConverter.ToString(message)}"; // Запись в последний элемент массива

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

        private void DisplayResponseMessageInConsole(byte[] message)
        {
            for (int i = 0; i < ConsoleText.Length - 1; i++)
            {
                ConsoleText[i] = ConsoleText[i + 1];
            }
            ConsoleText[ConsoleText.Length - 1] = $"    {DateTime.UtcNow}    RESPONSE    --->    " + $"{BitConverter.ToString(message)}"; // Запись в последний элемент массива

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

        private void DisplayInactiveMesResults()
        {
            ValueSymbol_textBlock.Text = "X:";
            Value_textBlock.Text = "---";

            tgSymbol_textBlock.Text = "tg:";
            tg_textBlock.Text = "---";

            FSymbol_textBlock.Text = "F:";
            F_textBlock.Text = "---";

            EquivalentCircuit_textBlock.Text = "---";
            fixedMeasIntervalValue_textBlock.Text = "---";
            integrationValue_textBlock.Text = "---";
            averagingValue_textBlock.Text = "---";
            chanalNumber_textBlock.Text = "---";
        }

        private void DisplayEquivalentCircuitResult()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (EquivalentCircuit_textBlock.CheckAccess())
            {
                EquivalentCircuit_textBlock.Text = EquivalentCircuit;
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                statusTextBlock.Dispatcher.InvokeAsync(() => EquivalentCircuit_textBlock.Text = EquivalentCircuit);
            }
        }

        private void DisplayIntegrationResult()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (integrationValue_textBlock.CheckAccess())
            {
                integrationValue_textBlock.Text = Integration;
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                statusTextBlock.Dispatcher.InvokeAsync(() => integrationValue_textBlock.Text = Integration);
            }
        }

        private void DisplayAveragingResult()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (averagingValue_textBlock.CheckAccess())
            {
                averagingValue_textBlock.Text = Averaging;
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                statusTextBlock.Dispatcher.InvokeAsync(() => averagingValue_textBlock.Text = Averaging);
            }
        }

        private void DisplayFixedMeasIntervalResult()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (fixedMeasIntervalValue_textBlock.CheckAccess())
            {
                fixedMeasIntervalValue_textBlock.Text = FixedMeasInterval;
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                statusTextBlock.Dispatcher.InvokeAsync(() => fixedMeasIntervalValue_textBlock.Text = FixedMeasInterval);
            }
        }

        private void Display_R()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (ValueSymbol_textBlock.CheckAccess())
            {
                ValueSymbol_textBlock.Text = "R:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                ValueSymbol_textBlock.Dispatcher.InvokeAsync(() => ValueSymbol_textBlock.Text = "R:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (Value_textBlock.CheckAccess())
            {
                Value_textBlock.Text = Resistance.ToString() + " Ом";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                Value_textBlock.Dispatcher.InvokeAsync(() => Value_textBlock.Text = Resistance.ToString() + " Ом");
            }
        }

        private void Display_tgR()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tgSymbol_textBlock.CheckAccess())
            {
                tgSymbol_textBlock.Text = "tg" + "\u03B4:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tgSymbol_textBlock.Dispatcher.InvokeAsync(() => tgSymbol_textBlock.Text = "tg" + "\u03B4:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tg_textBlock.CheckAccess())
            {
                tg_textBlock.Text = tg_R.ToString("0.######");
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tg_textBlock.Dispatcher.InvokeAsync(() => tg_textBlock.Text = tg_R.ToString("0.######"));
            }
        }

        private void Display_F()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (F_textBlock.CheckAccess())
            {
                F_textBlock.Text = Frequency.ToString();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                F_textBlock.Dispatcher.InvokeAsync(() => F_textBlock.Text = Frequency.ToString() + " Гц");
            }
        }

        private void Display_L()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (ValueSymbol_textBlock.CheckAccess())
            {
                ValueSymbol_textBlock.Text = "L:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                ValueSymbol_textBlock.Dispatcher.InvokeAsync(() => ValueSymbol_textBlock.Text = "L:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (Value_textBlock.CheckAccess())
            {
                Value_textBlock.Text = Inductance.ToString() + " Гн";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                Value_textBlock.Dispatcher.InvokeAsync(() => Value_textBlock.Text = Inductance.ToString() + " Гн");
            }
        }

        private void Display_tgL()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tgSymbol_textBlock.CheckAccess())
            {
                tgSymbol_textBlock.Text = "tg" + "\u03C6:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tgSymbol_textBlock.Dispatcher.InvokeAsync(() => tgSymbol_textBlock.Text = "tg" + "\u03C6:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tg_textBlock.CheckAccess())
            {
                tg_textBlock.Text = tg_L.ToString();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tg_textBlock.Dispatcher.InvokeAsync(() => tg_textBlock.Text = tg_L.ToString());
            }
        }

        private void Display_C()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (ValueSymbol_textBlock.CheckAccess())
            {
                ValueSymbol_textBlock.Text = "C:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                ValueSymbol_textBlock.Dispatcher.InvokeAsync(() => ValueSymbol_textBlock.Text = "C:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (Value_textBlock.CheckAccess())
            {
                Value_textBlock.Text = Capacity.ToString() + " Ф";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                Value_textBlock.Dispatcher.InvokeAsync(() => Value_textBlock.Text = Capacity.ToString() + " Ф");
            }
        }

        private void Display_tgC()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tgSymbol_textBlock.CheckAccess())
            {
                tgSymbol_textBlock.Text = "tg" + "\u03B4:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tgSymbol_textBlock.Dispatcher.InvokeAsync(() => tgSymbol_textBlock.Text = "tg" + "\u03B4:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tg_textBlock.CheckAccess())
            {
                tg_textBlock.Text = tg_C.ToString();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tg_textBlock.Dispatcher.InvokeAsync(() => tg_textBlock.Text = tg_C.ToString());
            }
        }

        private void Display_M()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (ValueSymbol_textBlock.CheckAccess())
            {
                ValueSymbol_textBlock.Text = "M:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                ValueSymbol_textBlock.Dispatcher.InvokeAsync(() => ValueSymbol_textBlock.Text = "M:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (Value_textBlock.CheckAccess())
            {
                Value_textBlock.Text = MutualInductance.ToString();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                Value_textBlock.Dispatcher.InvokeAsync(() => Value_textBlock.Text = MutualInductance.ToString());
            }
        }

        private void Display_tgM()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tgSymbol_textBlock.CheckAccess())
            {
                tgSymbol_textBlock.Text = "tg" + "\u03C6:";
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tgSymbol_textBlock.Dispatcher.InvokeAsync(() => tgSymbol_textBlock.Text = "tg" + "\u03C6:");
            }

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (tg_textBlock.CheckAccess())
            {
                tg_textBlock.Text = tg_M.ToString();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                tg_textBlock.Dispatcher.InvokeAsync(() => tg_textBlock.Text = tg_M.ToString());
            }
        }

        private void DisplayChanalNumber()
        {
            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (chanalNumber_textBlock.CheckAccess())
            {
                chanalNumber_textBlock.Text = ChanalNumber.ToString();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                chanalNumber_textBlock.Dispatcher.InvokeAsync(() => chanalNumber_textBlock.Text = ChanalNumber.ToString());
            }
        }

        private string CreateDataRow()
        {
            string dataRow = "";

            // ЗАПИСЫВАЕМ НАБОР ДАННЫХ
            switch (this.ChanalFlag)
            {
                case 0:
                    StringBuilder stringBuilder_0 = new StringBuilder();
                    stringBuilder_0.Append(DataRowNumber + ";");
                    stringBuilder_0.Append(DateTime.UtcNow + ";");
                    stringBuilder_0.Append(EquivalentCircuit + ";");
                    stringBuilder_0.Append(Frequency + ";");
                    stringBuilder_0.Append(Resistance + ";");
                    stringBuilder_0.Append(tg_R + ";");
                    stringBuilder_0.Append(";;;;;;");
                    dataRow = stringBuilder_0.ToString();
                    break;
                case 1:
                    StringBuilder stringBuilder_1 = new StringBuilder();
                    stringBuilder_1.Append(DataRowNumber + ";");
                    stringBuilder_1.Append(DateTime.UtcNow + ";");
                    stringBuilder_1.Append(EquivalentCircuit + ";");
                    stringBuilder_1.Append(Frequency + ";");
                    stringBuilder_1.Append(";;");
                    stringBuilder_1.Append(Inductance + ";");
                    stringBuilder_1.Append(tg_L + ";");
                    stringBuilder_1.Append(";;;;");
                    dataRow = stringBuilder_1.ToString();
                    break;
                case 2:
                    StringBuilder stringBuilder_2 = new StringBuilder();
                    stringBuilder_2.Append(DataRowNumber + ";");
                    stringBuilder_2.Append(DateTime.UtcNow + ";");
                    stringBuilder_2.Append(EquivalentCircuit + ";");
                    stringBuilder_2.Append(Frequency + ";");
                    stringBuilder_2.Append(";;;;");
                    stringBuilder_2.Append(Capacity + ";");
                    stringBuilder_2.Append(tg_C + ";");
                    stringBuilder_2.Append(";;");
                    dataRow = stringBuilder_2.ToString();
                    break;
                case 3:
                    StringBuilder stringBuilder_3 = new StringBuilder();
                    stringBuilder_3.Append(DataRowNumber + ";");
                    stringBuilder_3.Append(DateTime.UtcNow + ";");
                    stringBuilder_3.Append(EquivalentCircuit + ";");
                    stringBuilder_3.Append(Frequency + ";");
                    stringBuilder_3.Append(";;;;;;");
                    stringBuilder_3.Append(MutualInductance + ";");
                    stringBuilder_3.Append(tg_M + ";");
                    dataRow = stringBuilder_3.ToString();
                    break;
            }
            return dataRow;
        }

        private void Start_measurement()
        {
            if (Modbus == null)
            {
                CurrentModbusRTUSettings = new ModbusRTUSettings(); // Создаем объект настроек
                CurrentModbusRTUSettings.SettingsFileNotFoundError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "не найден файл настроек" 
                CurrentModbusRTUSettings.SettingsFileReadingError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "ошибка при чтении файла настроек"

                CurrentModbusRTUSettings.GetCurrentSettings(); // Считываем настройки из файла настроек

                Modbus = new ModbusRTU(CurrentModbusRTUSettings); // Создаем объект ModbusRTU

                // Modbus.DeviceNotRespondingError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "Устройство не отвечает" 
                Modbus.SerialPortOpeningError += this.DisplayErrorOccurred; // Подписываемся на обработчик события "Ошибка открытия порта"
                Modbus.RequestSent += this.DisplayRequestMessageInConsole; // Подписываемся на обработчик события "Отправлена команда"
                Modbus.ResponseReceived += this.DisplayResponseMessageInConsole; // Подписываемся на обработчик события "Получен ответ"
                Modbus.CRC_Error += this.ProcessMissedResult;
                Modbus.SlaveError += this.ProcessMissedResult;
                Modbus.DeviceNotRespondingError += this.ProcessMissedResult;

                // Создаем функцию обратного вызова по таймеру
                Timer = new Timer(new TimerCallback(GetSlaveState), null, 0, CurrentModbusRTUSettings.PollingInterval * 1000);
            }
        }

        public void Stop_measurement()
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
                Modbus.ResponseReceived -= this.DisplayResponseMessageInConsole;
                Modbus.RequestSent -= this.DisplayRequestMessageInConsole;
                Modbus.CRC_Error -= this.ProcessMissedResult;
                Modbus.SlaveError -= this.ProcessMissedResult;
                Modbus.DeviceNotRespondingError -= this.ProcessMissedResult;

                // ПРИОСТАНАВЛИВАЕМ ТАЙМЕР
                Timer.Change(Timeout.Infinite, 0); // Приостанавливаем вызов метода GetSlaveState
                Thread.Sleep(200);

                Modbus.Close(); // Закрываем COM порт
                Modbus = null; // Ссылка в null

                DisplayInactiveMesResults();
            }
        }

        public void Close_program()
        {
            Stop_measurement();
            DataToSaveExists = false;
        }

        private void AboutApp_MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}