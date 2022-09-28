using J2N.IO;
using J2N.Numerics;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ko.Dict
{
   public sealed class ConnectionCosts
    {
        public static readonly string FILENAME_SUFFIX = ".dat";
        public static readonly string HEADER = "ko_cc";
        public static readonly int VERSION = 1;
        private readonly ByteBuffer buffer;
        private readonly int forwardSize;

        private ConnectionCosts()
        {
            ByteBuffer buffer;
            using (Stream @is = BinaryDictionary.GetTypeResource(GetType(), FILENAME_SUFFIX))
            {
                DataInput @in = new InputStreamDataInput(@is);
                CodecUtil.CheckHeader(@in, HEADER, VERSION, VERSION);
                forwardSize = @in.ReadVInt32();
                int backwardSize = @in.ReadVInt32();
                int size = forwardSize * backwardSize;

                ByteBuffer tmpBuffer = ByteBuffer.Allocate(size);
                int accum = 0;
                for (int j = 0; j < backwardSize; j++)
                {
                    for (int i = 0; i < forwardSize; i++)
                    {
                        int raw = @in.ReadVInt32();
                        accum += raw.TripleShift(1) ^ -(raw & 1);
                        tmpBuffer.PutInt16((short)accum);
                    }
                }

                buffer = tmpBuffer.AsReadOnlyBuffer();
            }

            this.buffer = buffer;
        }

        public int Get(int forwardId, int backwardId)
        {
            int offset = (backwardId * forwardSize + forwardId) * 2;
            return buffer.GetInt16(offset);
        }

        public static ConnectionCosts Instance => SingletonHolder.INSTANCE;

        private class SingletonHolder
        {
            internal static readonly ConnectionCosts INSTANCE = LoadInstance();
            private static ConnectionCosts LoadInstance() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return new ConnectionCosts();
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create("Cannot load ConnectionCosts.", ioe);
                }
            }
        }
    }
}