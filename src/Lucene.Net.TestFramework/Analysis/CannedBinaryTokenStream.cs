using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis
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

    using Lucene.Net.Util;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// TokenStream from a canned list of binary (BytesRef-based)
    /// tokens.
    /// </summary>
    public sealed class CannedBinaryTokenStream : TokenStream
    {
        /// <summary>
        /// Represents a binary token. </summary>
        public sealed class BinaryToken
        {
            internal BytesRef Term;
            internal int PosInc;
            internal int PosLen;
            internal int StartOffset;
            internal int EndOffset;

            public BinaryToken(BytesRef term)
            {
                this.Term = term;
                this.PosInc = 1;
                this.PosLen = 1;
            }

            public BinaryToken(BytesRef term, int posInc, int posLen)
            {
                this.Term = term;
                this.PosInc = posInc;
                this.PosLen = posLen;
            }
        }

        private readonly BinaryToken[] Tokens;
        private int Upto = 0;
        private readonly IBinaryTermAttribute TermAtt;// = AddAttribute<BinaryTermAttribute>();
        private readonly IPositionIncrementAttribute PosIncrAtt;// = AddAttribute<PositionIncrementAttribute>();
        private readonly IPositionLengthAttribute PosLengthAtt;// = addAttribute(typeof(PositionLengthAttribute));
        private readonly IOffsetAttribute OffsetAtt;// = addAttribute(typeof(OffsetAttribute));

        /// <summary>
        /// An attribute extending {@link
        ///  TermToBytesRefAttribute} but exposing {@link
        ///  #setBytesRef} method.
        /// </summary>
        public interface IBinaryTermAttribute : ITermToBytesRefAttribute
        {
            /// <summary>
            /// Set the current binary value. </summary>
            BytesRef BytesRef { set; }
        }

        /// <summary>
        /// Implementation for <seealso cref="IBinaryTermAttribute"/>. </summary>
        public sealed class BinaryTermAttribute : Attribute, IBinaryTermAttribute
        {
            internal readonly BytesRef Bytes = new BytesRef();

            public void FillBytesRef()
            {
                // no-op: we already filled externally during owner's incrementToken
            }

            public BytesRef BytesRef
            {
                get
                {
                    return Bytes;
                }
                set
                {
                    this.Bytes.CopyBytes(value);
                }
            }

            public override void Clear()
            {
            }

            public override bool Equals(object other)
            {
                return other == this;
            }

            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }

            public override void CopyTo(IAttribute target)
            {
                BinaryTermAttribute other = (BinaryTermAttribute)target;
                other.Bytes.CopyBytes(Bytes);
            }

            public override object Clone()
            {
                throw new System.NotSupportedException();
            }
        }

        public CannedBinaryTokenStream(params BinaryToken[] tokens)
            : base()
        {
            this.Tokens = tokens;
            TermAtt = AddAttribute<IBinaryTermAttribute>();
            PosIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            PosLengthAtt = AddAttribute<IPositionLengthAttribute>();
            OffsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public override bool IncrementToken()
        {
            if (Upto < Tokens.Length)
            {
                BinaryToken token = Tokens[Upto++];
                // TODO: can we just capture/restoreState so
                // we get all attrs...?
                ClearAttributes();
                TermAtt.BytesRef = token.Term;
                PosIncrAtt.PositionIncrement = token.PosInc;
                PosLengthAtt.PositionLength = token.PosLen;
                OffsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}