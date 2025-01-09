using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Lifecycle stages for tracking resources.
    /// </summary>
    public enum LifecycleScope // From randomizedtesing
    {
        /// <summary>
        /// A single test case.
        /// </summary>
        TEST,

        /// <summary>
        /// A single suite (class).
        /// </summary>
        SUITE
    }
}
