using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using System;
using System.Runtime.CompilerServices;
using Attribute = Lucene.Net.Util.Attribute;

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

    /// <summary>
    /// Represents a binary token. </summary>
    public sealed class BinaryToken
    {
        internal BytesRef Term { get; set; }
        internal int PosInc { get; set; }
        internal int PosLen { get; set; }
        internal int StartOffset { get; set; }
        internal int EndOffset { get; set; }

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

    /// <summary>
    /// An attribute extending <see cref="ITermToBytesRefAttribute"/>
    /// but exposing <see cref="BytesRef"/> property.
    /// </summary>
    public interface IBinaryTermAttribute : ITermToBytesRefAttribute
    {
        /// <summary>
        /// Set the current binary value. </summary>
        new BytesRef BytesRef { get; set; }
    }

    /// <summary>
    /// Implementation for <see cref="IBinaryTermAttribute"/>. </summary>
    public sealed class BinaryTermAttribute : Attribute, IBinaryTermAttribute
    {
        private readonly BytesRef bytes = new BytesRef();

        public void FillBytesRef()
        {
            // no-op: we already filled externally during owner's incrementToken
        }

        public BytesRef BytesRef
        {
            get => bytes;
            set => this.bytes.CopyBytes(value);
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

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IBinaryTermAttribute other)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IBinaryTermAttribute)}", nameof(target));
            other.BytesRef.CopyBytes(bytes);
        }

        public override object Clone()
        {
            throw UnsupportedOperationException.Create();
        }
    }

    /// <summary>
    /// <see cref="TokenStream"/> from a canned list of binary (<see cref="BytesRef"/>-based)
    /// tokens.
    /// </summary>
    public sealed class CannedBinaryTokenStream : TokenStream
    {
        // LUCENENET specific - de-nested BinaryToken

        private readonly BinaryToken[] tokens;
        private int upto = 0;
        private readonly IBinaryTermAttribute termAtt;
        private readonly IPositionIncrementAttribute posIncrAtt;
        private readonly IPositionLengthAttribute posLengthAtt;
        private readonly IOffsetAttribute offsetAtt;

        // LUCENENET specific - de-nested IBinaryTermAttribute

        // LUCENENET specific - de-nested BinaryTermAttribute

        public CannedBinaryTokenStream(params BinaryToken[] tokens)
            : base()
        {
            this.tokens = tokens;
            termAtt = AddAttribute<IBinaryTermAttribute>();
            posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            posLengthAtt = AddAttribute<IPositionLengthAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
        }

        public override bool IncrementToken()
        {
            if (upto < tokens.Length)
            {
                BinaryToken token = tokens[upto++];
                // TODO: can we just capture/restoreState so
                // we get all attrs...?
                ClearAttributes();
                termAtt.BytesRef = token.Term;
                posIncrAtt.PositionIncrement = token.PosInc;
                posLengthAtt.PositionLength = token.PosLen;
                offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}