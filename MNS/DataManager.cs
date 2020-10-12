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
        public static string DataFilePath = @"AppData";
        public static string DataFileName = @"MeasData";
        public static string TempDataFilePath = @"Temp";
        public static string TempDataFileName = @"Data.csv";

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
            File.Create(DataFilePath + @"\" + TempDataFileName).Dispose(); // Освобождаем все ресурсы

            // Составляем строку колонок в файле
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("#;");
            stringBuilder.Append("Time;");
            stringBuilder.Append("Equiv. circuit;");
            stringBuilder.Append("Frequency [Hz];");
            stringBuilder.Append("R [Ohm];");
            stringBuilder.Append("tg R;"); //+  +
            stringBuilder.Append("L [H];");
            stringBuilder.Append("tg L;"); // + "\u03C6" +
            stringBuilder.Append("C [F];");
            stringBuilder.Append("tg C;");
            stringBuilder.Append("M;");
            stringBuilder.Append("tg M;");

            string title = stringBuilder.ToString();

            // Вписываем в файл заголовоки колонок
            try
            {
                StreamWriter streamWriter = new StreamWriter(TempDataFilePath + @"\" + TempDataFileName, true, Encoding.ASCII);
                streamWriter.WriteLine(title);
                streamWriter.Dispose();
            }
            catch (FileNotFoundException)
            {

            }
        }

        public static void ClearTempDirectory()
        {
            File.Delete(TempDataFilePath + @"\" + TempDataFileName);
        }

        public static void SaveDataRow(string dataRow)
        {
            // Вписываем в файл заголовоки колонок
            try
            {
                StreamWriter streamWriter = new StreamWriter(TempDataFilePath + @"\" + TempDataFileName, true, Encoding.ASCII);
                streamWriter.WriteLine(dataRow);
                streamWriter.Dispose();
            }
            catch (FileNotFoundException)
            {

            }
        }
    }
}
