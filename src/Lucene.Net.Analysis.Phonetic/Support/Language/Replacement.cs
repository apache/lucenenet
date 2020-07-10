using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Phonetic.Language
{
    /// <summary>
    /// Provides an easy means to cache a culture-invariant pre-compiled regular expression
    /// and its replacement value. This class doesn't do any caching itself, it is meant to
    /// be created within an object initializer and stored as a static reference.
    /// </summary>
    internal class Replacement
    {
        private readonly Regex regex;
        private readonly string _replacement;

        public Replacement(string pattern, string replacement)
        {
            regex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
            this._replacement = replacement;
        }

        public string Replace(string input)
        {
            return regex.Replace(input, _replacement);
        }
    }
}
