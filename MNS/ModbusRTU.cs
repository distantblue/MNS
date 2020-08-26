using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MNS
{
    class ModbusRTU
    {
        //Переменная которая будет хранить сообщние-команду Modbus в виде List
        private List<byte> Modbus_Message;
        //Переменная которая будет хранить сообщние-команду Modbus в виде byte[]
        public byte[] ModbusMessage;
        //Экземпляр класса SerialPort
        private SerialPort SerialPort;

        //Объявляем делегат
        public delegate void ModbusRTUEventHandler();
        //Объявляем событие "получен ответ от SLAVE-устройства"
        public event ModbusRTUEventHandler ResponseReceived;
        //Объявляем событие "не получен ответ от SLAVE-устройства"
        public event ModbusRTUEventHandler ResponseError;

        /// <summary>
        /// Конструктор класса ModbusRTU
        /// </summary>
        /// <param name="modbusRTUSettings">Экземпляр класса ModbusRTUSettings</param>
        public ModbusRTU(ModbusRTUSettings modbusRTUSettings)
        {
            Modbus_Message = new List<byte>();

            //КОНФИГУРИРОВАНИЕ COM-ПОРТА
            SerialPort = new SerialPort(modbusRTUSettings.PortName, ModbusRTUSettings.BaudRate, ModbusRTUSettings.Parity, ModbusRTUSettings.DataBits, ModbusRTUSettings.StopBits); // конфигурируем COM-порт
            SerialPort.Handshake = ModbusRTUSettings.Handshake;
            SerialPort.ReadTimeout = ModbusRTUSettings.ReponseTimeout; //время ожидания ответа на COM-порт
            SerialPort.WriteTimeout = modbusRTUSettings.SilentInterval; //интервал тишины после отправки данных по COM-порт    
        }

        public byte[] BuildModbusMessage(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToRead, ushort QuantityOfRegistersToRead)
        {
            Modbus_Message.Add(SlaveAddress);
            Modbus_Message.Add(ModbusFunctionCode);
            Modbus_Message.Add((byte)(StartingAddressOfRegisterToRead >> 8)); // сдвиг регистров на 8 позиций вправо, чтобы получить старший байт [HI Byte] 16 битного числа
            Modbus_Message.Add((byte)(StartingAddressOfRegisterToRead & 0xFF)); // накладываем битовую маску 00000000 11111111 (0xFF) чтобы получить младший байт [LO Byte] 16 битного числа
            Modbus_Message.Add((byte)(QuantityOfRegistersToRead >> 8)); // сдвиг регистров на 8 позиций вправо, чтобы получить старший байт [HI Byte] 16 битного числа
            Modbus_Message.Add((byte)(QuantityOfRegistersToRead & 0xFF)); // накладываем битовую маску 00001111 (0xF) чтобы получить младший байт [LO Byte] 16 битного числа
            byte[] data = Modbus_Message.ToArray(); // формируем массив данных по которым будет выполнен подсчет контрольной суммы
            ushort CRC = GenerateCRC(data); // генерация контрольной суммы
            byte CRC_LO_byte = (byte)(CRC & 0xFF); // разделение 2 байт на старший и младший байты
            byte CRC_HI_byte = (byte)(CRC >> 8);
            Modbus_Message.Add(CRC_LO_byte); // добавление байтов контрольной суммы к сообщению MODBUS
            Modbus_Message.Add(CRC_HI_byte);
            ModbusMessage = Modbus_Message.ToArray(); // получаем массив байт (сообщение Modbus)

            return ModbusMessage;
        }

        public byte[] BuildModbusMessage(byte[] bytesToWrite, byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegister, ushort QuantityOfRegisters)
        {
            //позже
            return ModbusMessage;
        }

        private ushort GenerateCRC(byte[] data)
        {
            ushort CRC = 0xFFFF; // 11111111 11111111
            for (int i = 0; i < data.Length; i++) // для каждого байта в Modbus сообщении
            {
                CRC ^= data[i]; // исключающее ИЛИ (XOR) с каждым байтом данных в сообщении
                for (int j = 0; j < 8; j++) // для каждого бита в байте
                {
                    bool LSB_flag = false;
                    if ((CRC & 0x0001) == 0x0001) // если младший бит 1
                    {
                        LSB_flag = true;
                    }
                    CRC >>= 1; // сдвиг регистра вправо на 1
                    if (LSB_flag) // если младший бит был равен 1
                    {
                        CRC ^= 0xA001; // еще исключающее ИЛИ (XOR) с полиномом 0xA001
                    }
                }
            }
            return CRC;
        }

        private void SendModbusMessage(byte[] modbusMessage)
        {
            if (!SerialPort.IsOpen)
            {
                try
                {
                    SerialPort.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Возникла ошибка при попытке открыть порт {SerialPort.PortName}. Подробнее о возникшей исключительной ситуации: " + "\n\n" + ex.Message, "Ошибка!");
                }
            }
            try
            {
                SerialPort.Write(ModbusMessage, 0, ModbusMessage.Length);
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show("Устройство не ответило на запрос. Проверьте подключение устройства. Подробнее о возникшей исключительной ситуации: " + "\n\n" + ex.Message, "Ошибка!");
            }

            SerialPort.Close();
        }

        private void ListenToSlaveResponse()
        {
            SerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPortDataReceived); //подписываемся на событие "пришли данные на COM-порт"

            if (!SerialPort.IsOpen)
            {
                try
                {
                    SerialPort.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Возникла ошибка при попытке открыть порт {SerialPort.PortName}. Подробнее о возникшей исключительной ситуации: " + "\n\n" + ex.Message, "Ошибка!");
                }
            }
            try
            {
                SerialPort.Read(ModbusMessage, 0, ModbusMessage.Length);
            }
            catch (TimeoutException ex)
            {
                MessageBox.Show("Устройство не ответило на запрос. Проверьте подключение устройства. Подробнее о возникшей исключительной ситуации: " + "\n\n" + ex.Message, "Ошибка!");
            }

            SerialPort.Close();
        }

        //обработка события "пришли данные на COM-порт"
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int buferSize = sp.BytesToRead; // получаем количество пришедших байтов данных в буфере приема

            //считываем побайтно:
            for (int i = 0; i < length; i++)
            {
                //https://qna.habr.com/q/199341
            }
            //sp.ReadExisting();
            // int bytesQuantity;
            // bytesQuantity = 
        }

        public void SendRequestToSlaveDeviceToReceiveData(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToRead, ushort QuantityOfRegistersToRead)
        {
            SendModbusMessage(BuildModbusMessage(SlaveAddress, ModbusFunctionCode, StartingAddressOfRegisterToRead, QuantityOfRegistersToRead)); //отправка сообщения
            ListenToSlaveResponse(); //прослушка порта
        }

        public void SendCommandToSlaveDevice()
        {
            //позже...
        }






    }
}
