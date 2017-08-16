namespace Lucene.Net.Support
{
    public static class StringExtensions
    {
        public static string Intern(this string value)
        {
#if NETSTANDARD
            return value;
#else
            return string.Intern(value);
#endif
        }
    }
}
