using Lucene.Net.Util;
using System;

namespace Lucene.Net.Search.Grouping
{
    /// <summary>
    /// Base class for grouping related tests.
    /// </summary>
    // TODO (MvG) : The grouping tests contain a lot of code duplication. Try to move the common code to this class..
    public abstract class AbstractGroupingTestCase : LuceneTestCase
    {
        protected string GenerateRandomNonEmptyString()
        {
            string randomValue;
            do
            {
                // B/c of DV based impl we can't see the difference between an empty string and a null value.
                // For that reason we don't generate empty string
                // groups.
                randomValue = TestUtil.RandomRealisticUnicodeString(Random());
                //randomValue = TestUtil.randomSimpleString(random());
            } while ("".Equals(randomValue, StringComparison.Ordinal));
            return randomValue;
        }
    }
}
