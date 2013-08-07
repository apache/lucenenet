using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    public abstract class DataInput : ICloneable
    {
        public abstract byte ReadByte();

        public sbyte ReadSByte()
        {
            // helper method to account for java's byte being signed
            return (sbyte)ReadByte();
        }

        public abstract void ReadBytes(byte[] b, int offset, int len);

        public virtual void ReadBytes(byte[] b, int offset, int len, bool useBuffer)
        {
            // Default to ignoring useBuffer entirely
            ReadBytes(b, offset, len);
        }

        public void ReadBytes(sbyte[] b, int offset, int len)
        {
            // helper method to account for java's byte being signed
            ReadBytes(b, offset, len, false);
        }

        public void ReadBytes(sbyte[] b, int offset, int len, bool useBuffer)
        {
            // helper method to account for java's byte being signed
            ReadBytes((byte[])(Array)b, offset, len, useBuffer);
        }

        public virtual short ReadShort()
        {
            return (short)(((ReadByte() & 0xFF) << 8) | (ReadByte() & 0xFF));
        }

        public virtual int ReadInt()
        {
            return ((ReadByte() & 0xFF) << 24) | ((ReadByte() & 0xFF) << 16)
                | ((ReadByte() & 0xFF) << 8) | (ReadByte() & 0xFF);
        }

        public virtual int ReadVInt()
        {
            // .NET Port: Going back to original code instead of Java code below due to sbyte/byte diff
            byte b = ReadByte();
            int i = b & 0x7F;
            for (int shift = 7; (b & 0x80) != 0; shift += 7)
            {
                b = ReadByte();
                i |= (b & 0x7F) << shift;
            }
            return i;

            /* This is the original code of this method,
             * but a Hotspot bug (see LUCENE-2975) corrupts the for-loop if
             * ReadByte() is inlined. So the loop was unwinded!
            byte b = ReadByte();
            int i = b & 0x7F;
            for (int shift = 7; (b & 0x80) != 0; shift += 7) {
              b = ReadByte();
              i |= (b & 0x7F) << shift;
            }
            return i;
            */
            //byte b = ReadByte();
            //if (b >= 0) return b;
            //int i = b & 0x7F;
            //b = ReadByte();
            //i |= (b & 0x7F) << 7;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7F) << 14;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7F) << 21;
            //if (b >= 0) return i;
            //b = ReadByte();
            //// Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
            //i |= (b & 0x0F) << 28;
            //if ((b & 0xF0) == 0) return i;
            //throw new System.IO.IOException("Invalid vInt detected (too many bits)");
        }

        public virtual long ReadLong()
        {
            return (((long)ReadInt()) << 32) | (ReadInt() & 0xFFFFFFFFL);
        }

        public virtual long ReadVLong()
        {
            // .NET Port: going back to old style code
            byte b = ReadByte();
            long i = b & 0x7F;
            for (int shift = 7; (b & 0x80) != 0; shift += 7)
            {
                b = ReadByte();
                i |= (b & 0x7FL) << shift;
            }
            return i;

            /* This is the original code of this method,
             * but a Hotspot bug (see LUCENE-2975) corrupts the for-loop if
             * ReadByte() is inlined. So the loop was unwinded!
            byte b = ReadByte();
            long i = b & 0x7F;
            for (int shift = 7; (b & 0x80) != 0; shift += 7) {
              b = ReadByte();
              i |= (b & 0x7FL) << shift;
            }
            return i;
            */
            //byte b = ReadByte();
            //if (b >= 0) return b;
            //long i = b & 0x7FL;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 7;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 14;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 21;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 28;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 35;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 42;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 49;
            //if (b >= 0) return i;
            //b = ReadByte();
            //i |= (b & 0x7FL) << 56;
            //if (b >= 0) return i;
            //throw new System.IO.IOException("Invalid vLong detected (negative values disallowed)");
        }

        public virtual string ReadString()
        {
            int length = ReadVInt();
            byte[] bytes = new byte[length];
            ReadBytes(bytes, 0, length);

            return IOUtils.CHARSET_UTF_8.GetString(bytes);
        }

        public virtual object Clone()
        {
            return this.MemberwiseClone();
        }

        public virtual IDictionary<string, string> ReadStringStringMap()
        {
            IDictionary<String, String> map = new HashMap<String, String>();
            int count = ReadInt();
            for (int i = 0; i < count; i++)
            {
                String key = ReadString();
                String val = ReadString();
                map[key] = val;
            }

            return map;
        }

        public ISet<string> ReadStringSet()
        {
            ISet<String> set = new HashSet<String>();
            int count = ReadInt();
            for (int i = 0; i < count; i++)
            {
                set.Add(ReadString());
            }

            return set;
        }
    }
}
