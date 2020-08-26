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
    public class ModbusRTUSettings
    {
        //НАСТРОЙКИ COM-порта
        public string PortName { get; set; }

        //ИНТЕРВАЛ ОПРОСА
        public int PollingInterval { get; set; }

        //ПУТЬ к ФАЙЛУ НАСТРОЕК
        [NonSerialized]
        public static string ModbusRTUSettingsFilePath = @"ModbusRTUSettings.dat";

        //НАСТРОЙКИ Modbus
        [NonSerialized]
        public static byte ModbusSlaveAddress = 0x9;
        [NonSerialized]
        public static int BaudRate = 19200;
        [NonSerialized]
        public static Parity Parity = Parity.Even;
        [NonSerialized]
        public static StopBits StopBits = StopBits.One;
        [NonSerialized]
        public static int DataBits = 8;
        [NonSerialized]
        public static Handshake Handshake = Handshake.None;

        //ИНТЕРВАЛ ТИШИНЫ после отправки сообщения ModbusRTU 
        [NonSerialized]
        public int SilentInterval = GetSilentInterval();

        //ВРЕМЯ ОЖИДАНИЯ ОТВЕТА от SLAVE-устройства [мс]
        [NonSerialized]
        public static int ReponseTimeout = 150;

        public ModbusRTUSettings(string portName, int pollingInterval)
        {
            PortName = portName;
            PollingInterval = pollingInterval;
        }

        public static ModbusRTUSettings GetCurrentSettings(string settingsFilePath)
        {
            ModbusRTUSettings currentSettings = null;
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (FileStream fileStream = new FileStream(settingsFilePath, FileMode.Open)) // десериализация объекта
            {
                currentSettings = (ModbusRTUSettings)binaryFormatter.Deserialize(fileStream);
            }
            return currentSettings;
        }

        public static void SaveSettings(ModbusRTUSettings settings, string settingsFilePath)
        {
            BinaryFormatter binaryFormatter = new BinaryFormatter();

            using (FileStream fileStream = new FileStream(settingsFilePath, FileMode.OpenOrCreate)) // сериализация объекта
            {
                binaryFormatter.Serialize(fileStream, settings);
            }
        }

        private static int GetSilentInterval()
        {
            int delay = 1; // задержка в [мс]
            if (BaudRate == 19200)
            {
                return delay;
            }
            if (BaudRate == 9600 | BaudRate > 19200)
            {
                return delay = 2;
            }
            return delay;
        }
    }
}
