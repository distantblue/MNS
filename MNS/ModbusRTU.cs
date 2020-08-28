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
        //Интервал тишины после отправки команды Slave-устройству
        private readonly int SilentInterval;

        //Объявляем делегат
        public delegate void ModbusRTUEventHandler(byte[] buffer);
        //Объявляем событие "получен ответ от SLAVE-устройства"
        public event ModbusRTUEventHandler ResponseReceived;
        //Объявляем событие "не получен ответ от SLAVE-устройства"
        //public event ModbusRTUEventHandler ResponseError;

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
            //SerialPort.ReadTimeout = ModbusRTUSettings.ReponseTimeout; //время ожидания ответа на COM-порт
            this.SilentInterval = modbusRTUSettings.SilentInterval; //интервал тишины после отправки данных по COM-порт    
        }

        private byte[] BuildModbusMessage(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToRead, ushort QuantityOfRegistersToRead)
        {
            Modbus_Message = new List<byte>();
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

        private byte[] BuildModbusMessage(byte[] bytesToWrite, byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegister, ushort QuantityOfRegisters)
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

        private bool CheckCRC_Correct(byte[] modbusMessage)
        {
            bool res = false;
            byte[] data = new byte[modbusMessage.Length-2];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = modbusMessage[i];
            }

            ushort CRC = GenerateCRC(modbusMessage); // генерация контрольной суммы

            //Cтарший и младший байт контрольной суммы
            byte CRC_LO_byte = (byte)(CRC & 0xFF);
            byte CRC_HI_byte = (byte)(CRC >> 8);

            //Полученные байты контрольной суммы
            byte received_CRC_LO_byte = modbusMessage[modbusMessage.Length-2]; 
            byte received_CRC_HI_byte = modbusMessage[modbusMessage.Length - 1];

            //Сравнение
            if (CRC_LO_byte == received_CRC_LO_byte && CRC_HI_byte== received_CRC_HI_byte)
            {
                res = true;
            }
            return res;
        }

        public void SendModbusMessage(byte[] modbusMessage)
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
            //try
            //{
                SerialPort.Write(modbusMessage, 0, modbusMessage.Length);
            //}
            //catch (TimeoutException ex)
            //{
            //    MessageBox.Show("Устройство не ответило на запрос. Проверьте подключение устройства. Подробнее о возникшей исключительной ситуации: " + "\n\n" + ex.Message, "Ошибка!");
            //}
           

            //SerialPort.DiscardOutBuffer(); //удаляем данные из буфера приема
            //SerialPort.DiscardInBuffer(); //удаляем данные из буфера передачи
            //SerialPort.BaseStream.Flush();
            //SerialPort.BaseStream.Dispose();
            SerialPort.Dispose(); //освобождаем ресурсы используемые COM-портом
            SerialPort.Close();

            Thread.Sleep(1000); //выдерживаем интервал тишины после отправки сообщения Modbus
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
            SerialPort.Dispose(); //освобождаем ресурсы используемые COM-портом
            SerialPort.Close();
        }

        //обработка события "пришли данные на COM-порт"
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            int bufferSize = sp.BytesToRead; // получаем количество пришедших байтов данных в буфере приема
            byte[] buffer = new byte[bufferSize]; //создаем массив байтов

            sp.DiscardNull = false; //не игнорировать пустые байты - 0000 0000

            //считываем побайтно и заполняем массив байтов:
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = (byte)sp.ReadByte();
            }

            sp.DataReceived -= new SerialDataReceivedEventHandler(SerialPortDataReceived); //отписываемся от события "пришли данные на COM-порт"
            //sp.DiscardOutBuffer(); //удаляем данные из буфера приема
            //sp.DiscardInBuffer(); //удаляем данные из буфера передачи
            sp.Dispose(); //освобождаем ресурсы используемые COM-портом
            sp.Close();

            //проверка контрольной суммы
            if (CheckCRC_Correct(buffer))
            {
                //просмотреть не прислало ли устройство ошибку
                //потом дописать...

                //событие пришли данные и они корректные
                ResponseReceived?.Invoke(buffer);
            }
            else
            {
                //пришли некорректные данные - сделать повторный запрос
            }
        }

        public void SendRequestToSlaveDeviceToReceiveData(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToRead, ushort QuantityOfRegistersToRead)
        {
            SendModbusMessage(BuildModbusMessage(SlaveAddress, ModbusFunctionCode, StartingAddressOfRegisterToRead, QuantityOfRegistersToRead)); //отправка сообщения
            //ListenToSlaveResponse(); //прослушка порта
        }

        public void SendCommandToSlaveDevice()
        {
            //позже...
        }






    }
}
