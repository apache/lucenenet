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
        /*
        * Licensed to the Apache Software Foundation (ASF) under one or more
        * contributor license agreements.  See the NOTICE file distributed with
        * this work for additional information regarding copyright ownership.
        * The ASF licenses this file to You under the Apache License, Version 2.0
        * (the "License"); you may not use this file except in compliance with
        * the License.  You may obtain a copy of the License at
        *
        *     http://www.apache.org/licenses/LICENSE-2.0
        *
        * Unless required by applicable law or agreed to in writing, software
        * distributed under the License is distributed on an "AS IS" BASIS,
        * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
        * See the License for the specific language governing permissions and
        * limitations under the License.
        */

        /// <summary>
        /// n-gram connection cost data
        /// </summary>
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

                // copy the matrix into a direct byte buffer
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
            // map 2d matrix into a single dimension short array
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