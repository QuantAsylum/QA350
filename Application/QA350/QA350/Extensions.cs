using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QA350
{
    public static class Extensions
    {
        public static double StandardDeviation(this IEnumerable<double> values)
        {
            double avg = values.Average();
            return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
        }

        public static string ToEngineeringNotation(this double d)
        {
            return ToEngineeringNotation(d, "F4");
        }

        public static string ToEngineeringNotation(this double d, string formatString)
        {
            //string formatString = "F3";
            double exponent = Math.Log10(Math.Abs(d));
            if (Math.Abs(d) >= 1)
            {
                switch ((int)Math.Floor(exponent))
                {
                    case 0:
                    case 1:
                    case 2:
                        return d.ToString(formatString);
                    case 3:
                    case 4:
                    case 5:
                        return (d / 1e3).ToString(formatString) + "k";
                    case 6:
                    case 7:
                    case 8:
                        return (d / 1e6).ToString(formatString) + "M";
                    case 9:
                    case 10:
                    case 11:
                        return (d / 1e9).ToString(formatString) + "G";
                    case 12:
                    case 13:
                    case 14:
                        return (d / 1e12).ToString(formatString) + "T";
                    case 15:
                    case 16:
                    case 17:
                        return (d / 1e15).ToString(formatString) + "P";
                    case 18:
                    case 19:
                    case 20:
                        return (d / 1e18).ToString(formatString) + "E";
                    case 21:
                    case 22:
                    case 23:
                        return (d / 1e21).ToString(formatString) + "Z";
                    default:
                        return (d / 1e24).ToString(formatString) + "Y";
                }
            }
            else if (Math.Abs(d) > 0)
            {
                switch ((int)Math.Floor(exponent))
                {
                    case -1:
                    case -2:
                    case -3:
                        return (d * 1e3).ToString(formatString) + "m";
                    case -4:
                    case -5:
                    case -6:
                        return (d * 1e6).ToString(formatString) + "μ";
                    case -7:
                    case -8:
                    case -9:
                        return (d * 1e9).ToString(formatString) + "n";
                    case -10:
                    case -11:
                    case -12:
                        return (d * 1e12).ToString(formatString) + "p";
                    case -13:
                    case -14:
                    case -15:
                        return (d * 1e15).ToString(formatString) + "f";
                    case -16:
                    case -17:
                    case -18:
                        return (d * 1e15).ToString(formatString) + "a";
                    case -19:
                    case -20:
                    case -21:
                        return (d * 1e15).ToString(formatString) + "z";
                    default:
                        return (d * 1e15).ToString(formatString) + "y";
                }
            }
            else
            {
                return "0";
            }
        }
    }
}
