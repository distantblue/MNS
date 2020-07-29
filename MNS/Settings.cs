using System;
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
    public class Settings
    {
        //ПУТЬ к ФАЙЛУ НАСТРОЕК
        [NonSerialized]
        public static string SettingsFilePath = @"Settings.dat";

        //НАСТРОЙКИ Modbus
        [NonSerialized]
        public static byte ModbusSlaveAddress = 0x9;

        //НАСТРОЙКИ COM-порта
        public string PortName { get; set; }
        [NonSerialized]
        public static int BaudRate = 19200;
        [NonSerialized]
        public static Parity Parity = Parity.None;
        [NonSerialized]
        public static StopBits StopBits = StopBits.One;
        [NonSerialized]
        public static int DataBits = 8;

        //ИНТЕРВАЛ ОПРОСА
        public int PollingInterval { get; set; }


        public Settings(string portName, int pollingInterval)
        {
            PortName = portName;
            PollingInterval = pollingInterval;
        }

        public static Settings GetCurrentSettings(string settingsFilePath)
        {
            Settings currentSettings = null;
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (FileStream fileStream = new FileStream(settingsFilePath, FileMode.Open)) // десериализация объекта
            {
                currentSettings = (Settings)binaryFormatter.Deserialize(fileStream);
            }
            return currentSettings;
        }

        public static void SaveSettings(Settings settings, string settingsFilePath)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (FileStream fileStream = new FileStream(settingsFilePath, FileMode.OpenOrCreate)) // сериализация объекта
            {
                binaryFormatter.Serialize(fileStream, settings);
            }
        }
    }
}
