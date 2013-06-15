using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Packed
{
    public class GrowableWriter : PackedInts.IMutable
    {
        private long currentMaxValue;
        private PackedInts.IMutable current;
        private readonly float acceptableOverheadRatio;

        public GrowableWriter(int startBitsPerValue, int valueCount, float acceptableOverheadRatio)
        {
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            current = PackedInts.GetMutable(valueCount, startBitsPerValue, this.acceptableOverheadRatio);
            currentMaxValue = PackedInts.MaxValue(current.GetBitsPerValue());
        }

        public virtual long Get(int index)
        {
            return current.Get(index);
        }

        public virtual int Size()
        {
            return current.Size();
        }

        public virtual int GetBitsPerValue()
        {
            return current.GetBitsPerValue();
        }

        public virtual PackedInts.IMutable Mutable
        {
            get { return current; }
        }

        public virtual object GetArray()
        {
            return current.GetArray();
        }

        public virtual bool HasArray()
        {
            return current.HasArray();
        }

        private void EnsureCapacity(long value)
        {
            //assert value >= 0;
            if (value <= currentMaxValue)
            {
                return;
            }
            int bitsRequired = PackedInts.BitsRequired(value);
            int valueCount = Size();
            PackedInts.IMutable next = PackedInts.GetMutable(valueCount, bitsRequired, acceptableOverheadRatio);
            PackedInts.Copy(current, 0, next, 0, valueCount, PackedInts.DEFAULT_BUFFER_SIZE);
            current = next;
            currentMaxValue = PackedInts.MaxValue(current.GetBitsPerValue());
        }

        public virtual void Set(int index, long value)
        {
            EnsureCapacity(value);
            current.Set(index, value);
        }

        public virtual void Clear()
        {
            current.Clear();
        }

        public virtual GrowableWriter Resize(int newSize)
        {
            GrowableWriter next = new GrowableWriter(GetBitsPerValue(), newSize, acceptableOverheadRatio);
            int limit = Math.Min(Size(), newSize);
            PackedInts.Copy(current, 0, next, 0, limit, PackedInts.DEFAULT_BUFFER_SIZE);
            return next;
        }
        
        public virtual int Get(int index, long[] arr, int off, int len)
        {
            return current.Get(index, arr, off, len);
        }

        public virtual int Set(int index, long[] arr, int off, int len)
        {
            long max = 0;
            for (int i = off, end = off + len; i < end; ++i)
            {
                max |= arr[i];
            }
            EnsureCapacity(max);
            return current.Set(index, arr, off, len);
        }

        public virtual void Fill(int fromIndex, int toIndex, long val)
        {
            EnsureCapacity(val);
            current.Fill(fromIndex, toIndex, val);
        }

        public virtual long RamBytesUsed()
        {
            return current.RamBytesUsed();
        }

        public virtual void Save(DataOutput output)
        {
            current.Save(output);
        }
    }
}
