/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lucene.Net.Codecs.MockSep
{
    /// <summary>
    /// Writes ints directly to the file (not in blocks) as
    /// vInt.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class MockSingleIntIndexOutput : Int32IndexOutput
    {
        private readonly IndexOutput @out;
        internal const string CODEC = "SINGLE_INTS";
        internal const int VERSION_START = 0;
        internal const int VERSION_CURRENT = VERSION_START;

        public MockSingleIntIndexOutput(Directory dir, string fileName, IOContext context)
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
            return new MockSingleIntIndexOutputIndex(this);
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
            return "MockSingleIntIndexOutput fp=" + @out.GetFilePointer();
        }

        private class MockSingleIntIndexOutputIndex : Index
        {
            internal long fp;
            internal long lastFP;
            private readonly MockSingleIntIndexOutput outerClass;

            public MockSingleIntIndexOutputIndex(MockSingleIntIndexOutput outerClass)
            {
                this.outerClass = outerClass;
            }

            public override void Mark()
            {
                fp = outerClass.@out.GetFilePointer();
            }

            public override void CopyFrom(Index other, bool copyLast)
            {
                fp = ((MockSingleIntIndexOutputIndex)other).fp;
                if (copyLast)
                {
                    lastFP = ((MockSingleIntIndexOutputIndex)other).fp;
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
