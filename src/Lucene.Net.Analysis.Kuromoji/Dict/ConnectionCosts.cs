using J2N.Numerics;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ja.Dict
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
    public sealed class ConnectionCosts
    {
        public static readonly string FILENAME_SUFFIX = ".dat";
        public static readonly string HEADER = "kuromoji_cc";
        public static readonly int VERSION = 1;

        private readonly short[][] costs; // array is backward IDs first since get is called using the same backward ID consecutively. maybe doesn't matter.

        private ConnectionCosts()
        {
            short[][] costs = null;

            using (Stream @is = BinaryDictionary.GetTypeResource(GetType(), FILENAME_SUFFIX))
            {
                DataInput @in = new InputStreamDataInput(@is);
                CodecUtil.CheckHeader(@in, HEADER, VERSION, VERSION);
                int forwardSize = @in.ReadVInt32();
                int backwardSize = @in.ReadVInt32();
                costs = RectangularArrays.ReturnRectangularArray<short>(backwardSize, forwardSize);
                int accum = 0;
                for (int j = 0; j < costs.Length; j++)
                {
                    short[] a = costs[j];
                    for (int i = 0; i < a.Length; i++)
                    {
                        int raw = @in.ReadVInt32();
                        accum += raw.TripleShift(1) ^ -(raw & 1);
                        a[i] = (short)accum;
                    }
                }
            }

            this.costs = costs;
        }

        public int Get(int forwardId, int backwardId)
        {
            return costs[backwardId][forwardId];
        }

        public static ConnectionCosts Instance => SingletonHolder.INSTANCE;

        private static class SingletonHolder
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
