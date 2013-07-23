using System;
using System.Collections;
using Lucene.Net.Search;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestFixedBitSet : LuceneTestCase
    {
        internal virtual void DoGet(BitArray a, FixedBitSet b)
        {
            int max = b.Length;
            for (var i = 0; i < max; i++)
            {
                if (a[i] != b[i])
                {
                    Fail("mismatch: BitArray=[" + i + "]=" + a[i]);
                }
            }
        }

        internal virtual void doNextSetBit(BitArray a, FixedBitSet b)
        {
            int aa = -1, bb = -1;
            do
            {
                aa = a.NextSetBit(aa + 1);
                bb = bb < b.Length - 1 ? b.NextSetBit(bb + 1) : -1;
                Assert.Equals(aa, bb);
            } while (aa >= 0);
        }

        internal virtual void doPrevSetBit(BitArray a, FixedBitSet b)
        {
            int aa = a.Length + new Random().Next(100);
            int bb = aa;
            do
            {
                // aa = a.prevSetBit(aa-1);
                aa--;
                while ((aa >= 0) && (!a[aa]))
                {
                    aa--;
                }
                if (b.Length == 0)
                {
                    bb = -1;
                }
                else if (bb > b.Length - 1)
                {
                    bb = b.PrevSetBit(b.Length - 1);
                }
                else if (bb < 1)
                {
                    bb = -1;
                }
                else
                {
                    bb = bb >= 1 ? b.PrevSetBit(bb - 1) : -1;
                }
                Assert.Equals(aa, bb);
            } while (aa >= 0);
        }

        // test interleaving different FixedBitSetIterator.next()/skipTo()
        internal virtual void DoIterate(BitArray a, FixedBitSet b, int mode)
        {
            if (mode == 1) DoIterate1(a, b);
            if (mode == 2) DoIterate2(a, b);
        }

        internal virtual void DoIterate1(BitArray a, FixedBitSet b)
        {
            int aa = -1, bb = -1;
            var iterator = b.Iterator();
            do
            {
                aa = a.NextSetBit(aa + 1);
                bb = (bb < b.Length && new Random().NextBool()) ? iterator.NextDoc() : iterator.Advance(bb + 1);
                Assert.Equals(aa == -1 ? DocIdSetIterator.NO_MORE_DOCS : aa, bb);
            } while (aa >= 0);
        }

        internal virtual void DoIterate2(BitArray a, FixedBitSet b)
        {
            int aa = -1, bb = -1;
            var iterator = b.Iterator();
            do
            {
                aa = a.NextSetBit(aa + 1);
                bb = new Random().NextBool() ? iterator.NextDoc() : iterator.Advance(bb + 1);
                Assert.Equals(aa == -1 ? DocIdSetIterator.NO_MORE_DOCS : aa, bb);
            } while (aa >= 0);
        }

        internal virtual void DoRandomSets(int maxSize, int iter, int mode)
        {
            BitArray a0 = null;
            FixedBitSet b0 = null;

            var random = new Random();

            for (var i = 0; i < iter; i++)
            {
                int sz = _TestUtil.Next(random, 2, maxSize);
                var a = new BitArray(sz);
                var b = new FixedBitSet(sz);

                // test the various ways of setting bits
                if (sz > 0)
                {
                    var nOper = random.Next(sz);
                    for (var j = 0; j < nOper; j++)
                    {
                        int idx;

                        idx = random.Next(sz);
                        a.Set(idx);
                        b.Set(idx);

                        idx = random.Next(sz);
                        a.Clear(idx);
                        b.Clear(idx);

                        idx = random.Next(sz);
                        a.Flip(idx);
                        b.Flip(idx, idx + 1);

                        idx = random.Next(sz);
                        a.Flip(idx);
                        b.Flip(idx, idx + 1);
                        bool val2 = b[idx];
                        bool val = b.GetAndSet(idx);
                        Assert.IsTrue(val2 == val);
                        Assert.IsTrue(b[idx]);

                        if (!val) b.Clear(idx);
                        Assert.IsTrue(b[idx] == val);
                    }
                }

                // test that the various ways of accessing the bits are equivalent
                DoGet(a, b);

                // test ranges, including possible extension
                int fromIndex, toIndex;
                fromIndex = random.Next(sz / 2);
                toIndex = fromIndex + random.Next(sz - fromIndex);
                var aa = (BitArray)a.Clone(); aa.Flip(fromIndex, toIndex);
                var bb = b.Clone(); bb.Flip(fromIndex, toIndex);

                DoIterate(aa, bb, mode);   // a problem here is from Flip or DoIterate

                fromIndex = random.Next(sz / 2);
                toIndex = fromIndex + random.Next(sz - fromIndex);
                aa = (BitArray)a.Clone(); aa.Clear(fromIndex, toIndex);
                bb = b.Clone(); bb.Clear(fromIndex, toIndex);

                doNextSetBit(aa, bb); // a problem here is from Clear() or nextSetBit

                doPrevSetBit(aa, bb);

                fromIndex = random.Next(sz / 2);
                toIndex = fromIndex + random.Next(sz - fromIndex);
                aa = (BitArray)a.Clone(); aa.Set(fromIndex, toIndex);
                bb = b.Clone(); bb.Set(fromIndex, toIndex);

                doNextSetBit(aa, bb); // a problem here is from set() or nextSetBit

                doPrevSetBit(aa, bb);

                if (b0 != null && b0.Length <= b.Length)
                {
                    Assert.Equals(a.Cardinality(), b.Cardinality());

                    var a_and = (BitArray)a.Clone(); a_and.And(a0);
                    var a_or = (BitArray)a.Clone(); a_or.Or(a0);
                    var a_andn = (BitArray)a.Clone(); a_andn.AndNot(a0);

                    var b_and = b.Clone(); Assert.Equals(b, b_and); b_and.And(b0);
                    var b_or = b.Clone(); b_or.Or(b0);
                    var b_andn = b.Clone(); b_andn.AndNot(b0);

                    Assert.Equals(a0.Cardinality(), b0.Cardinality());
                    Assert.Equals(a_or.Cardinality(), b_or.Cardinality());

                    DoIterate(a_and, b_and, mode);
                    DoIterate(a_or, b_or, mode);
                    DoIterate(a_andn, b_andn, mode);

                    Assert.Equals(a_and.Cardinality(), b_and.Cardinality());
                    Assert.Equals(a_or.Cardinality(), b_or.Cardinality());
                    Assert.Equals(a_andn.Cardinality(), b_andn.Cardinality());
                }

                a0 = a;
                b0 = b;
            }
        }

        // large enough to flush obvious bugs, small enough to run in <.5 sec as part of a
        // larger testsuite.
        [Test]
        public void TestSmall()
        {
            DoRandomSets(AtLeast(1200), AtLeast(1000), 1);
            DoRandomSets(AtLeast(1200), AtLeast(1000), 2);
        }

        // uncomment to run a bigger test (~2 minutes).
        /*
        [Test]
        public void TestBig() {
          DoRandomSets(2000,200000, 1);
          DoRandomSets(2000,200000, 2);
        }
        */

        [Test]
        public void TestEquals()
        {
            var random = new Random();

            // This test can't handle numBits==0:
            var numBits = random.Next(2000) + 1;
            var b1 = new FixedBitSet(numBits);
            var b2 = new FixedBitSet(numBits);
            Assert.IsTrue(b1.Equals(b2));
            Assert.IsTrue(b2.Equals(b1));
            for (var iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                var idx = random.Next(numBits);
                if (!b1[idx])
                {
                    b1.Set(idx);
                    Assert.IsFalse(b1.Equals(b2));
                    Assert.IsFalse(b2.Equals(b1));
                    b2.Set(idx);
                    Assert.IsTrue(b1.Equals(b2));
                    Assert.IsTrue(b2.Equals(b1));
                }
            }

            // try different type of object
            Assert.IsFalse(b1.Equals(new Object()));
        }

        [Test]
        public void TestHashCodeEquals()
        {
            var random = new Random();

            // This test can't handle numBits==0:
            var numBits = random.Next(2000) + 1;
            var b1 = new FixedBitSet(numBits);
            var b2 = new FixedBitSet(numBits);
            Assert.IsTrue(b1.Equals(b2));
            Assert.IsTrue(b2.Equals(b1));
            for (var iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                int idx = random.Next(numBits);
                if (!b1[idx])
                {
                    b1.Set(idx);
                    Assert.IsFalse(b1.Equals(b2));
                    Assert.IsFalse(b1.GetHashCode() == b2.GetHashCode());
                    b2.Set(idx);
                    Assert.Equals(b1, b2);
                    Assert.Equals(b1.GetHashCode(), b2.GetHashCode());
                }
            }
        }

        [Test]
        public void TestSmallBitSets()
        {
            // Make sure size 0-10 bit sets are OK:
            for (var numBits = 0; numBits < 10; numBits++)
            {
                var b1 = new FixedBitSet(numBits);
                var b2 = new FixedBitSet(numBits);
                Assert.IsTrue(b1.Equals(b2));
                Assert.Equals(b1.GetHashCode(), b2.GetHashCode());
                Assert.Equals(0, b1.Cardinality());
                if (numBits > 0)
                {
                    b1.Set(0, numBits);
                    Assert.Equals(numBits, b1.Cardinality());
                    b1.Flip(0, numBits);
                    Assert.Equals(0, b1.Cardinality());
                }
            }
        }

        private FixedBitSet MakeFixedBitSet(int[] a, int numBits)
        {
            var random = new Random();
            
            FixedBitSet bs;
            if (random.NextBool())
            {
                var bits2words = FixedBitSet.Bits2Words(numBits);
                var words = new long[bits2words + random.Next(100)];
                for (var i = bits2words; i < words.Length; i++)
                {
                    words[i] = random.NextLong();
                }
                bs = new FixedBitSet(words, numBits);

            }
            else
            {
                bs = new FixedBitSet(numBits);
            }
            foreach (var e in a)
            {
                bs.Set(e);
            }
            return bs;
        }

        private BitArray MakeBitSet(int[] a)
        {
            var bs = new BitArray();
            foreach (var e in a)
            {
                bs.Set(e);
            }
            return bs;
        }

        private void CheckPrevSetBitArray(int[] a, int numBits)
        {
            var obs = MakeFixedBitSet(a, numBits);
            var bs = MakeBitSet(a);
            doPrevSetBit(bs, obs);
        }

        [Test]
        public void TestPrevSetBit()
        {
            CheckPrevSetBitArray(new int[] { }, 0);
            CheckPrevSetBitArray(new int[] { 0 }, 1);
            CheckPrevSetBitArray(new int[] { 0, 2 }, 3);
        }

        private void CheckNextSetBitArray(int[] a, int numBits)
        {
            var obs = MakeFixedBitSet(a, numBits);
            var bs = MakeBitSet(a);
            doNextSetBit(bs, obs);
        }

        [Test]
        public void TestNextBitSet()
        {
            var random = new Random();

            var setBits = new int[0 + random.Next(1000)];
            for (var i = 0; i < setBits.Length; i++)
            {
                setBits[i] = random.Next(setBits.Length);
            }
            CheckNextSetBitArray(setBits, setBits.Length + random.Next(10));

            CheckNextSetBitArray(new int[0], setBits.Length + random.Next(10));
        }
    }
}
