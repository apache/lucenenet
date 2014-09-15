using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Suggest.Fst
{
    /// <summary>
    /// Collects <seealso cref="BytesRef"/> and then allows one to iterate over their sorted order. Implementations
    /// of this interface will be called in a single-threaded scenario.
    /// </summary>
    public interface BytesRefSorter
    {
        /// <summary>
        /// Adds a single suggestion entry (possibly compound with its bucket).
        /// </summary>
        /// <exception cref="IOException"> If an I/O exception occurs. </exception>
        /// <exception cref="InvalidOperationException"> If an addition attempt is performed after
        /// a call to <seealso cref="#iterator()"/> has been made. </exception>
        void Add(BytesRef utf8);

        /// <summary>
        /// Sorts the entries added in <seealso cref="#add(BytesRef)"/> and returns 
        /// an iterator over all sorted entries.
        /// </summary>
        /// <exception cref="IOException"> If an I/O exception occurs. </exception>
        BytesRefIterator Iterator();

        /// <summary>
        /// Comparator used to determine the sort order of entries.
        /// </summary>
        IComparer<BytesRef> Comparator { get; }
    }
}
