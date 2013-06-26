using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    public static class ReaderUtil
    {
        public static IndexReaderContext GetTopLevelContext(IndexReaderContext context)
        {
            while (context.parent != null)
            {
                context = context.parent;
            }
            return context;
        }

        public static int SubIndex(int n, int[] docStarts)
        {
            // find
            // searcher/reader for doc n:
            int size = docStarts.Length;
            int lo = 0; // search starts array
            int hi = size - 1; // for first element less than n, return its index
            while (hi >= lo)
            {
                int mid = Number.URShift((lo + hi), 1);
                int midValue = docStarts[mid];
                if (n < midValue)
                    hi = mid - 1;
                else if (n > midValue)
                    lo = mid + 1;
                else
                { // found a match
                    while (mid + 1 < size && docStarts[mid + 1] == midValue)
                    {
                        mid++; // scan to last match
                    }
                    return mid;
                }
            }
            return hi;
        }

        public static int SubIndex(int n, IList<AtomicReaderContext> leaves)
        { // find
            // searcher/reader for doc n:
            int size = leaves.Count;
            int lo = 0; // search starts array
            int hi = size - 1; // for first element less than n, return its index
            while (hi >= lo)
            {
                int mid = Number.URShift((lo + hi), 1);
                int midValue = leaves[mid].docBase;
                if (n < midValue)
                    hi = mid - 1;
                else if (n > midValue)
                    lo = mid + 1;
                else
                { // found a match
                    while (mid + 1 < size && leaves[mid + 1].docBase == midValue)
                    {
                        mid++; // scan to last match
                    }
                    return mid;
                }
            }
            return hi;
        }
    }
}
