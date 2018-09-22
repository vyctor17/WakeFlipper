using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestePoE
{
        public static class MyListExtensions
        {
            public static decimal Mean(this List<decimal> values)
            {
                return values.Count == 0 ? 0 : values.Mean(0, values.Count);
            }

            public static decimal Mean(this List<decimal> values, int start, int end)
            {
                decimal s = 0;

                for (int i = start; i < end; i++)
                {
                    s += values[i];
                }

                return s / (end - start);
            }

            public static decimal Variance(this List<decimal> values)
            {
                return values.Variance(values.Mean(), 0, values.Count);
            }

            public static decimal Variance(this List<decimal> values, decimal mean)
            {
                return values.Variance(mean, 0, values.Count);
            }

            public static decimal Variance(this List<decimal> values, decimal mean, int start, int end)
            {
                decimal variance = 0;

                for (int i = start; i < end; i++)
                {
                    variance += (decimal)Math.Pow((double)(values[i] - mean), (double)2);
                }

                int n = end - start;
                if (start > 0) n -= 1;

                return variance / (n);
            }

            public static decimal StandardDeviation(this List<decimal> values)
            {
                return values.Count == 0 ? 0 : values.StandardDeviation(0, values.Count);
            }

            public static decimal StandardDeviation(this List<decimal> values, int start, int end)
            {
                decimal mean = values.Mean(start, end);
                decimal variance = values.Variance(mean, start, end);

                return (decimal)Math.Sqrt((double)variance);
            }
        }
    }