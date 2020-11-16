﻿using Microsoft.Win32;
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
        double[] R_array;

        // Порядковый номер измерения R (индекс массива)
        int Index_R;

        // Построен ли график
        bool R_ScatterIsBuilt;

        // Коллекция времени измерений R
        double[] R_OADate_array;

        // Тангенс R
        float tg_R;

        // Частота
        float Frequency;

        // Индуктивность
        float Inductance;

        // Коллекция L
        double[] L_array;

        // Коллекция времени измерений L
        double[] L_OADate_array;

        // Тангенс L
        float tg_L;

        // Емкость
        float Capacity;

        // Коллекция C
        double[] C_array;

        // Коллекция времени измерений C
        double[] C_OADate_array;

        // Тангенс С
        float tg_C;

        // Взаимоиндуктивность
        float MutualInductance;

        // Коллекция M
        double[] M_array;

        // Коллекция времени измерений M
        double[] M_OADate_array;

        // Тангенс M
        float tg_M;

        // Флаг основного индицируемого канала
        int ChanalFlag;

        // Порядковый номер измерения
        int DataRowNumber;

        // Флаг - данные для сохранения существуют
        public bool DataToSaveExists;

        // X,Y- ЗНАЧЕНИЯ ДЛЯ ГРАФИКА
        double[] X_array;
        double[] Y_array;

        //public delegate void GrafHandler();
        //public event GrafHandler GraphUpdate;

        public MainWindow()
        {
            InitializeComponent();

            // ДОБАВЛЯЕМ ОБРАБОТЧИКИ СОБЫТИЙ 
            this.Loaded += MainWindow_Loaded; // Загружено и отрисовано окно
            this.Closing += MainWindow_Closing; // При закрытии окна
            this.Closed += MainWindow_Closed; // Окно закрыто
            this.Unloaded += MainWindow_Unloaded; // Окно закрыто и освобождены все ресурсы
                                                  //this.GraphUpdate += UpdateGraph;

            // ИНИЦИАЛИЗИРУЕМ ПЕРЕМЕННЫЕ
            this.ConsoleText = new string[16];
            this.DataRowNumber = 0;
            this.Index_R = 0;
            this.R_ScatterIsBuilt = false;

            // ИНИЦИАЛИЗИРУЕМ МАССИВЫ ДАННЫХ ДЛЯ ГРАФИКОВ
            this.R_array = new double[172800];
            this.L_array = new double[172800];
            this.C_array = new double[172800];
            this.M_array = new double[172800];
            this.R_OADate_array = new double[172800];
            this.L_OADate_array = new double[172800];
            this.C_OADate_array = new double[172800];
            this.M_OADate_array = new double[172800];
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
                    this.EquivalentCircuit = "Посл.";
                    break;
                case 0:
                    this.EquivalentCircuit = "Парал.";
                    break;
            }
            // Отображаем результат
            CheckAccessAndDisplayOnTextBlock(EquivalentCircuit_textBlock, EquivalentCircuit);

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

        private void Get_F(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.Get_F;

            // Получаем значение F
            this.Frequency = BitConverter.ToSingle(new byte[4] { buffer[6], buffer[5], buffer[4], buffer[3] }, 0);

            // ОТОБРАЖАЕМ РЕЗУЛЬТАТ F
            Display_F();

            DataRowNumber++; // Увеличиваем значение счетчика порядкового номера измерения программы

            // ВСЕ 4 КАНАЛА ОПРОШЕНЫ - ЗАПЫСЫВАЕМ ДАННЫЕ В ПЕРЕМЕННЫЕ
            // СОСТАВЛЯЕМ И ЗАПИСЫВАЕМ СТРОКУ ДАННЫХ В ФАЙЛ
            DataManager.SaveDataRow(CreateDataRow());

            // Указываем что данные появились
            if (DataToSaveExists == false)
            {
                DataToSaveExists = true;
            }

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.IdentifyRangeInterval;

            Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 201, 1, 2); // Команда (0x03) на чтение 201-го регистра статуса, считываем 1 регистр 16 бит
        }

        private void IdentifyRangeInterval(byte[] buffer)
        {
            // ОТПИСЫВАЕМСЯ ОТ СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived -= this.IdentifyRangeInterval;

            // ПОЛУЧАЕМ 16 БИТРОЕ ЗНАЧЕНИЕ ПОДДИАПАЗОНА ИЗМЕРЕНИЯ
            this.RangeIntervalRegister = BitConverter.ToUInt16(new byte[2] { buffer[4], buffer[3] }, 0);
            ushort rangeIntervalValue = (ushort)(RangeIntervalRegister & 0xF); // Накладываем битовую маску 00000000 00001111 чтобы получить значение 4ех последних битов 

            // УЗНАЕМ НОМЕР ПОДДИАПАЗОНА ИЗМЕРЕНИЯ
            switch (rangeIntervalValue)
            {
                case 0:
                    this.RangeIntervalNumber = 1;
                    if (ChanalFlag == 0) RangeInterval = "от 10^-5 до 1 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 10^-10 до 16*10^-5 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-5 до 10 Ф";

                    break;
                case 1:
                    this.RangeIntervalNumber = 2;
                    if (ChanalFlag == 0) RangeInterval = "от 1 до 10 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 16*10^-5 до 16*10^-4 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-5 до 16*10^-4 Ф";
                    break;
                case 2:
                    this.RangeIntervalNumber = 3;
                    if (ChanalFlag == 0) RangeInterval = "от 10 до 100 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 16*10^-4 до 16*10^-3 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-9 до 16*10^-4 Ф";
                    break;
                case 3:
                    this.RangeIntervalNumber = 4;
                    if (ChanalFlag == 0) RangeInterval = "от 100 до 1000 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 16*10^-3 до 16*10^-2 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-9 до 16*10^-8 Ф";
                    break;
                case 4:
                    this.RangeIntervalNumber = 5;
                    if (ChanalFlag == 0) RangeInterval = "от 1000 до 10^4 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 16*10^-2 до 1,6 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-8 до 16*10^-7 Ф";
                    break;
                case 5:
                    this.RangeIntervalNumber = 6;
                    if (ChanalFlag == 0) RangeInterval = "от 10^4 до 10^5 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 1,6 до 16 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-12 до 16*10^-7 Ф";
                    break;
                case 6:
                    this.RangeIntervalNumber = 7;
                    if (ChanalFlag == 0) RangeInterval = "от 10^5 до 10^6 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 16 до 160 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-12 до 16*10^-11 Ф";

                    break;
                case 7:
                    this.RangeIntervalNumber = 8;
                    if (ChanalFlag == 0) RangeInterval = "от 10^6 до 10^7 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 160 до 16*10^2 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-11 до 16*10^-10 Ф";
                    break;
                case 8:
                    this.RangeIntervalNumber = 9;
                    if (ChanalFlag == 0) RangeInterval = "от 10^7 до 10^8 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 16*10^2 до 16*10^3 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 16*10^-15 до 16*10^-10 Ф";
                    break;
                case 9:
                    this.RangeIntervalNumber = 10;
                    if (ChanalFlag == 0) RangeInterval = "от 10^8 до 10^11 Ом";
                    if (ChanalFlag == 1) RangeInterval = "от 16*10^3 до 1*10^8 Гн";
                    if (ChanalFlag == 2) RangeInterval = "от 1*10^-16 до 16*10^-15 Ф";
                    break;
            }
            // Отображаем результат
            DisplayRangeInterval();

            // ПОДПИСЫВАЕМСЯ НА ОБРАБОТЧИК СОБЫТИЯ ResposeReceived
            Modbus.ResponseReceived += this.IdentifyStatus;

            // Отправляем запрос на чтение регистра F
            //Modbus.SendCommandToReadRegisters(CurrentModbusRTUSettings.ModbusRTUSlaveAddress, 0x03, 200, 1, 2);

            // ========================= Опрос всех каналов окончен
            
            

            // Отображаем результат
            //CheckAccessAndDisplayOnTextBlock(EquivalentCircuit_textBlock, EquivalentCircuit);

            //------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
            // Добавляем значение R, L, C или M, а также соответствующее время измерения в коллекции 
            //AddResultsToCollections();

            // ОТОБРАЖАЕМ НА ГРАФИКЕ
            //DisplayGraphAcync();

            //DisplayGraph();
            //==========================================================================================================================
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

            EquivalentCircuit_textBlock.Text = "---";
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
            CheckAccessAndDisplayOnTextBlock(tgSymbol_textBlock, "tg" + "\u03B4:");
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
            CheckAccessAndDisplayOnTextBlock(tgSymbol_textBlock, "tg" + "\u03C6:");
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
            CheckAccessAndDisplayOnTextBlock(rangeIntervalNumber_textBlock, RangeIntervalNumber.ToString());
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

                // ИНИЦИАЛИЗИРУЕМ ПЕРЕМЕННЫЕ ДЛЯ ГРАФИКА
                //this.X_array = new double[1] { 0 }; // Переменная будет хранить начальное значение X=0 для постройки графика
                //this.Y_array = new double[1] { 0 }; // Переменная будет хранить начальное значение Y=0 для постройки графика

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


        private void AddResultsToCollections()
        {
            switch (ChanalFlag)
            {
                case 0:

                    if (Index_R < R_array.Length) // Проверка переполнения массива
                    {
                        this.R_array[Index_R] = this.Resistance;
                        this.R_OADate_array[Index_R] = DateTime.Now.ToOADate();
                        Index_R++; // Увеличиваем значение индекса массива

                        // ЕСЛИ ГРАФИК НЕ ПОСТРОЕН
                        if (R_ScatterIsBuilt == false)
                        {
                            // СОЗДАЕМ ДИАГРАММУ РАССЕЯНИЯ
                            var plot_R = new ScottPlot.Plot();

                            plot_R.PlotSignalXY(R_array, R_OADate_array, System.Drawing.Color.Black, lineWidth: 1, markerSize: 0, lineStyle: ScottPlot.LineStyle.Solid);
                            plot_R.Title("Диаграмма рассеяния");
                            plot_R.YLabel("Активная составляющая сопротивления [Ом]", fontSize: 18, bold: true);
                            plot_R.XLabel("Время", fontSize: 18, bold: true);
                            //plot_R.Ticks(dateTimeX: true);
                            plot_R.AxisAuto();


                            R_ScatterIsBuilt = true;
                        }

                        //plot_R.Render();
                        //BuildAndUpdateScatter();
                    }
                    else
                    {
                        Stop_measurement();
                    }
                    break;
                case 1:
                    break;
                case 2:
                    break;
                case 3:
                    break;
                default:
                    break;
            }
        }

        private async void DisplayGraphAcync()
        {
            // Вызываем метод в асинхронном режиме
            await Task.Run(() => DisplayGraph());
        }

        private void DisplayGraph()
        {
            switch (ChanalFlag)
            {
                case 0:

                    CheckAccessAndDisplayGraph(value_plot);
                    break;
                case 1:
                    //
                    break;
                case 2:
                    //
                    break;
                case 3:
                    //
                    break;
                default:
                    break;
            }
        }

        private void CheckAccessAndDisplayGraph(object wpfElement)
        {
            value_plot = wpfElement as ScottPlot.WpfPlot;

            //Проверяем имеет ли вызывающий поток доступ к потоку UI
            // Поток имеет доступ к потоку UI
            if (value_plot.CheckAccess())
            {
                //plot_R.plt.Style(figBg: System.Drawing.Color.Black, tick: System.Drawing.Color.White, label: System.Drawing.Color.Yellow, dataBg:System.Drawing.Color.Black, grid: System.Drawing.Color.DimGray);
                //plot_R.plt.Title("R = f(time)");
                //plot_R.plt.YLabel("Активная составляющая сопротивления [Ом]", fontSize: 18, bold: true);
                //plot_R.plt.XLabel("Время", fontSize: 18, bold: true);
                //plot_R.plt.Ticks(dateTimeX: true);
                //plot_R.plt.AxisAuto();
                value_plot.Render();
            }
            //Поток не имеет доступ к потоку UI 
            else
            {
                value_plot.Dispatcher.InvokeAsync(() =>
                {
                    //plot_R.plt.Style(figBg: System.Drawing.Color.Black, tick: System.Drawing.Color.White, label: System.Drawing.Color.Yellow, dataBg:System.Drawing.Color.Black, grid: System.Drawing.Color.DimGray);
                    // plot_R.plt.Title("R = f(time)");
                    // plot_R.plt.YLabel("Активная составляющая сопротивления [Ом]", fontSize: 18, bold: true);
                    // plot_R.plt.XLabel("Время", fontSize: 18, bold: true);
                    //plot_R.plt.Ticks(dateTimeX: true);
                    // plot_R.plt.AxisAuto();
                    //plot_R.plt.Legend(enableLegend: false);
                    value_plot.Render();
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

        }
        /*
        private void UpdateGraph()
        {
            CheckAccessAndDisplayGraph(plot_R);
        }*/
    }
}