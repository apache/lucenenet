namespace Lucene.Net.QueryParsers.Flexible.Core.Util
{
    /// <summary>
    /// String manipulation routines
    /// </summary>
    public sealed class StringUtils
    {
        public static string ToString(object obj)
        {
            if (obj != null)
            {
                return obj.ToString();
            }
            else
            {
                return null;
            }
        }
    }
}
