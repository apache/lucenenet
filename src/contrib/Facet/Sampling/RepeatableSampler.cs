using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Sampling
{
    public class RepeatableSampler : Sampler
    {
        public RepeatableSampler(SamplingParams params_renamed)
            : base(params_renamed)
        {
        }

        protected override SampleResult CreateSample(IScoredDocIDs docids, int actualSize, int sampleSetSize)
        {
            int[] sampleSet = null;
            try
            {
                sampleSet = RepeatableSample(docids, actualSize, sampleSetSize);
            }
            catch (IOException e)
            {
                Trace.TraceWarning(@"sampling failed: " + e.Message + @" - falling back to no sampling!", e);
                
                return new SampleResult(docids, 1.0);
            }

            IScoredDocIDs sampled = ScoredDocIdsUtils.CreateScoredDocIDsSubset(docids, sampleSet);
            Debug.WriteLine(@"******************** " + sampled.Size);

            return new SampleResult(sampled, sampled.Size / (double)docids.Size);
        }

        private static int[] RepeatableSample(IScoredDocIDs collection, int collectionSize, int sampleSize)
        {
            return RepeatableSample(collection, collectionSize, sampleSize, Algorithm.HASHING, Sorted.NO);
        }

        private static int[] RepeatableSample(IScoredDocIDs collection, int collectionSize, int sampleSize, Algorithm algorithm, Sorted sorted)
        {
            if (collection == null)
            {
                throw new IOException(@"docIdSet is null");
            }

            if (sampleSize < 1)
            {
                throw new IOException(@"sampleSize < 1 (" + sampleSize + @")");
            }

            if (collectionSize < sampleSize)
            {
                throw new IOException(@"collectionSize (" + collectionSize + @") less than sampleSize (" + sampleSize + @")");
            }

            int[] sample = new int[sampleSize];
            long[] times = new long[4];
            if (algorithm == Algorithm.TRAVERSAL)
            {
                Sample1(collection, collectionSize, sample, times);
            }
            else if (algorithm == Algorithm.HASHING)
            {
                Sample2(collection, collectionSize, sample, times);
            }
            else
            {
                throw new ArgumentException(@"Invalid algorithm selection");
            }

            if (sorted == Sorted.YES)
            {
                Array.Sort(sample);
            }

            if (returnTimings)
            {
                times[3] = DateTime.UtcNow.CurrentTimeMillis();
                Debug.WriteLine(@"Times: " + (times[1] - times[0]) + @"ms, " + (times[2] - times[1]) + @"ms, " + (times[3] - times[2]) + @"ms");
            }

            return sample;
        }

        private static void Sample1(IScoredDocIDs collection, int collectionSize, int[] sample, long[] times)
        {
            IScoredDocIDsIterator it = collection.Iterator();
            if (returnTimings)
            {
                times[0] = DateTime.UtcNow.CurrentTimeMillis();
            }

            int sampleSize = sample.Length;
            int prime = FindGoodStepSize(collectionSize, sampleSize);
            int mod = prime % collectionSize;
            if (returnTimings)
            {
                times[1] = DateTime.UtcNow.CurrentTimeMillis();
            }

            int sampleCount = 0;
            int index = 0;
            for (; sampleCount < sampleSize; )
            {
                if (index + mod < collectionSize)
                {
                    for (int i = 0; i < mod; i++, index++)
                    {
                        it.Next();
                    }
                }
                else
                {
                    index = index + mod - collectionSize;
                    it = collection.Iterator();
                    for (int i = 0; i < index; i++)
                    {
                        it.Next();
                    }
                }

                sample[sampleCount++] = it.DocID;
            }

            if (returnTimings)
            {
                times[2] = DateTime.UtcNow.CurrentTimeMillis();
            }
        }

        private static int FindGoodStepSize(int collectionSize, int sampleSize)
        {
            int i = (int)Math.Sqrt(collectionSize);
            if (sampleSize < i)
            {
                i = collectionSize / sampleSize;
            }

            do
            {
                i = FindNextPrimeAfter(i);
            }
            while (collectionSize % i == 0);
            return i;
        }

        private static int FindNextPrimeAfter(int n)
        {
            n += (n % 2 == 0) ? 1 : 2;
        
            for (; ; n += 2)
            {
                bool shouldContinueOuter = false;

                int sri = (int)(Math.Sqrt(n));

                for (int primeIndex = 0; primeIndex < N_PRIMES; primeIndex++)
                {
                    int p = primes[primeIndex];
                    if (p > sri)
                    {
                        return n;
                    }

                    if (n % p == 0)
                    {
                        shouldContinueOuter = true;
                        break;
                    }
                }

                if (shouldContinueOuter)
                    continue;

                for (int p = primes[N_PRIMES - 1] + 2; ; p += 2)
                {
                    if (p > sri)
                    {
                        return n;
                    }

                    if (n % p == 0)
                    {
                        shouldContinueOuter = true;
                        break;
                    }
                }

                if (shouldContinueOuter)
                    continue;
            }
        }

        private static readonly int N_PRIMES = 4000;
        private static int[] primes = new int[N_PRIMES];
        static RepeatableSampler()
        {
            primes[0] = 3;
            for (int count = 1; count < N_PRIMES; count++)
            {
                primes[count] = FindNextPrimeAfter(primes[count - 1]);
            }
        }

        private static void Sample2(IScoredDocIDs collection, int collectionSize, int[] sample, long[] times)
        {
            if (returnTimings)
            {
                times[0] = DateTime.UtcNow.CurrentTimeMillis();
            }

            int sampleSize = sample.Length;
            IntPriorityQueue pq = new IntPriorityQueue(sampleSize);
            IScoredDocIDsIterator it = collection.Iterator();
            MI mi = null;
            while (it.Next())
            {
                if (mi == null)
                {
                    mi = new MI();
                }

                mi.value = (int)(it.DocID * PHI_32) & 0x7FFFFFFF;
                mi = pq.InsertWithOverflow(mi);
            }

            if (returnTimings)
            {
                times[1] = DateTime.UtcNow.CurrentTimeMillis();
            }

            Object[] heap = pq.GetHeap();
            for (int si = 0; si < sampleSize; si++)
            {
                sample[si] = (int)(((MI)heap[si + 1]).value * PHI_32I) & 0x7FFFFFFF;
            }

            if (returnTimings)
            {
                times[2] = DateTime.UtcNow.CurrentTimeMillis();
            }
        }

        private class MI
        {
            internal MI()
            {
            }

            public int value;
        }

        private class IntPriorityQueue : Lucene.Net.Util.PriorityQueue<MI>
        {
            public IntPriorityQueue(int size)
                : base(size)
            {
            }

            public virtual Object[] GetHeap()
            {
                return GetHeapArray();
            }

            public override bool LessThan(MI o1, MI o2)
            {
                return o1.value < o2.value;
            }
        }

        private enum Algorithm
        {
            TRAVERSAL,
            HASHING
        }

        private enum Sorted
        {
            YES,
            NO
        }

        private static readonly long PHI_32 = 2654435769L;
        private static readonly long PHI_32I = 340573321L;
        private static bool returnTimings = false;
    }
}
