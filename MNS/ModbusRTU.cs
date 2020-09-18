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
        // Переменная которая хранит сообщние-команду Modbus в виде List
        private List<byte> Modbus_Message;
        // Переменная которая хранит сообщние-команду Modbus в виде byte[]
        private byte[] ModbusMessage;
        // Экземпляр класса SerialPort
        private SerialPort SerialPort;
        // Гарантированный интервал тишины после отправки команды устройству после которого устройство начинает обработку команды
        private readonly int SilentInterval;
        // Интервал после которого начинается считывание поступивших данных на COM порт или вызывается исключение TimeoutException
        private readonly int ResponseTimeout;

        // Объявляем делегат
        public delegate void ModbusRTUEventHandler(byte[] buffer);
        // Объявляем событие "получен ответ от устройства"
        public event ModbusRTUEventHandler ResponseReceived;
        // Объявляем событие "отправлена команда"
        public event ModbusRTUEventHandler RequestSent;

        // Объявляем делегат
        public delegate void ModbusRTUErrorHandler(string message);
        // Объявляем событие "не корректная контрольная сумма сообщения ответа Slave-устройства"
        public event ModbusRTUErrorHandler CRCError;
        // Объявляем событие "устройство не ответило на запрос"
        public event ModbusRTUErrorHandler DeviceNotRespondingError;
        // Объявляем событие "не удалось открыть порт"
        public event ModbusRTUErrorHandler SerialPortOpeningError;
        // Объявляем событие "не удалось записать данные в порт"
        public event ModbusRTUErrorHandler SerialPortWritingError;
        // Объявляем событие "устройство сообщило об ошибке"
        public event ModbusRTUErrorHandler SlaveError;
        // Перемення хранит ожидаемое количество байт данных в ответе устройства
        private int ExpectedQuantityOfDataBytesInResponse;
        // Перемення хранит ожидаемое количество байт в ответе устройства
        private int ExpectedQuantityOfBytesInResponse;

        // Переменная хранит количество повторных попыток отправки 
        // сообщения Modbus по причине некорректной контрольной суммы 
        //private int CRC_error_etempt = 0;

        // Переменная хранит количество повторных попыток отправки 
        // сообщения Modbus по причине "количество полученных байт меньше 5" 
        //private int BytesQuantity_error_etempt = 0;



        // Объявляем событие "не получен ответ от SLAVE-устройства"
        // public event ModbusRTUEventHandler ResponseError;

        /// <summary>
        /// Конструктор класса ModbusRTU
        /// </summary>
        /// <param name="modbusRTUSettings">Экземпляр класса ModbusRTUSettings</param>
        public ModbusRTU(ModbusRTUSettings modbusRTUSettings)
        {
            Modbus_Message = new List<byte>();

            // КОНФИГУРИРОВАНИЕ COM-ПОРТА
            SerialPort = new SerialPort(modbusRTUSettings.PortName, modbusRTUSettings.BaudRate, modbusRTUSettings.Parity, modbusRTUSettings.DataBits, modbusRTUSettings.StopBits);
            SerialPort.Handshake = modbusRTUSettings.Handshake; // Аппаратное рукопожатие
            SerialPort.ReadTimeout = modbusRTUSettings.ReadTimeout; // Время ожидания ответа устройства на COM-порт
            SerialPort.WriteTimeout = modbusRTUSettings.WriteTimeout; // Время ожидания записи данных в COM-порт
            SilentInterval = modbusRTUSettings.SilentInterval; // Гарантированный интервал тишины после отправки данных устройству после которого устройство начинает обработку запроса
            ResponseTimeout = modbusRTUSettings.ResponseTimeout; // Интервал после которого начинается считывание поступивших данных на COM порт или вызывается исключение TimeoutException
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

        public void SendRequestToSlaveDeviceToReceiveData(byte SlaveAddress, byte ModbusFunctionCode, ushort StartingAddressOfRegisterToRead, ushort QuantityOfRegistersToRead, int QuantityOfBytesInReg)
        {
            this.ExpectedQuantityOfDataBytesInResponse = QuantityOfRegistersToRead * QuantityOfBytesInReg; // Ожидаемое количество байтов данных в сообщении ответа устройства
            this.ExpectedQuantityOfBytesInResponse = 5 + ExpectedQuantityOfDataBytesInResponse; // Ожидаемое количество байтов в сообщении ответа устройства
            byte[] messageToSend = BuildModbusMessage(SlaveAddress, ModbusFunctionCode, StartingAddressOfRegisterToRead, QuantityOfRegistersToRead); // Формируем массив байт для отправки
            SendModbusMessage(messageToSend); // Отправляем данные
            ReadResponse(); // Читаем данные 
        }

        private void SendModbusMessage(byte[] modbusMessage)
        {
            // ЕСЛИ ПОРТ ЗАКРЫТ
            if (!SerialPort.IsOpen)
            {
                try
                {
                    SerialPort.Open();
                }
                catch (InvalidOperationException ex)
                {
                    SerialPortOpeningError?.Invoke($"Возникла ошибка при попытке открыть порт {SerialPort.PortName}. Подробнее о возникшей исключительной ситуации: \n\n {ex.Message}");
                }
            }

            // Очищаем буффер исходящих данных (порт может быть уже открытым и там могут быть данные с прошлой отправки)
            SerialPort.DiscardOutBuffer();
            Thread.Sleep(50);

            try
            {
                // Отправляем данные
                SerialPort.Write(modbusMessage, 0, modbusMessage.Length);
            }
            catch (TimeoutException ex) // По истечении WriteTimeout [мс]
            {
                SerialPort.Close(); // Закрыть порт
                SerialPortWritingError?.Invoke($"Произошла ошибка при записи порт. \n\nПодробнее о возникшей исключительной ситуации: \n\n {ex.Message}");
            }
            RequestSent?.Invoke(modbusMessage); // Вызов события "отправлена команда"

            Thread.Sleep(ResponseTimeout); // Задержка выжидания поступления данных на COM порт
        }

        private void ReadResponse()
        {
            // ЕСЛИ ПОРТ ЗАКРЫТ 
            if (!SerialPort.IsOpen)
            {
                return; // Выходим из метода, невозможно читать из порта когда он закрыт
            }

            // ЕСЛИ ПОРТ ОТКРЫТ И В НЕМ ЕСТЬ ДАННЫЕ
            if (SerialPort.BytesToRead >= 5)
            {
                int bufferSize = SerialPort.BytesToRead; // Получаем количество пришедших байтов данных в буффере приема
                byte[] buffer = new byte[bufferSize]; // Создаем массив байтов
                SerialPort.DiscardNull = false; // Не игнорировать пустые байты - 0000 0000

                // Считываем побайтно и заполняем массив байтов:
                for (int i = 0; i < bufferSize; i++)
                {
                    buffer[i] = (byte)SerialPort.ReadByte();
                }

                // ПРОВЕРКА КОНТРОЛЬНОЙ СУММЫ 
                // ЕСЛИ КОНТРОЛЬНАЯ СУММА СОШЛАСЬ
                if (CheckCRC_Correct(buffer))
                {
                    // ЕСЛИ ПРИШЛО ОЖИДАЕМОЕ КОЛИЧЕСТВО БАЙТОВ В ОТВЕТЕ
                    if (bufferSize == ExpectedQuantityOfBytesInResponse)
                    {
                        // Если адрес устройства правильный
                        if (buffer[0] == ModbusMessage[0])
                        {
                            // Если совпадает функция команды и количество запрошенных байтов данных 
                            if (buffer[1] == ModbusMessage[1] && buffer[2] == ExpectedQuantityOfDataBytesInResponse)
                            {
                                // Событие "пришли данные"
                                ResponseReceived?.Invoke(buffer);
                            }
                            else
                            {
                                SendMessageAgain();
                            }
                        }
                        else
                        {
                            SendMessageAgain();
                        }
                    }

                }
                // ЕСЛИ КОНТРОЛЬНАЯ СУММА НЕ СОШЛАСЬ
                else
                {
                    SendMessageAgain();
                }
            }
            else
            {
                SendMessageAgain();
            }
        }

        private bool CheckCRC_Correct(byte[] modbusMessage)
        {
            bool res = false;
            byte[] data = new byte[modbusMessage.Length - 2];

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = modbusMessage[i];
            }

            ushort CRC = GenerateCRC(data); // генерация контрольной суммы

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

        public void Close()
        {
            SerialPort.Close();
        }

        private void SendMessageAgain()
        {
            // Очищаем буффер входящих данных 
            SerialPort.DiscardInBuffer();

            // Пауза для очистки буффера входящих данных
            Thread.Sleep(50);

            // Повторно отправляем сообщение
            SendModbusMessage(this.ModbusMessage);
        }
    }
}

/*
            // ЕСЛИ ПОРТ ЗАКРЫТ 
            if (!SerialPort.IsOpen)
            {
                return; // Выходим из метода, невозможно читать из порта когда он закрыт
            }

            // ЕСЛИ ПОРТ ОТКРЫТ И В НЕМ ЕСТЬ ДАННЫЕ
            if (SerialPort.BytesToRead > 0)
            {
                int bufferSize = SerialPort.BytesToRead; // Получаем количество пришедших байтов данных в буффере приема

                byte[] buffer = new byte[bufferSize]; // Создаем массив байтов




                // ЕCЛИ В БУФФЕРЕ 4 И МЕНЕ БАЙТ - ОШИБКА 
                if (buffer.Length <= 4)
                {
                    BytesQuantity_error_etempt++; // Увеличиваем счетчик, считаем количество повторных попыток отослать команду

                    if (BytesQuantity_error_etempt <= 3)
                    {
                        // Очищаем буффер входящих данных 
                        SerialPort.DiscardInBuffer();

                        SendModbusMessage(this.ModbusMessage); // Повторно отправляем сообщение
                    }
                    else
                    {
                        SlaveError?.Invoke("Программа несколько раз отправила повторные запросы, но в ответ получила сообщения с количеством байт 4 или менее, что является недопустимым.");
                    }
                }
                // ЕCЛИ В БУФФЕРЕ 5 И БОЛЕЕ БАЙТ - ОК 
                else
                {
                    BytesQuantity_error_etempt = 0; // Обнуляем количество попыток

                    SerialPort.DiscardNull = false; // Не игнорировать пустые байты - 0000 0000

                    // Считываем побайтно и заполняем массив байтов:
                    for (int i = 0; i < bufferSize; i++)
                    {
                        buffer[i] = (byte)SerialPort.ReadByte();
                    }

                    // Очищаем буффер входящих данных 
                    SerialPort.DiscardInBuffer();

                    // ПРОВЕРКА КОНТРОЛЬНОЙ СУММЫ 
                    // ЕСЛИ КОНТРОЛЬНАЯ СУММА СОШЛАСЬ
                    if (CheckCRC_Correct(buffer))
                    {
                        CRC_error_etempt = 0; // Обнуляем количество попыток

                        // ПРОВЕРКА АДРЕСНОГО ПОЛЯ SLAVE УСТРОЙСТВА И КОДА КОМАНДЫ
                        // Если адресное поле и код команды сходятся
                        if (buffer[1] == ModbusMessage[1] && buffer[0] == ModbusMessage[0])
                        {
                            // Событие "пришли данные"
                            ResponseReceived?.Invoke(buffer);
                        }
                        // Если во втором байте код ошибки 81 или 82
                        else if (buffer[1] == 81 || buffer[1] == 82)
                        {
                            // Если адресное поле сходится и код ответа 81
                            if (buffer[1] == 81 && buffer[0] == ModbusMessage[0])
                            {
                                switch (buffer[2])
                                {
                                    case 1:
                                        SlaveError?.Invoke("На запрос программы устройство ответило ошибкой \"01\": \n\n\"Запрошенная функция не поддерживается устройством\"");
                                        break;
                                    case 2:
                                        SlaveError?.Invoke("На запрос программы устройство ответило ошибкой \"02\": \n\n\"Запрошенное адресное поле не поддерживается устройством\"");
                                        break;
                                    default:
                                        SlaveError?.Invoke("На запрос программы устройство ответило ошибкой");
                                        break;
                                }
                            }
                            // Если адресное поле сходится и код ответа 82
                            else if (buffer[1] == 82 && buffer[0] == ModbusMessage[0])
                            {
                                switch (buffer[2])
                                {
                                    case 1:
                                        SlaveError?.Invoke("На запрос программы устройство ответило ошибкой \"01\": \n\n\"Запрошенная функция не поддерживается устройством\"");
                                        break;
                                    case 2:
                                        SlaveError?.Invoke("На запрос программы устройство ответило ошибкой \"02\": \n\n\"Запрошенное адресное поле не поддерживается устройством\"");
                                        break;
                                    default:
                                        SlaveError?.Invoke("На запрос программы устройство ответило ошибкой");
                                        break;
                                }
                            }
                        }
                    }
                    // ЕСЛИ КОНТРОЛЬНАЯ СУММА НЕ СОШЛАСЬ
                    else
                    {
                        CRC_error_etempt++; // Увеличиваем счетчик, считаем количество повторных попыток отослать команду

                        if (CRC_error_etempt <= 3)
                        {
                            // Очищаем буффер входящих данных 
                            SerialPort.DiscardInBuffer();

                            SendModbusMessage(this.ModbusMessage); // Повторно отправляем сообщение
                        }
                        else
                        {
                            BadSignalError?.Invoke("Программа несколько раз отправила повторные запросы, но в ответ получила сообщения с некорректной контрольной суммой. \n\nПроверьте подключение, возможны помехи и наводки на линии передачи данных.");
                        }
                    }
                }
            }
            // ЕСЛИ В БУФФЕРЕ НЕТ ДАННЫХ
            else
            {
                DeviceNotRespondingError?.Invoke($"Устройство не ответило на запрос. \n\nПроверьте подключение устройства. Подробнее о возникшей исключительной ситуации: \n\n {new TimeoutException().Message}");
            }*/


