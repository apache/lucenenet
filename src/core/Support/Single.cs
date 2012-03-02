using System;
using System.Globalization;

namespace Lucene.Net.Support
{
    /// <summary>
    /// 
    /// </summary>
    public class Single
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="style"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s, System.Globalization.NumberStyles style, System.IFormatProvider provider)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1), style, provider);
                else
                    return System.Single.Parse(s, style, provider);
            }
            catch (System.FormatException fex)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s, System.IFormatProvider provider)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1), provider);
                else
                    return System.Single.Parse(s, provider);
            }
            catch (System.FormatException fex)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s, System.Globalization.NumberStyles style)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1), style);
                else
                    return System.Single.Parse(s, style);
            }
            catch (System.FormatException fex)
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1).Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
                else
                    return System.Single.Parse(s.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            }
            catch (System.FormatException fex)
            {
                throw;
            }
        }

        public static bool TryParse(System.String s, out float f)
        {
            bool ok = false;

            if (s.EndsWith("f") || s.EndsWith("F"))
                ok = System.Single.TryParse(s.Substring(0, s.Length - 1).Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out f);
            else
                ok = System.Single.TryParse(s.Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator), out f);

            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string ToString(float f)
        {
            return f.ToString().Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToString(float f, string format)
        {
            return f.ToString(format).Replace(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".");
        }

        public static int FloatToIntBits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        public static float IntBitsToFloat(int value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
        }
    }
}