using Lucene.Net.Analysis.TokenAttributes;

namespace Lucene.Net.Analysis
{
    using System.IO;

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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Wraps a whitespace tokenizer with a filter that sets
    /// the first token, and odd tokens to posinc=1, and all others
    /// to 0, encoding the position as pos: XXX in the payload.
    ///
    /// </summary>
    public sealed class MockPayloadAnalyzer : Analyzer
    {
        protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
        {
            Tokenizer result = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
            return new TokenStreamComponents(result, new MockPayloadFilter(result, fieldName));
        }
    }

    ///
    ///
    ///
    internal sealed class MockPayloadFilter : TokenFilter
    {
        internal string FieldName;

        internal int Pos;

        internal int i;

        internal readonly IPositionIncrementAttribute PosIncrAttr;
        internal readonly IPayloadAttribute PayloadAttr;
        internal readonly ICharTermAttribute TermAttr;

        public MockPayloadFilter(TokenStream input, string fieldName)
            : base(input)
        {
            this.FieldName = fieldName;
            Pos = 0;
            i = 0;
            PosIncrAttr = input.AddAttribute<IPositionIncrementAttribute>();
            PayloadAttr = input.AddAttribute<IPayloadAttribute>();
            TermAttr = input.AddAttribute<ICharTermAttribute>();
        }

        public override bool IncrementToken()
        {
            if (input.IncrementToken())
            {
                PayloadAttr.Payload = new BytesRef(("pos: " + Pos)/*.getBytes(IOUtils.CHARSET_UTF_8)*/);
                int posIncr;
                if (Pos == 0 || i % 2 == 1)
                {
                    posIncr = 1;
                }
                else
                {
                    posIncr = 0;
                }
                PosIncrAttr.PositionIncrement = posIncr;
                Pos += posIncr;
                i++;
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void Reset()
        {
            base.Reset();
            i = 0;
            Pos = 0;
        }
    }
}