using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNS
{
    class BitReader
    {
        /// <summary>
        /// This method converts uint (binary32 IEEE 754-1985 single format) value to C# float value 
        /// </summary>
        /// <param name="value">uint (binary32 IEEE 754-1985 single format) value</param>
        /// <returns>C# float value</returns>
        public static float IEEE745_to_float(uint value)
        {
            int sign = (int)(value >> 31); // 0 - положительный знак, 1 - отрицательный
            int exponent = (int)((value >> 23) & 0xFF); // Экспонента 
            int mantisa = (int)(value & 0x7FFFFF); // Мантиса числа


            float res = ((-1) ^ sign) * mantisa * (10 ^ exponent);

            return res;
        }
    }
}
