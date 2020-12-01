using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MNS
{
    public static class Statistics
    {
        internal static double GetMeanValue(double[] array)
        {
            double sum = 0;

            foreach (var item in array)
            {
                sum += item;
            }
            double result = sum / array.Length;

            return result;
        }

        internal static double GetStDev(double[] array)
        {
            double sumOfSquares = 0;
            double mean = GetMeanValue(array);

            foreach (var item in array)
            {
                sumOfSquares += Math.Pow((item - mean), 2);
            }
            return Math.Sqrt(sumOfSquares / (array.Length - 1));
        }
                
        private static double GetStError(double[] array)
        {
            return GetStDev(array) / Math.Sqrt(array.Length);
        }

        private static double GetMinValue(double[] array)
        {
            double min = array[0];
            foreach (var item in array)
            {
                if (item < min)
                {
                    min = item;
                }
            }
            return min;
        }

        private static double GetMaxValue(double[] array)
        {
            double max = array[0];
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
