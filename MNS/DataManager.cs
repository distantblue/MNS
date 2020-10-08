using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNS
{
    static class DataManager
    {
        static string DataFilePath = @"Temp";
        static string DataFileName = @"Data.csv";

        public static void CreateNewDataFile()
        {
            // СОЗДАЕМ КАТАЛОГ ГДЕ ХРАНИТСЯ ФАЙЛ ДАННЫХ
            DirectoryInfo directoryInfo = new DirectoryInfo(DataFilePath);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }
            // СОЗДАЕМ ФАЙЛ 
            //File.Create(DataFilePath + @"\" + DataFileName);
            File.Create(DataFilePath + @"\" + DataFileName).Dispose(); // Освобождаем все ресурсы

            // Составляем заголовки колонок в файле
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("#,");
            stringBuilder.Append("Время,");
            stringBuilder.Append("Схема замещения,");
            stringBuilder.Append("Частота [Гц],");
            stringBuilder.Append("R [Ом],");
            stringBuilder.Append("tg" + "\u03B4" + "R,");
            stringBuilder.Append("L, [Гн],");
            stringBuilder.Append("tg" + "\u03C6" + "L,");
            stringBuilder.Append("C [Ф],");
            stringBuilder.Append("tg" + "\u03C6" + "С,");
            stringBuilder.Append("М,");
            stringBuilder.Append("tg" + "\u03C6" + "M,");

            string title = stringBuilder.ToString();

            // Вписываем в файл заголовоки колонок
            try
            {
                StreamWriter streamWriter = new StreamWriter(DataFilePath + @"\" + DataFileName, true, Encoding.Unicode);
                streamWriter.WriteLine(title);
                streamWriter.Dispose();
            }
            catch (FileNotFoundException)
            {
                
            }
        }

        static void AppendDataSet()
        {
           
        }
    }
}
