using Microsoft.Win32;
using ScottPlot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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

        // РЕГИСТР ДИАПАЗОНА ИЗМЕРЕНИЯ
        ushort RangeIntervalRegister;

        // НОМЕР ИНТЕРВАЛА ИЗМЕРЕНИЯ (ПОДДИАПАЗОНА)
        int RangeIntervalNumber;

        // ИНТЕРВАЛ ДИАПАЗОНА (ОТ...ДО)
        string RangeInterval;

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

        // Коллекция R
        List<double> R_list;

        // Начальный индекс коллекции R с которого будет отрисовываться диаграммы
        int R_plot_index;

        // Коллекция времени измерений R
        List<double> R_OADate_list;

        // Тангенс R
        float tg_R;

        // Частота
        float Frequency;

        // Индуктивность
        float Inductance;

        // Коллекция L
        List<double> L_list;

        // Начальный индекс коллекции L с которого будет отрисовываться диаграммы
        int L_plot_index;

        // Коллекция времени измерений L
        List<double> L_OADate_list;

        // Тангенс L
        float tg_L;

        // Емкость
        float Capacity;

        // Коллекция C
        List<double> C_list;

        // Начальный индекс коллекции C с которого будет отрисовываться диаграммы
        int C_plot_index;

        // Коллекция времени измерений C
        List<double> C_OADate_list;

        // Тангенс С
        float tg_C;

        // Взаимоиндуктивность
        float MutualInductance;

        // Коллекция M
        List<double> M_list;

        // Начальный индекс коллекции M с которого будет отрисовываться диаграммы
        int M_plot_index;

        // Коллекция времени измерений M
        List<double> M_OADate_list;

        // Тангенс M
        float tg_M;

        // Флаг основного индицируемого канала
        int ChanalFlag;

        // Порядковый номер измерения
        int DataRowNumber;

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
            this.ConsoleText = new string[12];

            // ИНИЦИАЛИЗИРУЕМ МАССИВЫ ДАННЫХ ДЛЯ ГРАФИКОВ
            this.R_list = new List<double>();
            this.L_list = new List<double>();
            this.C_list = new List<double>();
            this.M_list = new List<double>();
            this.R_OADate_list = new List<double>();
            this.L_OADate_list = new List<double>();
            this.C_OADate_list = new List<double>();
            this.M_OADate_list = new List<double>();

            // ОТРИСОВЫВАЕМ ДИАГРАММЫ (ЧТО НЕ БУДЕТ МЕНЯТЬСЯ ПРИ РАБОТЕ ИЛИ БУДЕТ ИЗНАЧАЛЬНО)
            StylePlots();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // ОТОБРАЖАЕМ ПУСТЫЕ РЕЗУЛЬТАТЫ ИЗМЕРЕНИЙ
            DisplayInactiveMesResults();

            // УДАЛЯЕМ ФАЙЛ ДАННЫХ С ПРЕДЫДУЩЕГО ЗАПУСКА ПРОГРАММЫ 
            DataManager.ClearTempDirectory();
            DataManager.CreateNewDataFile();

            /*
            // ВРЕМЕННОЕ СЕРИАЛИЗАЦИЯ ФАЙЛА НАСТРОЕК
            CurrentModbusRTUSettings = new ModbusRTUSettings("COM1",1);
            CurrentModbusRTUSettings.SaveSettings(CurrentModbusRTUSettings, CurrentModbusRTUSettings.ModbusRTUSettingsFilePath);
            */

            // ВРЕМЕННАЯ СИМУЛЯЦИЯ НАЛИЧИЯ ДАННЫХ ДЛЯ СОХРАНЕНИЯ
            //DataToSaveExists = true;
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

            // Отправляем запрос на чтение регистра статуса
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
            CheckAccessAndDisplayOnTextBlock(equivalentCircuit_textBlock, EquivalentCircuit);

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
            CheckAccessAndDisplayOnTextBlock(integrationValue_textBlock, Integration);

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
            CheckAccessAndDisplayOnTextBlock(averagingValue_textBlock, Averaging);

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
            CheckAccessAndDisplayOnTextBlock(fixedMeasIntervalValue_textBlock, FixedMeasInterval);

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

            // Отправляем запрос на чтение регистра F
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 120, 1, 4);
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

            // Отправляем запрос на чтение регистра tgС
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 114, 1, 4);
        }

        private void Get_tgC(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgC;

            // Получаем значение тангенса
            this.tg_C = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ tgC
            Display_tgC();

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.Get_F;

            // Отправляем запрос на чтение регистра F
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 120, 1, 4);
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

            // Отправляем запрос на чтение регистра tgM
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 118, 1, 4);
        }

        private void Get_tgM(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_tgM;

            // Получаем значение тангенса
            this.tg_M = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ tgМ
            Display_tgM();

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

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.IdentifyRangeInterval;

            // Отправляем запрос на чтение регистра пределов измерения
            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 201, 1, 2); // Команда (0x03) на чтение 201-го регистра статуса, считываем 1 регистр 16 бит
        }

        private void IdentifyRangeInterval(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.IdentifyRangeInterval;

            // ПОЛУЧАЕМ 16 БИТНОЕ ЗНАЧЕНИЕ ПОДДИАПАЗОНА ИЗМЕРЕНИЯ
            this.RangeIntervalRegister = BitConverter.ToUInt16(new byte[2] { buffer[4], buffer[3] }, 0);
            ushort rangeIntervalValue = (ushort)(RangeIntervalRegister & 0xF); // Накладываем битовую маску 00000000 00001111 чтобы получить значение 4-ох последних битов 

            // УЗНАЕМ НОМЕР ПОДДИАПАЗОНА ИЗМЕРЕНИЯ И ИНТЕРВАЛ
            switch (rangeIntervalValue)
            {
                case 0:
                    this.RangeIntervalNumber = 1;
                    if (ChanalFlag == 0) this.RangeInterval = "от 10^-5 до 1 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 10^-10 до 16*10^-5 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-5 до 10 Ф";
                    break;
                case 1:
                    this.RangeIntervalNumber = 2;
                    if (ChanalFlag == 0) this.RangeInterval = "от 1 до 10 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 16*10^-5 до 16*10^-4 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-5 до 16*10^-4 Ф";
                    break;
                case 2:
                    this.RangeIntervalNumber = 3;
                    if (ChanalFlag == 0) this.RangeInterval = "от 10 до 100 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 16*10^-4 до 16*10^-3 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-9 до 16*10^-4 Ф";
                    break;
                case 3:
                    this.RangeIntervalNumber = 4;
                    if (ChanalFlag == 0) this.RangeInterval = "от 100 до 1000 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 16*10^-3 до 16*10^-2 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-9 до 16*10^-8 Ф";
                    break;
                case 4:
                    this.RangeIntervalNumber = 5;
                    if (ChanalFlag == 0) this.RangeInterval = "от 1000 до 10^4 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 16*10^-2 до 1,6 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-8 до 16*10^-7 Ф";
                    break;
                case 5:
                    this.RangeIntervalNumber = 6;
                    if (ChanalFlag == 0) this.RangeInterval = "от 10^4 до 10^5 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 1,6 до 16 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-12 до 16*10^-7 Ф";
                    break;
                case 6:
                    this.RangeIntervalNumber = 7;
                    if (ChanalFlag == 0) this.RangeInterval = "от 10^5 до 10^6 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 16 до 160 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-12 до 16*10^-11 Ф";

                    break;
                case 7:
                    this.RangeIntervalNumber = 8;
                    if (ChanalFlag == 0) this.RangeInterval = "от 10^6 до 10^7 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 160 до 16*10^2 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-11 до 16*10^-10 Ф";
                    break;
                case 8:
                    this.RangeIntervalNumber = 9;
                    if (ChanalFlag == 0) this.RangeInterval = "от 10^7 до 10^8 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 16*10^2 до 16*10^3 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 16*10^-15 до 16*10^-10 Ф";
                    break;
                case 9:
                    this.RangeIntervalNumber = 10;
                    if (ChanalFlag == 0) this.RangeInterval = "от 10^8 до 10^11 Ом";
                    if (ChanalFlag == 1) this.RangeInterval = "от 16*10^3 до 1*10^8 Гн";
                    if (ChanalFlag == 2) this.RangeInterval = "от 1*10^-16 до 16*10^-15 Ф";
                    break;
            }

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ ПРЕДЕЛОВ ИЗМЕРЕНИЙ
            DisplayRangeInterval();

            DataRowNumber++; // Увеличиваем значение счетчика порядкового номера измерения программы

            //===============================================================================================================================================
            // ВСЕ 5 КАНАЛОВ ОПРОШЕНЫ - ЗАПЫСЫВАЕМ ДАННЫЕ В ПЕРЕМЕННЫЕ
            // СОСТАВЛЯЕМ И ЗАПИСЫВАЕМ СТРОКУ ДАННЫХ В ФАЙЛ
            DataManager.SaveDataRow(CreateDataRow());

            // Указываем что данные появились
            if (DataToSaveExists == false)
            {
                DataToSaveExists = true;
            }

            // ДОБАВЛЯЕМ ЗНАЧЕНИЯ R, L, C или M, А ТАКЖЕ СООТВЕТСТВУЮЩЕЕ ВРЕМЯ ИЗМЕРЕНИЯ В КОЛЛЕКЦИИ 
            AddResultsToCollections();

            // СТРОИМ ГРАФИКИ           
            BuildGraphes();
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
            if (Timer != null && Modbus != null)
            {
                Timer.Change(Timeout.Infinite, 0); // Приостанавливаем измерение

                // ОТПИСЫВАЕМСЯ ОТ ОБРАБОТЧИКОВ СОБЫТИЯ ResposeReceived
                Modbus.ResponseReceived -= this.IdentifyStatus;
                Modbus.ResponseReceived -= this.Get_R;
                Modbus.ResponseReceived -= this.Get_L;
                Modbus.ResponseReceived -= this.Get_C;
                Modbus.ResponseReceived -= this.Get_M;
                Modbus.ResponseReceived -= this.Get_F;
                Modbus.ResponseReceived -= this.IdentifyRangeInterval;
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
            }

            MessageBox.Show(errorMessage, "Ошибка!");
        }

        private void DisplayRequestMessageInConsole(byte[] message)
        {
            for (int i = 0; i < ConsoleText.Length - 1; i++)
            {
                ConsoleText[i] = ConsoleText[i + 1];
            }
            ConsoleText[ConsoleText.Length - 1] = $"    {DateTime.UtcNow}    ЗАПРОС    --->    " + $"{BitConverter.ToString(message)}"; // Запись в последний элемент массива

            string displStr = "";
            foreach (var item in ConsoleText)
            {
                if (item != null)
                {
                    displStr += $"\n{item}";
                }
            }

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(statusTextBlock, displStr);
        }

        private void DisplayResponseMessageInConsole(byte[] message)
        {
            for (int i = 0; i < ConsoleText.Length - 1; i++)
            {
                ConsoleText[i] = ConsoleText[i + 1];
            }
            ConsoleText[ConsoleText.Length - 1] = $"    {DateTime.UtcNow}    ОТВЕТ       --->    " + $"{BitConverter.ToString(message)}"; // Запись в последний элемент массива

            string displStr = "";
            foreach (var item in ConsoleText)
            {
                if (item != null)
                {
                    displStr += $"\n{item}";
                }
            }

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(statusTextBlock, displStr);
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
            Modbus.ResponseReceived -= this.IdentifyRangeInterval;
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

            rangeIntervalNumber_textBlock.Text = "---";
            equivalentCircuit_textBlock.Text = "---";
            fixedMeasIntervalValue_textBlock.Text = "---";
            integrationValue_textBlock.Text = "---";
            averagingValue_textBlock.Text = "---";
            chanalNumber_textBlock.Text = "---";
        }


        private void Display_R()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(ValueSymbol_textBlock, "R:");
            CheckAccessAndDisplayOnTextBlock(Value_textBlock, Resistance.ToString() + " Ом");
        }

        private void Display_tgR()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(tgSymbol_textBlock, "tg" + "\u03C6:"); // "\u03B4:" сигма
            CheckAccessAndDisplayOnTextBlock(tg_textBlock, tg_R.ToString("0.######"));
        }

        private void Display_F()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(F_textBlock, Frequency.ToString() + " Гц");
        }

        private void Display_L()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(ValueSymbol_textBlock, "L:");
            CheckAccessAndDisplayOnTextBlock(Value_textBlock, Inductance.ToString() + " Гн");
        }

        private void Display_tgL()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(tgSymbol_textBlock, "tg" + "\u03B4:"); 
            CheckAccessAndDisplayOnTextBlock(tg_textBlock, tg_L.ToString("0.######"));
        }

        private void Display_C()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(ValueSymbol_textBlock, "C:");
            CheckAccessAndDisplayOnTextBlock(Value_textBlock, Capacity.ToString() + " Ф");
        }

        private void Display_tgC()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(tgSymbol_textBlock, "tg" + "\u03B4:");
            CheckAccessAndDisplayOnTextBlock(tg_textBlock, tg_C.ToString("0.######"));
        }

        private void Display_M()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(ValueSymbol_textBlock, "M:");
            CheckAccessAndDisplayOnTextBlock(Value_textBlock, MutualInductance.ToString());
        }

        private void Display_tgM()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(tgSymbol_textBlock, "tg" + "\u03C6:");
            CheckAccessAndDisplayOnTextBlock(tg_textBlock, tg_M.ToString("0.######"));
        }

        private void DisplayChanalNumber()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(chanalNumber_textBlock, ChanalNumber.ToString());
        }

        private void DisplayRangeInterval()
        {
            // ОТОБРАЖАЕМ РЕЗУЛЬТАТЫ
            CheckAccessAndDisplayOnTextBlock(rangeIntervalNumber_textBlock, this.RangeIntervalNumber.ToString());
            CheckAccessAndDisplayOnTextBlock(rangeInterval_textBlock, this.RangeInterval);
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
                    stringBuilder_0.Append(DateTime.Now + ";");
                    stringBuilder_0.Append(EquivalentCircuit + ";");
                    stringBuilder_0.Append(Frequency + ";");
                    stringBuilder_0.Append(Resistance + ";");
                    stringBuilder_0.Append(tg_R + ";");
                    dataRow = stringBuilder_0.ToString();
                    break;
                case 1:
                    StringBuilder stringBuilder_1 = new StringBuilder();
                    stringBuilder_1.Append(DataRowNumber + ";");
                    stringBuilder_1.Append(DateTime.Now + ";");
                    stringBuilder_1.Append(EquivalentCircuit + ";");
                    stringBuilder_1.Append(Frequency + ";");
                    stringBuilder_1.Append(";;");
                    stringBuilder_1.Append(Inductance + ";");
                    stringBuilder_1.Append(tg_L + ";");
                    dataRow = stringBuilder_1.ToString();
                    break;
                case 2:
                    StringBuilder stringBuilder_2 = new StringBuilder();
                    stringBuilder_2.Append(DataRowNumber + ";");
                    stringBuilder_2.Append(DateTime.Now + ";");
                    stringBuilder_2.Append(EquivalentCircuit + ";");
                    stringBuilder_2.Append(Frequency + ";");
                    stringBuilder_2.Append(";;;;");
                    stringBuilder_2.Append(Capacity + ";");
                    stringBuilder_2.Append(tg_C + ";");
                    dataRow = stringBuilder_2.ToString();
                    break;
                case 3:
                    StringBuilder stringBuilder_3 = new StringBuilder();
                    stringBuilder_3.Append(DataRowNumber + ";");
                    stringBuilder_3.Append(DateTime.Now + ";");
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
                Modbus.ResponseReceived -= this.IdentifyRangeInterval;
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

                // ОЧИЩАЕМ ДИАГРАММУ РАССЕЯНИЯ
                value_plot.plt.Clear();
            }
        }

        public void Close_program()
        {
            Stop_measurement();
            DataToSaveExists = false;
        }

        private void SaveMeasurementData_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataToSaveExists == true)
            {
                // СОЗДАНИЕ ОТНОСИТЕЛЬНОГО ПУТИ СОХРАНЕНИЯ ФАЙЛА
                StringBuilder stringBuilder = new StringBuilder();
                StringBuilder pathStringBuilder = new StringBuilder();
                pathStringBuilder.Append(Directory.GetCurrentDirectory());
                pathStringBuilder.Append(@"\");
                pathStringBuilder.Append(DataManager.DataDirectoryName);
                pathStringBuilder.Append(@"\");
                pathStringBuilder.Append(DataManager.DataFileName);
                pathStringBuilder.Append("_");
                pathStringBuilder.Append(DateTime.Now.ToString(("dd_MM_yyyy_hh-mmtt")));
                string filePath = pathStringBuilder.ToString();

                // КОНФИГУРИРОВАНИЕ SaveFileDialog
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Сохранение массива измерянных данных";
                saveFileDialog.FileName = $"{filePath}";
                saveFileDialog.InitialDirectory = $"{filePath}";
                saveFileDialog.OverwritePrompt = true;
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.AddExtension = true;
                if (saveFileDialog.ShowDialog() == true)
                {
                    // ОСТАНОВКА ИЗМЕРЕНИЯ
                    Stop_measurement();

                    // КОПИРОВАНИЕ ФАЙЛА "Data.csv" ИЗ ПАПКИ "Temp"
                    File.Copy($"{DataManager.TempDirectoryName}" + @"\" + $"{DataManager.TempDataFileName}" + "." + $"{DataManager.DataFileExt}", saveFileDialog.FileName, true);
                }
            }

        }

        private void AboutApp_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            AboutAppWindow aboutAppWindow = new AboutAppWindow();
            aboutAppWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        private void TechnicalAssistance_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            TechnicalAssistanceWindow technicalAssistanceWindow = new TechnicalAssistanceWindow();
            technicalAssistanceWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        private void Exit_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Stop_measurement();
            this.Close();
        }

        private void AboutDataFile_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            VisualEffects.ApplyBlurEffect(this);
            AboutDataFileWindow aboutDataFileWindow = new AboutDataFileWindow();
            aboutDataFileWindow.ShowDialog();
            VisualEffects.ClearBlurEffect(this);
        }

        private void StylePlots()
        {
            // НАСТРОЙКИ ГРАФИКОВ ПРИ ИХ ПОСТРОЕНИИ (до рендеринга)
            // Scatter
            value_plot.plt.Title("Диаграмма рассеяния"); // Заголовок
            value_plot.plt.YLabel("Значение", bold: true); // Надпись у оси Y
            value_plot.plt.XLabel("Время", bold: true); // Надпись у оси X
            value_plot.plt.Ticks(dateTimeX: true); // Используем на оси X - формат времени
            value_plot.plt.Ticks(displayTickLabelsX: false); // Не показываем значения у делений на оси X
            value_plot.plt.Ticks(displayTickLabelsY: false); // Не показываем значения у делений на оси Y


            // Population
            probability_plot.plt.Title("Оценка среднего"); // Заголовок
            probability_plot.plt.XLabel("Значение", bold: true); // Надпись у оси Y
            probability_plot.plt.Ticks(displayTicksY: false); // Не показываем главные деления оси Y
            probability_plot.plt.Ticks(displayTickLabelsY: false); // Не показываем значения у делений на оси Y
            probability_plot.plt.Ticks(displayTicksYminor: false); // Не показываем дополнительные деления оси Y
            probability_plot.plt.Ticks(displayTickLabelsX: false); // Не показываем значения у делений на оси X
            probability_plot.plt.Grid(lineStyle: ScottPlot.LineStyle.Dot);
        }

        private void AddResultsToCollections()
        {
            switch (ChanalFlag)
            {
                case 0:
                    R_list.Add(this.Resistance);
                    R_OADate_list.Add(DateTime.Now.ToOADate());
                    break;
                case 1:
                    L_list.Add(this.Inductance);
                    L_OADate_list.Add(DateTime.Now.ToOADate());
                    break;
                case 2:
                    C_list.Add(this.Capacity);
                    C_OADate_list.Add(DateTime.Now.ToOADate());
                    break;
                case 3:
                    M_list.Add(this.MutualInductance);
                    M_OADate_list.Add(DateTime.Now.ToOADate());
                    break;
                default:
                    break;
            }
        }

        private void BuildGraphes()
        {
            switch (ChanalFlag)
            {
                case 0:
                    // СОЗДАНИЕ МАССИВОВ ДЛЯ ПОСТРОЕНИЯ ГРАФИКОВ
                    double[] R_values;
                    double[] R_dates;

                    if (R_plot_index == 0)
                    {
                        R_values = R_list.ToArray();
                        R_dates = R_OADate_list.ToArray();
                    }
                    else
                    {
                        R_values = R_list.GetRange(R_plot_index, R_list.Count - R_plot_index).ToArray();
                        R_dates = R_OADate_list.GetRange(R_plot_index, R_OADate_list.Count - R_plot_index).ToArray();
                    }

                    // НАСТРОЙКИ ГРАФИКОВ ПРИ РЕНДЕРИНГЕ
                    // Scatter
                    if (R_values.Length > 1) // Если есть уже более 2ух точек - строим и отрисовываем графики
                    {
                        value_plot.plt.Clear();
                        value_plot.plt.PlotScatter(R_dates, R_values, label: "Сопротивление, Ом");
                        value_plot.plt.Title("Диаграмма рассеяния R"); // Надпись у оси Y
                        value_plot.plt.YLabel("Значение R, Ом", bold: true); // Надпись у оси X
                        value_plot.plt.Ticks(displayTicksY: true); // Используем дополнительные деления оси Y
                        value_plot.plt.Ticks(displayTicksYminor: true); // Используем дополнительные деления оси Y
                        value_plot.plt.Ticks(displayTickLabelsX: true); // Показываем значения у делений на оси X
                        value_plot.plt.Ticks(displayTickLabelsY: true); // Показываем значения у делений на оси Y
                        value_plot.plt.Ticks(numericFormatStringY: "E5"); // Используем форматирование чисел
                        value_plot.plt.Legend(location: legendLocation.upperRight, shadowDirection: shadowDirection.lowerLeft); // разрешаем отображение легенд
                    }
                    
                    // Population
                    var R_pop = new ScottPlot.Statistics.Population(R_values);
                    double[] R_curveXs = ScottPlot.DataGen.Range(R_pop.minus3stDev, R_pop.plus3stDev, .00001); // Массив точек оси X графика плотности вероятности
                    double[] R_curveYs = R_pop.GetDistribution(R_curveXs, false); // Массив точек оси Y графика плотности вероятности

                    if (R_curveXs.Length > 1)
                    {
                        // Creating an Ys scatter of values on a plot
                        Random rand = new Random(0);
                        double[] R_ys = ScottPlot.DataGen.RandomNormal(rand, R_pop.values.Length, stdDev: .15);

                        probability_plot.plt.Clear();
                        probability_plot.plt.PlotScatter(R_values, R_ys, markerSize: 5, markerShape: MarkerShape.openCircle, lineWidth: 0); // Диаграмма рассеяния величины на графике плотности (с искусственным рандомным расбросом по оси Y)
                        probability_plot.plt.PlotScatter(R_curveXs, R_curveYs, markerSize: 0, lineWidth: 4, color: System.Drawing.Color.Black, label: "Плотн. распр. R"); // График плотности вероятности
                        probability_plot.plt.Axis(x1: R_pop.minus3stDev, x2: R_pop.plus3stDev, y1: -0.05, y2: 1.2);
                        probability_plot.plt.Title($"Оценка ср. R: ({R_pop.mean:0.0000} " + "\u00B1" + $" {R_pop.stDev:0.0000}) Ом, n={R_pop.n}");
                        probability_plot.plt.PlotVLine(R_pop.mean, label: "Ср. знач. R", lineStyle: LineStyle.Dash, lineWidth: 1, color: System.Drawing.Color.LimeGreen);
                        probability_plot.plt.PlotVLine(R_pop.mean - R_pop.stDev, label: "-1\u03C3", lineStyle: LineStyle.Dash, lineWidth: 2, color: System.Drawing.Color.Red);
                        probability_plot.plt.PlotVLine(R_pop.mean + R_pop.stDev, label: "+1\u03C3", lineStyle: LineStyle.Dash, lineWidth: 2, color: System.Drawing.Color.DodgerBlue);
                        probability_plot.plt.Legend(location: legendLocation.lowerRight, shadowDirection: shadowDirection.lowerLeft); // Разрешаем отображение легенд
                        probability_plot.plt.XLabel("Значение R, Ом", bold: true);
                        probability_plot.plt.Ticks(displayTicksXminor: true); // Используем дополнительные деления оси Х
                        probability_plot.plt.Ticks(displayTickLabelsX: true); // Показываем значения у делений на оси X
                        probability_plot.plt.Ticks(numericFormatStringX: "E4"); // Используем форматирование чисел
                    }
                    CheckAccessAndUpdate_value_plot(value_plot);
                    CheckAccessAndUpdate_probability_plot(probability_plot);
                    break;
                case 1:
                    // СОЗДАНИЕ МАССИВОВ ДЛЯ ПОСТРОЕНИЯ ГРАФИКОВ
                    double[] L_values;
                    double[] L_dates;

                    if (L_plot_index == 0)
                    {
                        L_values = L_list.ToArray();
                        L_dates = L_OADate_list.ToArray();
                    }
                    else
                    {
                        L_values = L_list.GetRange(L_plot_index, L_list.Count - L_plot_index).ToArray();
                        L_dates = L_OADate_list.GetRange(L_plot_index, L_OADate_list.Count - L_plot_index).ToArray();
                    }

                    // НАСТРОЙКИ ГРАФИКОВ ПРИ РЕНДЕРИНГЕ
                    // Scatter
                    if (L_values.Length > 1) // Если есть уже более 2ух точек - строим и отрисовываем графики
                    {
                        value_plot.plt.Clear();
                        value_plot.plt.PlotScatter(L_dates, L_values, label: "Индуктивность, Гн");
                        value_plot.plt.Title("Диаграмма рассеяния L"); // Надпись у оси Y
                        value_plot.plt.YLabel("Значение L, Гн", bold: true); // Надпись у оси X
                        value_plot.plt.Ticks(displayTicksY: true); // Используем дополнительные деления оси Y
                        value_plot.plt.Ticks(displayTicksYminor: true); // Используем дополнительные деления оси Y
                        value_plot.plt.Ticks(displayTickLabelsX: true); // Показываем значения у делений на оси X
                        value_plot.plt.Ticks(displayTickLabelsY: true); // Показываем значения у делений на оси Y
                        value_plot.plt.Ticks(numericFormatStringY: "E5"); // Используем форматирование чисел
                        value_plot.plt.Legend(location: legendLocation.upperRight, shadowDirection: shadowDirection.lowerLeft); // разрешаем отображение легенд
                    }

                    // Population
                    var L_pop = new ScottPlot.Statistics.Population(L_values);
                    double[] L_curveXs = ScottPlot.DataGen.Range(L_pop.minus3stDev, L_pop.plus3stDev, .000000001); // Массив точек оси X графика плотности вероятности
                    double[] L_curveYs = L_pop.GetDistribution(L_curveXs, false); // Массив точек оси Y графика плотности вероятности

                    if (L_curveXs.Length > 1)
                    {
                        // Creating an Ys scatter of values on a plot
                        Random rand = new Random(0);
                        double[] L_ys = ScottPlot.DataGen.RandomNormal(rand, L_pop.values.Length, stdDev: .15);

                        probability_plot.plt.Clear();
                        probability_plot.plt.PlotScatter(L_values, L_ys, markerSize: 5, markerShape: MarkerShape.openCircle, lineWidth: 0); // Диаграмма рассеяния величины на графике плотности (с искусственным рандомным расбросом по оси Y)
                        probability_plot.plt.PlotScatter(L_curveXs, L_curveYs, markerSize: 0, lineWidth: 4, color: System.Drawing.Color.Black, label: "Плотн. распр. L"); // График плотности вероятности
                        probability_plot.plt.Axis(x1: L_pop.minus3stDev, x2: L_pop.plus3stDev, y1: -0.05, y2: 1.2);
                        probability_plot.plt.Title($"Оценка ср. L: ({L_pop.mean:0.0000} " + "\u00B1" + $" {L_pop.stDev:0.0000}) Гн, n={L_pop.n}");
                        probability_plot.plt.PlotVLine(L_pop.mean, label: "Ср. знач. L", lineStyle: LineStyle.Dash, lineWidth: 1, color: System.Drawing.Color.LimeGreen);
                        probability_plot.plt.PlotVLine(L_pop.mean - L_pop.stDev, label: "-1\u03C3", lineStyle: LineStyle.Dash, lineWidth: 2, color: System.Drawing.Color.Red);
                        probability_plot.plt.PlotVLine(L_pop.mean + L_pop.stDev, label: "+1\u03C3", lineStyle: LineStyle.Dash, lineWidth: 2, color: System.Drawing.Color.DodgerBlue);
                        probability_plot.plt.Legend(location: legendLocation.lowerRight, shadowDirection: shadowDirection.lowerLeft); // Разрешаем отображение легенд
                        probability_plot.plt.XLabel("Значение L, Гн", bold: true);
                        probability_plot.plt.Ticks(displayTicksXminor: true); // Используем дополнительные деления оси Х
                        probability_plot.plt.Ticks(displayTickLabelsX: true); // Показываем значения у делений на оси X
                        probability_plot.plt.Ticks(numericFormatStringX: "E4"); // Используем форматирование чисел
                    }
                    CheckAccessAndUpdate_value_plot(value_plot);
                    CheckAccessAndUpdate_probability_plot(probability_plot);
                    break;
                case 2:
                    // СОЗДАНИЕ МАССИВОВ ДЛЯ ПОСТРОЕНИЯ ГРАФИКОВ
                    double[] C_values;
                    double[] C_dates;

                    if (C_plot_index == 0)
                    {
                        C_values = C_list.ToArray();
                        C_dates = C_OADate_list.ToArray();
                    }
                    else
                    {
                        C_values = C_list.GetRange(C_plot_index, C_list.Count - C_plot_index).ToArray();
                        C_dates = C_OADate_list.GetRange(C_plot_index, C_OADate_list.Count - C_plot_index).ToArray();
                    }

                    // НАСТРОЙКИ ГРАФИКОВ ПРИ РЕНДЕРИНГЕ
                    // Scatter
                    if (C_values.Length > 1) // Если есть уже более 2ух точек - строим и отрисовываем графики
                    {
                        value_plot.plt.Clear();
                        value_plot.plt.PlotScatter(C_dates, C_values, label: "Емкость, Ф");
                        value_plot.plt.Title("Диаграмма рассеяния C"); // Надпись у оси Y
                        value_plot.plt.YLabel("Значение C, Ф", bold: true); // Надпись у оси X
                        value_plot.plt.Ticks(displayTicksY: true); // Используем дополнительные деления оси Y
                        value_plot.plt.Ticks(displayTicksYminor: true); // Используем дополнительные деления оси Y
                        value_plot.plt.Ticks(displayTickLabelsX: true); // Показываем значения у делений на оси X
                        value_plot.plt.Ticks(displayTickLabelsY: true); // Показываем значения у делений на оси Y
                        value_plot.plt.Ticks(numericFormatStringY: "E5"); // Используем форматирование чисел
                        value_plot.plt.Legend(location: legendLocation.upperRight, shadowDirection: shadowDirection.lowerLeft); // разрешаем отображение легенд
                    }

                    // Population
                    var C_pop = new ScottPlot.Statistics.Population(C_values);
                    double[] C_curveXs = ScottPlot.DataGen.Range(C_pop.minus3stDev, C_pop.plus3stDev, .00000000000001); // Массив точек оси X графика плотности вероятности
                    double[] C_curveYs = C_pop.GetDistribution(C_curveXs, false); // Массив точек оси Y графика плотности вероятности

                    if (C_curveXs.Length > 1)
                    {
                        // Creating an Ys scatter of values on a plot
                        Random rand = new Random(0);
                        double[] C_ys = ScottPlot.DataGen.RandomNormal(rand, C_pop.values.Length, stdDev: .15);

                        probability_plot.plt.Clear();
                        probability_plot.plt.PlotScatter(C_values, C_ys, markerSize: 5, markerShape: MarkerShape.openCircle, lineWidth: 0); // Диаграмма рассеяния величины на графике плотности (с искусственным рандомным расбросом по оси Y)
                        probability_plot.plt.PlotScatter(C_curveXs, C_curveYs, markerSize: 0, lineWidth: 4, color: System.Drawing.Color.Black, label: "Плотн. распр. C"); // График плотности вероятности
                        probability_plot.plt.Axis(x1: C_pop.minus3stDev, x2: C_pop.plus3stDev, y1: -0.05, y2: 1.2);
                        probability_plot.plt.Title($"Оценка ср. C: ({C_pop.mean:0.0000} " + "\u00B1" + $" {C_pop.stDev:0.0000}) Ф, n={C_pop.n}");
                        probability_plot.plt.PlotVLine(C_pop.mean, label: "Ср. знач. C", lineStyle: LineStyle.Dash, lineWidth: 1, color: System.Drawing.Color.LimeGreen);
                        probability_plot.plt.PlotVLine(C_pop.mean - C_pop.stDev, label: "-1\u03C3", lineStyle: LineStyle.Dash, lineWidth: 2, color: System.Drawing.Color.Red);
                        probability_plot.plt.PlotVLine(C_pop.mean + C_pop.stDev, label: "+1\u03C3", lineStyle: LineStyle.Dash, lineWidth: 2, color: System.Drawing.Color.DodgerBlue);
                        probability_plot.plt.Legend(location: legendLocation.lowerRight, shadowDirection: shadowDirection.lowerLeft); // Разрешаем отображение легенд
                        probability_plot.plt.XLabel("Значение C, Ф", bold: true);
                        probability_plot.plt.Ticks(displayTicksXminor: true); // Используем дополнительные деления оси Х
                        probability_plot.plt.Ticks(displayTickLabelsX: true); // Показываем значения у делений на оси X
                        probability_plot.plt.Ticks(numericFormatStringX: "E5"); // Используем форматирование чисел
                    }
                    CheckAccessAndUpdate_value_plot(value_plot);
                    CheckAccessAndUpdate_probability_plot(probability_plot);
                    break;
                case 3:
                    //
                    break;
                default:
                    break;
            }
        }

        private void CheckAccessAndUpdate_value_plot(object wpfElement)
        {
            value_plot = wpfElement as ScottPlot.WpfPlot;

            if (value_plot.CheckAccess())
            {
                value_plot.plt.AxisAuto();
                value_plot.Render(skipIfCurrentlyRendering: true);
            }
            else
            {
                value_plot.Dispatcher.InvokeAsync(() =>
                {
                    value_plot.plt.AxisAuto();
                    value_plot.Render(skipIfCurrentlyRendering: true);
                });
            }
        }

        private void CheckAccessAndUpdate_probability_plot(object wpfElement)
        {
            probability_plot = wpfElement as ScottPlot.WpfPlot;

            if (probability_plot.CheckAccess())
            {
                probability_plot.plt.AxisAutoX();
                probability_plot.Render();
            }
            else
            {
                probability_plot.Dispatcher.InvokeAsync(() =>
                {
                    probability_plot.plt.AxisAutoX();
                    probability_plot.Render();
                });
            }
        }

        private void CheckAccessAndDisplayOnTextBlock(object wpfElement, string displayedString)
        {
            TextBlock textBlock = wpfElement as TextBlock;

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (textBlock.CheckAccess())
            {
                textBlock.Text = displayedString;
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                textBlock.Dispatcher.InvokeAsync(() => textBlock.Text = displayedString);
            }
        }

        private void ClearScatterPlots_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (ChanalFlag == 0)
            {
                if (ChanalFlag == 0)
                {
                    this.R_plot_index = R_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
                }
                if (ChanalFlag == 1)
                {
                    this.L_plot_index = L_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
                }
                if (ChanalFlag == 2)
                {
                    this.C_plot_index = C_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
                }
                if (ChanalFlag == 3)
                {
                    this.M_plot_index = M_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
                }
            }
        }

        private void ClearPlots_button_Click(object sender, RoutedEventArgs e)
        {
            if (ChanalFlag == 0)
            {
                this.R_plot_index = R_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
            }
            if (ChanalFlag == 1)
            {
                this.L_plot_index = L_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
            }
            if (ChanalFlag == 2)
            {
                this.C_plot_index = C_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
            }
            if (ChanalFlag == 3)
            {
                this.M_plot_index = M_list.Count; // Присваиваем индексу значение с которого будет отображаться график (отображение с последней точки в коллекции)
            }
        }
    }
}