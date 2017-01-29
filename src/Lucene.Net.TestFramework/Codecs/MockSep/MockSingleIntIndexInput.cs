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
    /// Reads IndexInputs written with {@link
    /// MockSingleIntIndexOutput}.  NOTE: this class is just for
    /// demonstration purposes(it is a very slow way to read a
    /// block of ints).
    /// 
    /// @lucene.experimental
    /// </summary>
    public class MockSingleIntIndexInput : IntIndexInput
    {
        private readonly IndexInput @in;

        public MockSingleIntIndexInput(Directory dir, string fileName, IOContext context)
        {
            @in = dir.OpenInput(fileName, context);
            CodecUtil.CheckHeader(@in, MockSingleIntIndexOutput.CODEC,
                          MockSingleIntIndexOutput.VERSION_START,
                          MockSingleIntIndexOutput.VERSION_START);
        }

        public override IntIndexInputReader GetReader()
        {
            return new MockReader((IndexInput)@in.Clone());
        }

        public override void Dispose()
        {
            @in.Dispose();
        }

        /**
         * Just reads a vInt directly from the file.
         */
        public class MockReader : IntIndexInputReader
        {
            // clone:
            internal readonly IndexInput @in;

            public MockReader(IndexInput @in)
            {
                this.@in = @in;
            }

            /** Reads next single int */
            public override int Next()
            {
                //System.out.println("msii.next() fp=" + in.getFilePointer() + " vs " + in.length());
                return @in.ReadVInt();
            }
        }

        internal class MockSingleIntIndexInputIndex : IntIndexInputIndex
        {
            private long fp;

            public override void Read(DataInput indexIn, bool absolute)
            {
                if (absolute)
                {
                    fp = indexIn.ReadVLong();
                }
                else
                {
                    fp += indexIn.ReadVLong();
                }
            }

            public override void CopyFrom(IntIndexInputIndex other)
            {
                fp = ((MockSingleIntIndexInputIndex)other).fp;
            }

            public override void Seek(IntIndexInputReader other)
            {
                ((MockReader)other).@in.Seek(fp);
            }

            public override string ToString()
            {
                return fp.ToString();
            }


            public override IntIndexInputIndex Clone()
            {
                MockSingleIntIndexInputIndex other = new MockSingleIntIndexInputIndex();
                other.fp = fp;
                return other;
            }
        }
        public override IntIndexInputIndex GetIndex()
        {
            return new MockSingleIntIndexInputIndex();
        }
    }
}
