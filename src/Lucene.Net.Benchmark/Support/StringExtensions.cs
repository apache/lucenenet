namespace Lucene.Net.Support
{
    public static class StringExtensions
    {
        public static string Intern(this string value)
        {
#if NETSTANDARD1_5
            return value; // LUCENENET TODO: Fix string interning
#else
            return string.Intern(value);
#endif
        }
    }
}
