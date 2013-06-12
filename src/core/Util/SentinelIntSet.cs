using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public class SentinelIntSet
    {
        /** A power-of-2 over-sized array holding the integers in the set along with empty values. */
        public int[] keys;
        public int count;
        public readonly int emptyVal;
        /** the count at which a rehash should be done */
        public int rehashCount;

        public SentinelIntSet(int size, int emptyVal)
        {
            this.emptyVal = emptyVal;
            int tsize = Math.Max(BitUtil.NextHighestPowerOfTwo(size), 1);
            rehashCount = tsize - (tsize >> 2);
            if (size >= rehashCount)
            {  // should be able to hold "size" w/o re-hashing
                tsize <<= 1;
                rehashCount = tsize - (tsize >> 2);
            }
            keys = new int[tsize];
            if (emptyVal != 0)
                Clear();
        }

        public void Clear()
        {
            Arrays.Fill(keys, emptyVal);
            count = 0;
        }

        public int Hash(int key)
        {
            return key;
        }

        public int Size { get { return count; } }

        public int GetSlot(int key)
        {
            //assert key != emptyVal;
            int h = Hash(key);
            int s = h & (keys.Length - 1);
            if (keys[s] == key || keys[s] == emptyVal) return s;

            int increment = (h >> 7) | 1;
            do
            {
                s = (s + increment) & (keys.Length - 1);
            } while (keys[s] != key && keys[s] != emptyVal);
            return s;
        }

        public int Find(int key)
        {
            //assert key != emptyVal;
            int h = Hash(key);
            int s = h & (keys.Length - 1);
            if (keys[s] == key) return s;
            if (keys[s] == emptyVal) return -s - 1;

            int increment = (h >> 7) | 1;
            for (; ; )
            {
                s = (s + increment) & (keys.Length - 1);
                if (keys[s] == key) return s;
                if (keys[s] == emptyVal) return -s - 1;
            }
        }

        public bool Exists(int key)
        {
            return Find(key) >= 0;
        }

        public int Put(int key)
        {
            int s = Find(key);
            if (s < 0)
            {
                count++;
                if (count >= rehashCount)
                {
                    Rehash();
                    s = GetSlot(key);
                }
                else
                {
                    s = -s - 1;
                }
                keys[s] = key;
            }
            return s;
        }

        public void Rehash()
        {
            int newSize = keys.Length << 1;
            int[] oldKeys = keys;
            keys = new int[newSize];
            if (emptyVal != 0) Arrays.Fill(keys, emptyVal);

            foreach (int key in oldKeys)
            {
                if (key == emptyVal) continue;
                int newSlot = GetSlot(key);
                keys[newSlot] = key;
            }
            rehashCount = newSize - (newSize >> 2);
        }
    }
}
