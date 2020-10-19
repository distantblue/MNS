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
        public static string DataDirectoryName = @"AppData";
        public static string DataFileName = @"Measuring_data";
        public static string DataFileExt = @"csv";
        public static string TempDirectoryName = @"Temp";
        public static string TempDataFileName = @"Data";

        public static void CreateNewDataFile()
        {
            // СОЗДАЕМ КАТАЛОГ ГДЕ ХРАНИТСЯ ФАЙЛ ДАННЫХ
            DirectoryInfo directoryInfo = new DirectoryInfo(TempDirectoryName);
            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            // СОЗДАЕМ ФАЙЛ
            File.Create(TempDirectoryName + @"\" + TempDataFileName + "." + DataFileExt).Dispose(); // Освобождаем все ресурсы

            // Составляем строку колонок в файле
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("#;");
            stringBuilder.Append("Time;");
            stringBuilder.Append("Equiv. circuit;");
            stringBuilder.Append("Freq. [Hz];");
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
                StreamWriter streamWriter = new StreamWriter(TempDirectoryName + @"\" + TempDataFileName + "." + DataFileExt, true, Encoding.ASCII);
                streamWriter.WriteLine(title);
                streamWriter.Dispose();
            }
            catch (FileNotFoundException)
            {

            }
        }

        public static void ClearTempDirectory()
        {
            File.Delete(TempDirectoryName + @"\" + TempDataFileName + "." + DataFileExt);
        }

        public static void SaveDataRow(string dataRow)
        {
            // Вписываем в файл заголовоки колонок
            try
            {
                StreamWriter streamWriter = new StreamWriter(TempDirectoryName + @"\" + TempDataFileName + "."+DataFileExt, true, Encoding.ASCII);
                streamWriter.WriteLine(dataRow);
                streamWriter.Dispose();
            }
            catch (FileNotFoundException)
            {

            }
        }
    }
}
