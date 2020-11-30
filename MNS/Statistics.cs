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
            double res;
            double sum = 0;

            foreach (var item in array)
            {
                sum += item;
            }
            res = sum / array.Length;

            return res;
        }

        internal static double GetStDev(double[] array)
        {
            double res;
            double avg = GetMeanValue(array);
            double sum = 0;

            foreach (var item in array)
            {
                double subs = item - avg;
                double pow = Math.Pow(subs, 2);
                sum += pow;
            }
            double expr = sum / (array.Length - 1);
            res = Math.Sqrt(expr);

            return res;
        }
    }
}
