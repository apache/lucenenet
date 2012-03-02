using System;
using System.Globalization;

namespace Lucene.Net.Support
{
    /// <summary>
    /// 
    /// </summary>
    public class Double
    {
        public static System.Double Parse(System.String s)
        {
            try
            {
                return System.Double.Parse(s.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            }
            catch (OverflowException)
            {
                return System.Double.MaxValue;
            }
        }
    }
}