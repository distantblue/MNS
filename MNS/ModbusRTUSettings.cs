﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Ports;

namespace MNS
{
    [Serializable]
    public class ModbusRTUSettings
    {
        // ИМЯ ПОРТА
        public string PortName { get; set; }

        // ИНТЕРВАЛ ОПРОСА
        public int PollingInterval { get; set; }

        // ПУТЬ к ФАЙЛУ НАСТРОЕК
        [NonSerialized]
        public readonly string ModbusRTUSettingsFilePath = @"ModbusRTUSettings.dat";

        // НАСТРОЙКИ Modbus
        [NonSerialized]
        public readonly byte ModbusRTUSlaveAddress = 0x9;
        [NonSerialized]
        public readonly int BaudRate = 19200;
        [NonSerialized]
        public readonly Parity Parity = Parity.None;
        [NonSerialized]
        public readonly StopBits StopBits = StopBits.One;
        [NonSerialized]
        public readonly int DataBits = 8;
        [NonSerialized]
        public readonly Handshake Handshake = Handshake.None;

        // ИНТЕРВАЛ ТИШИНЫ после отправки сообщения ModbusRTU 
        [NonSerialized]
        public readonly int SilentInterval; // Гарантированный интервал тишины после отправки данных устройству после которого устройство начинает обработку запроса   

        // ВРЕМЯ ОЖИДАНИЯ ЧТЕНИЯ из COM порта [мс]
        [NonSerialized]
        public readonly int ReadTimeout = -1; // [-1] - бесконечное время ожидания

        // ВРЕМЯ ОЖИДАНИЯ ЗАПИСИ в порт [мс]
        [NonSerialized]
        public readonly int WriteTimeout = 100;

        // ВРЕМЯ ОЖИДАНИЯ ОТВЕТА от устройства [мс]
        [NonSerialized]
        public readonly int ResponseTimeout = 100;

        // Объявляю делегат
        public delegate void ModbusRTUSettingsErrorHandler(string message);

        // Обявляю событие "не найден файл настроек"
        public event ModbusRTUSettingsErrorHandler SettingsFileNotFoundError;

        // Обявляю событие "ошибка при чтении файла настроек"
        public event ModbusRTUSettingsErrorHandler SettingsFileReadingError;

        public ModbusRTUSettings() // Инициализируем переменные значениями по умолчанию, чтоб не ссылались в null
        {
            this.SilentInterval = GetSilentInterval(); 
            this.PortName = "COM1"; 
            this.PollingInterval = 1;
        }

        public ModbusRTUSettings(string portName, int pollingInterval)
        {
            this.PortName = portName;
            this.PollingInterval = pollingInterval;
            this.SilentInterval = GetSilentInterval();
        }

        public void GetCurrentSettings()
        {
            ModbusRTUSettings currentSettings = GetCurrentSettings(this.ModbusRTUSettingsFilePath);
            this.PortName = currentSettings.PortName;
            this.PollingInterval = currentSettings.PollingInterval;
        }

        public ModbusRTUSettings GetCurrentSettings(string settingsFilePath)
        {
            ModbusRTUSettings currentSettings = null;
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            try
            {
                FileStream fileStream = new FileStream(settingsFilePath, FileMode.Open);
                currentSettings = (ModbusRTUSettings)binaryFormatter.Deserialize(fileStream); // получаем текущие настройки подключения
                fileStream.Dispose();
            }
            catch (FileNotFoundException exception)
            {
                SettingsFileNotFoundError?.Invoke($"В директории где расположен исполняемый файл программы отсутствует файл настроек {ModbusRTUSettingsFilePath} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
                //MessageBox.Show("В директории где расположен исполняемый файл программы отсутствует файл настроек ModbusRTUSettings.dat" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
            }
            catch (System.Runtime.Serialization.SerializationException exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при десериализации объекта настроек программы из файла настроек {ModbusRTUSettingsFilePath} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
                //MessageBox.Show("В директории где расположен исполняемый файл программы отсутствует файл настроек ModbusRTUSettings.dat" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
            }
            catch (Exception exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при считывании настроек программы из файла настроек {ModbusRTUSettingsFilePath} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
                //MessageBox.Show("Возникла ошибка при попытке считать настройки подключения программы" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
            }

            return currentSettings;
        }

        public void SaveSettings(ModbusRTUSettings settings, string settingsFilePath)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            try
            {
                FileStream fileStream = new FileStream(settingsFilePath, FileMode.OpenOrCreate);
                binaryFormatter.Serialize(fileStream, settings); // сериализация объекта
                fileStream.Dispose();
            }
            catch (System.Runtime.Serialization.SerializationException exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при десериализации объекта настроек программы из файла настроек {ModbusRTUSettingsFilePath} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
                //MessageBox.Show("В директории где расположен исполняемый файл программы отсутствует файл настроек ModbusRTUSettings.dat" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
            }
            catch (Exception exception)
            {
                SettingsFileReadingError?.Invoke($"Возникла ошибка при считывании настроек программы из файла настроек {ModbusRTUSettingsFilePath} \n\n Подробнее о возникшей исключительной ситуации: \n\n {exception.Message}");
                //MessageBox.Show("Возникла ошибка при попытке считать настройки подключения программы" + "\n\n" + "Exception message: " + exception.Message, "Ошибка!");
            }
        }

        private int GetSilentInterval()
        {
            int delay = 1; // задержка в [мс]
            if (this.BaudRate == 19200)
            {
                return delay;
            }
            if (this.BaudRate == 9600 | BaudRate > 19200)
            {
                return delay = 2;
            }
            return delay;
        }
    }
}
