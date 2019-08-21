using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;

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
    /// Reads IndexInputs written with 
    /// <see cref="MockSingleInt32IndexOutput"/>.  NOTE: this class is just for
    /// demonstration purposes(it is a very slow way to read a
    /// block of ints).
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class MockSingleInt32IndexInput : Int32IndexInput // LUCENENET specific: Renamed from MockSingleIntIndexInput
    {
        private readonly IndexInput @in;

        public MockSingleInt32IndexInput(Directory dir, string fileName, IOContext context)
        {
            @in = dir.OpenInput(fileName, context);
            CodecUtil.CheckHeader(@in, MockSingleInt32IndexOutput.CODEC,
                          MockSingleInt32IndexOutput.VERSION_START,
                          MockSingleInt32IndexOutput.VERSION_START);
        }

        public override Reader GetReader()
        {
            return new MockReader((IndexInput)@in.Clone());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                @in.Dispose();
            }
        }

        /// <summary>
        /// Just reads a vInt directly from the file.
        /// </summary>
        public class MockReader : Reader
        {
            // clone:
            internal readonly IndexInput @in;

            public MockReader(IndexInput @in)
            {
                this.@in = @in;
            }

            /// <summary>
            /// Reads next single int.
            /// </summary>
            public override int Next()
            {
                //System.out.println("msii.next() fp=" + in.getFilePointer() + " vs " + in.length());
                return @in.ReadVInt32();
            }
        }

        internal class MockSingleInt32IndexInputIndex : Index // LUCENENET specific: Renamed from MockSingleIntIndexInputIndex
        {
            private long fp;

            public override void Read(DataInput indexIn, bool absolute)
            {
                if (absolute)
                {
                    fp = indexIn.ReadVInt64();
                }
                else
                {
                    fp += indexIn.ReadVInt64();
                }
            }

            public override void CopyFrom(Index other)
            {
                fp = ((MockSingleInt32IndexInputIndex)other).fp;
            }

            public override void Seek(Reader other)
            {
                ((MockReader)other).@in.Seek(fp);
            }

            public override string ToString()
            {
                return fp.ToString();
            }


            public override object Clone()
            {
                MockSingleInt32IndexInputIndex other = new MockSingleInt32IndexInputIndex();
                other.fp = fp;
                return other;
            }
        }
        public override Index GetIndex()
        {
            return new MockSingleInt32IndexInputIndex();
        }
    }
}
