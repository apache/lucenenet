using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs
{
    public static class CodecUtil
    {
        public const int CODEC_MAGIC = 0x3fd76c17;

        public static void WriteHeader(DataOutput output, string codec, int version)
        {
            BytesRef bytes = new BytesRef(codec);
            if (bytes.length != codec.Length || bytes.length >= 128)
            {
                throw new ArgumentException("codec must be simple ASCII, less than 128 characters in length [got " + codec + "]");
            }
            output.WriteInt(CODEC_MAGIC);
            output.WriteString(codec);
            output.WriteInt(version);
        }

        public static int HeaderLength(string codec)
        {
            return 9 + codec.Length;
        }

        public static int CheckHeader(DataInput input, string codec, int minVersion, int maxVersion)
        {
            // Safety to guard against reading a bogus string:
            int actualHeader = input.ReadInt();
            if (actualHeader != CODEC_MAGIC)
            {
                throw new CorruptIndexException("codec header mismatch: actual header=" + actualHeader + " vs expected header=" + CODEC_MAGIC + " (resource: " + input + ")");
            }
            return CheckHeaderNoMagic(input, codec, minVersion, maxVersion);
        }

        public static int CheckHeaderNoMagic(DataInput input, String codec, int minVersion, int maxVersion)
        {
            String actualCodec = input.ReadString();
            if (!actualCodec.Equals(codec))
            {
                throw new CorruptIndexException("codec mismatch: actual codec=" + actualCodec + " vs expected codec=" + codec + " (resource: " + input + ")");
            }

            int actualVersion = input.ReadInt();
            if (actualVersion < minVersion)
            {
                throw new IndexFormatTooOldException(input, actualVersion, minVersion, maxVersion);
            }
            if (actualVersion > maxVersion)
            {
                throw new IndexFormatTooNewException(input, actualVersion, minVersion, maxVersion);
            }

            return actualVersion;
        }
    }
}
