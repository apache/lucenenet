using J2N;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Utilities for parsing and formatting random seeds.
    /// </summary>
    internal static class SeedUtils
    {
        /// <summary>
        /// Format a single <paramref name="seed"/>.
        /// </summary>
        // LUCENENET: Our format deviates from the Java randomizedtesting implementation
        public static string FormatSeed(long seed)
            => string.Concat("0x", seed.ToHexString());
    }
}
