using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Codecs;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Analysis.Ja.Util
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

    public sealed class ConnectionCostsWriter
    {
        private readonly short[][] costs; // array is backward IDs first since get is called using the same backward ID consecutively. maybe doesn't matter.
        private readonly int forwardSize;
        private readonly int backwardSize;
        /// <summary>
        /// Constructor for building. TODO: remove write access
        /// </summary>
        public ConnectionCostsWriter(int forwardSize, int backwardSize)
        {
            this.forwardSize = forwardSize;
            this.backwardSize = backwardSize;
            //this.costs = new short[backwardSize][forwardSize];
            this.costs = Support.RectangularArrays.ReturnRectangularArray<short>(backwardSize, forwardSize);
        }

        public void Add(int forwardId, int backwardId, int cost)
        {
            this.costs[backwardId][forwardId] = (short)cost;
        }

        public void Write(string baseDir)
        {
            //string filename = baseDir + System.IO.Path.DirectorySeparatorChar +
            //    typeof(ConnectionCosts).FullName.Replace('.', System.IO.Path.DirectorySeparatorChar) + ConnectionCosts.FILENAME_SUFFIX;

            // LUCENENET specific: we don't need to do a "classpath" output directory, since we
            // are changing the implementation to read files dynamically instead of making the
            // user recompile with the new files.
            string filename = System.IO.Path.Combine(baseDir, typeof(ConnectionCosts).Name + CharacterDefinition.FILENAME_SUFFIX);
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            using Stream os = new FileStream(filename, FileMode.Create, FileAccess.Write);
            DataOutput @out = new OutputStreamDataOutput(os);
            CodecUtil.WriteHeader(@out, ConnectionCosts.HEADER, ConnectionCosts.VERSION);
            @out.WriteVInt32(forwardSize);
            @out.WriteVInt32(backwardSize);
            int last = 0;
            if (Debugging.AssertsEnabled) Debugging.Assert(costs.Length == backwardSize);
            foreach (short[] a in costs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(a.Length == forwardSize);
                for (int i = 0; i < a.Length; i++)
                {
                    int delta = (int)a[i] - last;
                    @out.WriteVInt32((delta >> 31) ^ (delta << 1));
                    last = a[i];
                }
            }
        }
    }
}
