using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Store;

namespace Lucene.Net.Util.Packed
{
    public class PackedInts
    {
        /**
         * At most 700% memory overhead, always select a direct implementation.
         */
        public static readonly float FASTEST = 7f;

        /**
         * At most 50% memory overhead, always select a reasonably fast implementation.
         */
        public static readonly float FAST = 0.5f;

        /**
         * At most 20% memory overhead.
         */
        public static readonly float DEFAULT = 0.2f;

        /**
         * No memory overhead at all, but the returned implementation may be slow.
         */
        public static readonly float COMPACT = 0f;

        /**
         * Default amount of memory to use for bulk operations.
         */
        public static readonly int DEFAULT_BUFFER_SIZE = 1024; // 1K

        public static readonly String CODEC_NAME = "PackedInts";
        public static readonly int VERSION_START = 0; // PackedInts were long-aligned
        public static readonly int VERSION_BYTE_ALIGNED = 1;
        public static readonly int VERSION_CURRENT = VERSION_BYTE_ALIGNED;

        /**
         * Check the validity of a version number.
         */

        public static void checkVersion(int version)
        {
            if (version < VERSION_START)
            {
                throw new ArgumentException("Version is too old, should be at least " + VERSION_START + " (got " +
                                            version + ")");
            }
            else if (version > VERSION_CURRENT)
            {
                throw new ArgumentException("Version is too new, should be at most " + VERSION_CURRENT + " (got " +
                                            version + ")");
            }
        }

        /**
   * A format to write packed ints.
   *
   * @lucene.internal
   */

        public class Format
        {
            /**
         * Compact format, all bits are written contiguously.
         */

            public class PACKED
            {
                public long ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
                {
                    if (packedIntsVersion < VERSION_BYTE_ALIGNED)
                    {
                        return 8L*(long) Math.Ceiling((double) valueCount*bitsPerValue/64);
                    }
                    return (long) Math.Ceiling((double) valueCount*bitsPerValue/8);
                }
            }

            /**
         * A format that may insert padding bits to improve encoding and decoding
         * speed. Since this format doesn't support all possible bits per value, you
         * should never use it directly, but rather use
         * {@link PackedInts#fastestFormatAndBits(int, int, float)} to find the
         * format that best suits your needs.
         */

            public class PACKED_SINGLE_BLOCK
            {
                public int LongCount(int packedIntsVersion, int valueCount, int bitsPerValue)
                {
                    int valuesPerBlock = 64/bitsPerValue;
                    return (int) Math.Ceiling((double) valueCount/valuesPerBlock);
                }


                public bool IsSupported(int bitsPerValue)
                {
                    return Packed64SingleBlock.IsSupported(bitsPerValue);
                }


                public float overheadPerValue(int bitsPerValue)
                {
                    int valuesPerBlock = 64/bitsPerValue;
                    
                    int overhead = 64%bitsPerValue;
                    return (float) overhead/valuesPerBlock;
                }
            }

            /**
         * Get a format according to its ID.
         */

            public static Format ById(int id)
            {
                foreach (Format format in Format.values())
                {
                    if (format.GetId() == id)
                    {
                        return format;
                    }
                }
                throw new ArgumentException("Unknown format id: " + id);
            }

            private Format(int id)
            {
                this.id = id;
            }

            public int id;



            public int GetId()
            {
                return id;
            }



            public long ByteCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                // assume long-aligned
                return 8L*longCount(packedIntsVersion, valueCount, bitsPerValue);
            }



            public int longCount(int packedIntsVersion, int valueCount, int bitsPerValue)
            {
                
                long byteCount = ByteCount(packedIntsVersion, valueCount, bitsPerValue);
                
                if ((byteCount%8) == 0)
                {
                    return (int) (byteCount/8);
                }
                else
                {
                    return (int) (byteCount/8 + 1);
                }
            }



            public bool IsSupported(int bitsPerValue)
            {
                return bitsPerValue >= 1 && bitsPerValue <= 64;
            }



            public float OverheadPerValue(int bitsPerValue)
            {
                return 0f;
            }


            public float OverheadRatio(int bitsPerValue)
            {
                return OverheadPerValue(bitsPerValue)/bitsPerValue;
            }
        }




        public class FormatAndBits
        {
            public readonly Format Format;
            public readonly int BitsPerValue;

            public FormatAndBits(Format format, int bitsPerValue)
            {
                this.Format = format;
                this.BitsPerValue = bitsPerValue;
            }


            public override string ToString()
            {
                return "FormatAndBits(format=" + Format + " bitsPerValue=" + BitsPerValue + ")";
            }
        }
    }
}