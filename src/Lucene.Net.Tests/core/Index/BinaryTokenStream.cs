using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Index
{
    using Attribute = Lucene.Net.Util.Attribute;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using IAttribute = Lucene.Net.Util.IAttribute;

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

    using TokenStream = Lucene.Net.Analysis.TokenStream;

    // javadocs

    /// <summary>
    /// A binary tokenstream that lets you index a single
    /// binary token (BytesRef value).
    /// </summary>
    /// <seealso> cref= CannedBinaryTokenStream </seealso>
    public sealed class BinaryTokenStream : TokenStream
    {
        private readonly IByteTermAttribute BytesAtt;// = addAttribute(typeof(ByteTermAttribute));
        private readonly BytesRef Bytes;
        private bool Available = true;

        public BinaryTokenStream(BytesRef bytes)
        {
            this.Bytes = bytes;
            BytesAtt = AddAttribute<IByteTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (Available)
            {
                ClearAttributes();
                Available = false;
                BytesAtt.BytesRef = Bytes;
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            Available = true;
        }

        public interface IByteTermAttribute : ITermToBytesRefAttribute
        {
            BytesRef BytesRef { set; }
        }

        public class ByteTermAttribute : Attribute, IByteTermAttribute
        {
            internal BytesRef Bytes;

            public void FillBytesRef()
            {
                // no-op: the bytes was already filled by our owner's incrementToken
            }

            public BytesRef BytesRef
            {
                get
                {
                    return Bytes;
                }
                set
                {
                    this.Bytes = value;
                }
            }

            public override void Clear()
            {
            }

            public override void CopyTo(IAttribute target)
            {
                ByteTermAttribute other = (ByteTermAttribute)target;
                other.Bytes = Bytes;
            }
        }
    }
}