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
        //Переменная которая хранит сообщние-команду Modbus в виде List
        private List<byte> Modbus_Message;
        //Переменная которая хранит сообщние-команду Modbus в виде byte[]
        private byte[] ModbusMessage;
        //Экземпляр класса SerialPort
        private SerialPort SerialPort;
        //Интервал тишины после отправки команды Slave-устройству
        private readonly int SilentInterval;

        //Объявляем делегат
        public delegate void ModbusRTUEventHandler(byte[] buffer);
        //Объявляем событие "получен ответ от SLAVE-устройства"
        public event ModbusRTUEventHandler ResponseReceived;

        //Объявляем делегат
        public delegate void ModbusRTUErrorHandler(string message);
        //Объявляем событие "не корректная контрольная сумма сообщения ответа Slave-устройства"
        public event ModbusRTUErrorHandler BadSignalError;
        //Объявляем событие "устройство не ответило на запрос"
        public event ModbusRTUErrorHandler DeviceNotRespondingError;
        //Объявляем событие "не удалось открыть порт"
        public event ModbusRTUErrorHandler SerialPortOpeningError;

        //Переменная хранит количество повторных попыток отправки 
        //сообщения Modbus по причине некорректной контрольной суммы 
        private int CRC_error_etempt = 0;

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
            SerialPort = new SerialPort(modbusRTUSettings.PortName, modbusRTUSettings.BaudRate, modbusRTUSettings.Parity, modbusRTUSettings.DataBits, modbusRTUSettings.StopBits); // конфигурируем COM-порт
            SerialPort.Handshake = modbusRTUSettings.Handshake;
            SerialPort.ReadTimeout = modbusRTUSettings.ReponseTimeout; //время ожидания ответа устройства на COM-порт
            SerialPort.WriteTimeout = modbusRTUSettings.WriteTimeout; //время ожидания записи данных в COM-порт
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
            byte[] data = new byte[modbusMessage.Length - 2];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = modbusMessage[i];
            }

            ushort CRC = GenerateCRC(modbusMessage); // генерация контрольной суммы

            //Cтарший и младший байт контрольной суммы
            byte CRC_LO_byte = (byte)(CRC & 0xFF);
            byte CRC_HI_byte = (byte)(CRC >> 8);

            //Полученные байты контрольной суммы
            byte received_CRC_LO_byte = modbusMessage[modbusMessage.Length - 2];
            byte received_CRC_HI_byte = modbusMessage[modbusMessage.Length - 1];

            //Сравнение
            if (CRC_LO_byte == received_CRC_LO_byte && CRC_HI_byte == received_CRC_HI_byte)
            {
                res = true;
            }
            return res;
        }

        public void SendRequestToSlaveDeviceToReceiveData(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToRead, ushort QuantityOfRegistersToRead)
        {
            byte[] messageToSend = BuildModbusMessage(SlaveAddress, ModbusFunctionCode, StartingAddressOfRegisterToRead, QuantityOfRegistersToRead); // Формируем массив байт для отправки
            SendModbusMessage(messageToSend); // Отправляем данные
        }

        private void SendCommandToSlaveDevice()
        {
            //позже...
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
                    SerialPortOpeningError?.Invoke($"Возникла ошибка при попытке открыть порт {SerialPort.PortName}. Подробнее о возникшей исключительной ситуации: \n\n {ex.Message}");

                    MessageBox.Show($"Возникла ошибка при попытке открыть порт {SerialPort.PortName}. Подробнее о возникшей исключительной ситуации: " + "\n\n" + ex.Message, "Ошибка!"); // Позже переместить в класс Main
                }
            }
            try
            {
                // Отправляем данные
                SerialPort.Write(modbusMessage, 0, modbusMessage.Length);
            }
            catch (TimeoutException ex)
            {
                DeviceNotRespondingError?.Invoke($"Устройство не ответило на запрос. Проверьте подключение устройства. Подробнее о возникшей исключительной ситуации: \n\n {ex.Message}");

                MessageBox.Show("Устройство не ответило на запрос. Проверьте подключение устройства. Подробнее о возникшей исключительной ситуации: " + "\n\n" + ex.Message, "Ошибка!"); // Позже переместить в класс Main
            }
            
            SerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPortDataReceived); //подписываемся на событие "пришли данные на COM-порт"
        }
        
        //обработка события "пришли данные на COM-порт"
        private void SerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort.DataReceived -= new SerialDataReceivedEventHandler(SerialPortDataReceived); //отписываемся от события "пришли данные на COM-порт"
            if (!SerialPort.IsOpen)
            {
                SerialPort.Open();
            }           
            SerialPort sp = (SerialPort)sender;
            int bufferSize = sp.BytesToRead; // получаем количество пришедших байтов данных в буфере приема
            byte[] buffer = new byte[bufferSize]; //создаем массив байтов
            sp.DiscardNull = false; //не игнорировать пустые байты - 0000 0000

            //считываем побайтно и заполняем массив байтов:
            for (int i = 0; i < bufferSize; i++)
            {
                buffer[i] = (byte)sp.ReadByte();
            }
                        
            //sp.DiscardOutBuffer(); //удаляем данные из буфера приема
            sp.DiscardInBuffer(); //удаляем данные из буфера передачи
            sp.BaseStream.Flush();
            sp.BaseStream.Dispose();

            ResponseReceived?.Invoke(buffer);
            SerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPortDataReceived); //подписываемся на событие "пришли данные на COM-порт"

            // ПРОВЕРКА КОНТРОЛЬНОЙ СУММЫ
            /*
            if (CheckCRC_Correct(buffer)) // Если контрольная сумма сошлась
            {
                CRC_error_etempt = 0; // обнуляем количество попыток

                //просмотреть не прислало ли устройство ошибку
                //потом дописать...

                //отписываемся от события "пришли данные на COM-порт"
                sp.DataReceived -= new SerialDataReceivedEventHandler(SerialPortDataReceived);

                //событие "пришли данные"
                ResponseReceived?.Invoke(buffer);
            }
            else // Если контрольная сумма НЕ сошлась - сделать повторный запрос
            {
                sp.DiscardOutBuffer(); //удаляем данные из буфера приема
                sp.DiscardInBuffer(); //удаляем данные из буфера передачи

                CRC_error_etempt++; // увеличиваем счетчик, считаем попытки

                if (CRC_error_etempt <= 3)
                {
                    SendMessageAgain(SilentInterval);
                }
                else
                {
                    BadSignalError?.Invoke("Программа несколько раз отправила повторные запросы на получение данных, но в ответ получила некорректную контрольную сумму. Проверьте подключение, возможны помехи и наводки на линии передачи данных.");
                }
            }

            async void SendMessageAgain(int interval)
            {
                await Task.Delay(interval);
                SendModbusMessage(this.ModbusMessage);
            }
            */
        }
    }
}
