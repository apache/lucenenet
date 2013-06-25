using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class ParallelPostingsArray
    {
        internal const int BYTES_PER_POSTING = 3 * RamUsageEstimator.NUM_BYTES_INT;

        internal readonly int size;
        internal readonly int[] textStarts;
        internal readonly int[] intStarts;
        internal readonly int[] byteStarts;

        internal ParallelPostingsArray(int size)
        {
            this.size = size;
            textStarts = new int[size];
            intStarts = new int[size];
            byteStarts = new int[size];
        }

        internal virtual int BytesPerPosting
        {
            get { return BYTES_PER_POSTING; }
        }

        internal virtual ParallelPostingsArray NewInstance(int size)
        {
            return new ParallelPostingsArray(size);
        }

        internal ParallelPostingsArray Grow()
        {
            int newSize = ArrayUtil.Oversize(size + 1, BytesPerPosting);
            ParallelPostingsArray newArray = NewInstance(newSize);
            CopyTo(newArray, size);
            return newArray;
        }

        internal virtual void CopyTo(ParallelPostingsArray toArray, int numToCopy)
        {
            Array.Copy(textStarts, 0, toArray.textStarts, 0, numToCopy);
            Array.Copy(intStarts, 0, toArray.intStarts, 0, numToCopy);
            Array.Copy(byteStarts, 0, toArray.byteStarts, 0, numToCopy);
        }
    }
}
