using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.MockSep
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
    /// Writes ints directly to the file (not in blocks) as
    /// vInt.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class MockSingleInt32IndexOutput : Int32IndexOutput // LUCENENET specific: Renamed from MockSingleIntIndexOutput
    {
        private readonly IndexOutput @out;
        internal const string CODEC = "SINGLE_INTS";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;

        public MockSingleInt32IndexOutput(Directory dir, string fileName, IOContext context)
        {
            @out = dir.CreateOutput(fileName, context);
            bool success = false;
            try
            {
                CodecUtil.WriteHeader(@out, CODEC, VERSION_CURRENT);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(@out);
                }
            }
        }

        public override void Write(int v)
        {
            @out.WriteVInt32(v);
        }

        public override Index GetIndex()
        {
            return new MockSingleInt32IndexOutputIndex(this);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                @out.Dispose();
            }
        }

        public override string ToString()
        {
            return "MockSingleIntIndexOutput fp=" + @out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
        }

        private class MockSingleInt32IndexOutputIndex : Index // LUCENENET specific: Renamed from MockSingleIntIndexOutputIndex
        {
            internal long fp;
            internal long lastFP;
            private readonly MockSingleInt32IndexOutput outerClass;

            public MockSingleInt32IndexOutputIndex(MockSingleInt32IndexOutput outerClass)
            {
                this.outerClass = outerClass;
            }

            public override void Mark()
            {
                fp = outerClass.@out.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            }

            public override void CopyFrom(Index other, bool copyLast)
            {
                fp = ((MockSingleInt32IndexOutputIndex)other).fp;
                if (copyLast)
                {
                    lastFP = ((MockSingleInt32IndexOutputIndex)other).fp;
                }
            }

            public override void Write(DataOutput indexOut, bool absolute)
            {
                if (absolute)
                {
                    indexOut.WriteVInt64(fp);
                }
                else
                {
                    indexOut.WriteVInt64(fp - lastFP);
                }
                lastFP = fp;
            }

            public override string ToString()
            {
                return fp.ToString();
            }
        }
    }
}
