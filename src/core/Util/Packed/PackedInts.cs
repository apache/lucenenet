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

        public readonly static String CODEC_NAME = "PackedInts";
        public readonly static int VERSION_START = 0; // PackedInts were long-aligned
        public readonly static int VERSION_BYTE_ALIGNED = 1;
        public readonly static int VERSION_CURRENT = VERSION_BYTE_ALIGNED;

        /**
         * Check the validity of a version number.
         */
        public static void checkVersion(int version)
        {
            if (version < VERSION_START)
            {
                throw new ArgumentException("Version is too old, should be at least " + VERSION_START + " (got " + version + ")");
            }
            else if (version > VERSION_CURRENT)
            {
                throw new ArgumentException("Version is too new, should be at most " + VERSION_CURRENT + " (got " + version + ")");
            }
        }

        /**
   * A format to write packed ints.
   *
   * @lucene.internal
   */
      public enum Format {
        /**
         * Compact format, all bits are written contiguously.
         */
        PACKED(0)   { 

      
          public override long byteCount(int packedIntsVersion, int valueCount, int bitsPerValue) {
            if (packedIntsVersion < VERSION_BYTE_ALIGNED) {
              return 8L *  (long) Math.Ceiling((double) valueCount * bitsPerValue / 64);
            } else {
              return (long) Math.Ceiling((double) valueCount * bitsPerValue / 8);
            }
          }

        },

        /**
         * A format that may insert padding bits to improve encoding and decoding
         * speed. Since this format doesn't support all possible bits per value, you
         * should never use it directly, but rather use
         * {@link PackedInts#fastestFormatAndBits(int, int, float)} to find the
         * format that best suits your needs.
         */
        PACKED_SINGLE_BLOCK(1) {

      
          public override int longCount(int packedIntsVersion, int valueCount, int bitsPerValue) {
            final int valuesPerBlock = 64 / bitsPerValue;
            return (int) Math.ceil((double) valueCount / valuesPerBlock);
          }

      
          public override bool isSupported(int bitsPerValue) {
            return Packed64SingleBlock.isSupported(bitsPerValue);
          }

      
          public override float overheadPerValue(int bitsPerValue) {
            assert isSupported(bitsPerValue);
            final int valuesPerBlock = 64 / bitsPerValue;
            final int overhead = 64 % bitsPerValue;
            return (float) overhead / valuesPerBlock;
          }

        };

        /**
         * Get a format according to its ID.
         */
        public static Format byId(int id) {
          for (Format format : Format.values()) {
            if (format.getId() == id) {
              return format;
            }
          }
          throw new ArgumentException("Unknown format id: " + id);
        }

        private Format(int id) {
          this.id = id;
        }

        public int id;

        /**
         * Returns the ID of the format.
         */
        public int getId() {
          return id;
        }

        /**
         * Computes how many byte blocks are needed to store <code>values</code>
         * values of size <code>bitsPerValue</code>.
         */
        public long byteCount(int packedIntsVersion, int valueCount, int bitsPerValue) {
          assert bitsPerValue >= 0 && bitsPerValue <= 64 : bitsPerValue;
          // assume long-aligned
          return 8L * longCount(packedIntsVersion, valueCount, bitsPerValue);
        }

        /**
         * Computes how many long blocks are needed to store <code>values</code>
         * values of size <code>bitsPerValue</code>.
         */
        public int longCount(int packedIntsVersion, int valueCount, int bitsPerValue) {
          assert bitsPerValue >= 0 && bitsPerValue <= 64 : bitsPerValue;
          final long byteCount = byteCount(packedIntsVersion, valueCount, bitsPerValue);
          assert byteCount < 8L * Integer.MAX_VALUE;
          if ((byteCount % 8) == 0) {
            return (int) (byteCount / 8);
          } else {
            return (int) (byteCount / 8 + 1);
          }
        }

        /**
         * Tests whether the provided number of bits per value is supported by the
         * format.
         */
        public boolean isSupported(int bitsPerValue) {
          return bitsPerValue >= 1 && bitsPerValue <= 64;
        }

        /**
         * Returns the overhead per value, in bits.
         */
        public float overheadPerValue(int bitsPerValue) {
          assert isSupported(bitsPerValue);
          return 0f;
        }

        /**
         * Returns the overhead ratio (<code>overhead per value / bits per value</code>).
         */
        public final float overheadRatio(int bitsPerValue) {
          assert isSupported(bitsPerValue);
          return overheadPerValue(bitsPerValue) / bitsPerValue;
        }
      }



        /**
   * Simple class that holds a format and a number of bits per value.
   */
  public static class FormatAndBits {
    public readonly Format format;
    public readonly int bitsPerValue;
    public FormatAndBits(Format format, int bitsPerValue) {
      this.format = format;
      this.bitsPerValue = bitsPerValue;
    }

    
    public override string ToString() {
      return "FormatAndBits(format=" + format + " bitsPerValue=" + bitsPerValue + ")";
    }
  }

    }
}
