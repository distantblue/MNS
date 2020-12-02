using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNS
{
    public static class Statistics
    {
        internal static float GetMeanValue(float[] array)
        {
            float sum = 0;

            foreach (var item in array)
            {
                sum += item;
            }
            float result = sum / array.Length;

            return result;
        }
        
        internal static float GetStDev(float[] array)
        {
           
            return (float)Math.Sqrt(GetDispSquare(array));
        }

        internal static float GetStError(float[] array)
        {
            return GetStDev(array) / (float)Math.Sqrt(array.Length);
        }

        internal static float GetDispSquare(float[] array)
        {
            float sumOfSquares = 0;
            float mean = GetMeanValue(array);

            foreach (var item in array)
            {
                sumOfSquares += (item - mean) * (item - mean);
            }
            return sumOfSquares / (array.Length - 1);
        }

        internal static float GetMinValue(float[] array)
        {
            float min = array[0];
            foreach (var item in array)
            {
                if (item < min)
                {
                    min = item;
                }
            }
            return min;
        }

        internal static float GetMaxValue(float[] array)
        {
            float max = array[0];
            foreach (var item in array)
            {
                if (item > max)
                {
                    max = item;
                }
            }
            return max;
        }
    }
}
