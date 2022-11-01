using Lucene.Net.Store;
using Lucene.Net.Util.Fst;
using System;
using System.IO;
using Int64 = J2N.Numerics.Int64;

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
    /// Binary dictionary implementation for a known-word dictionary model:
    /// Words are encoded into an FST mapping to a list of wordIDs.
    /// </summary>
    public sealed class TokenInfoDictionary : BinaryDictionary
    {
        public static readonly string FST_FILENAME_SUFFIX = "$fst.dat";

        private readonly TokenInfoFST fst;

        private TokenInfoDictionary()
        {
            FST<Int64> fst = null;
            using (Stream @is = GetResource(FST_FILENAME_SUFFIX))
            {
                fst = new FST<Int64>(new InputStreamDataInput(@is), PositiveInt32Outputs.Singleton);
            }
            // TODO: some way to configure?
            this.fst = new TokenInfoFST(fst, true);
        }

        public TokenInfoFST FST => fst;

        public static TokenInfoDictionary Instance => SingletonHolder.INSTANCE;

        private static class SingletonHolder
        {
            internal static readonly TokenInfoDictionary INSTANCE = LoadInstance();
            private static TokenInfoDictionary LoadInstance() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return new TokenInfoDictionary();
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create("Cannot load TokenInfoDictionary.", ioe);
                }
            }
        }
    }
}
