using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Extension of IBits for live documents.
    /// </summary>
    public interface IMutableBits : IBits
    {
        /// <summary>
        /// Sets the bit specified by <paramref name="index"/> to false.
        /// </summary>
        /// <param name="index">index, should be non-negative and &lt; length. </param>
        public void Clear(int index);
    }
}
