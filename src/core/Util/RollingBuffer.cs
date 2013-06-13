using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public abstract class RollingBuffer<T>
        where T : RollingBuffer<T>.Resettable
    {
        public interface Resettable
        {
            void Reset();
        }

        private T[] buffer = new T[8];

        // Next array index to write to:
        private int nextWrite;

        // Next position to write:
        private int nextPos;

        // How many valid Position are held in the
        // array:
        private int count;

        public RollingBuffer()
        {
            for (int idx = 0; idx < buffer.Length; idx++)
            {
                buffer[idx] = NewInstance();
            }
        }

        protected abstract T NewInstance();

        public void Reset()
        {
            nextWrite--;
            while (count > 0)
            {
                if (nextWrite == -1)
                {
                    nextWrite = buffer.Length - 1;
                }
                buffer[nextWrite--].Reset();
                count--;
            }
            nextWrite = 0;
            nextPos = 0;
            count = 0;
        }

        private bool InBounds(int pos)
        {
            return pos < nextPos && pos >= nextPos - count;
        }

        private int GetIndex(int pos)
        {
            int index = nextWrite - (nextPos - pos);
            if (index < 0)
            {
                index += buffer.Length;
            }
            return index;
        }

        public T get(int pos)
        {
            //System.out.println("RA.get pos=" + pos + " nextPos=" + nextPos + " nextWrite=" + nextWrite + " count=" + count);
            while (pos >= nextPos)
            {
                if (count == buffer.Length)
                {
                    T[] newBuffer = new T[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    //System.out.println("  grow length=" + newBuffer.length);
                    Array.Copy(buffer, nextWrite, newBuffer, 0, buffer.Length - nextWrite);
                    Array.Copy(buffer, 0, newBuffer, buffer.Length - nextWrite, nextWrite);
                    for (int i = buffer.Length; i < newBuffer.Length; i++)
                    {
                        newBuffer[i] = NewInstance();
                    }
                    nextWrite = buffer.Length;
                    buffer = newBuffer;
                }
                if (nextWrite == buffer.Length)
                {
                    nextWrite = 0;
                }
                // Should have already been reset:
                nextWrite++;
                nextPos++;
                count++;
            }
            //assert inBounds(pos);
            int index = GetIndex(pos);
            //System.out.println("  pos=" + pos + " nextPos=" + nextPos + " -> index=" + index);
            //assert buffer[index].pos == pos;
            return buffer[index];
        }

        public int GetMaxPos()
        {
            return nextPos - 1;
        }

        public void freeBefore(int pos)
        {
            int toFree = count - (nextPos - pos);
            //assert toFree >= 0;
            //assert toFree <= count: "toFree=" + toFree + " count=" + count;
            int index = nextWrite - count;
            if (index < 0)
            {
                index += buffer.Length;
            }
            for (int i = 0; i < toFree; i++)
            {
                if (index == buffer.Length)
                {
                    index = 0;
                }
                //System.out.println("  fb idx=" + index);
                buffer[index].Reset();
                index++;
            }
            count -= toFree;
        }
    }
}
