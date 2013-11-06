using Lucene.Net.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public sealed class ArraysPool
    {
        private readonly ConcurrentQueue<int[]> intsPool;
        private readonly ConcurrentQueue<float[]> floatsPool;
        public readonly int arrayLength;

        public ArraysPool(int arrayLength, int maxArrays)
        {
            if (maxArrays == 0)
            {
                throw new ArgumentException(@"maxArrays cannot be 0 - don't use this class if you don't intend to pool arrays");
            }

            this.arrayLength = arrayLength;
            this.intsPool = new ConcurrentQueue<int[]>();
            this.floatsPool = new ConcurrentQueue<float[]>();
        }

        public int[] AllocateIntArray()
        {
            int[] arr;
            if (!intsPool.TryDequeue(out arr))
            {
                return new int[arrayLength];
            }

            Arrays.Fill(arr, 0);
            return arr;
        }

        public float[] AllocateFloatArray()
        {
            float[] arr;
            if (!floatsPool.TryDequeue(out arr))
            {
                return new float[arrayLength];
            }

            Arrays.Fill(arr, 0F);
            return arr;
        }

        public void Free(int[] arr)
        {
            if (arr != null)
            {
                intsPool.Enqueue(arr);
            }
        }

        public void Free(float[] arr)
        {
            if (arr != null)
            {
                floatsPool.Enqueue(arr);
            }
        }
    }
}
