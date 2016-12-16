using System;

namespace Lucene.Net.Analysis
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using CharTermAttribute = Lucene.Net.Analysis.TokenAttributes.CharTermAttribute;

    /// <summary>
    /// Extension of <seealso cref="CharTermAttributeImpl"/> that encodes the term
    /// text as UTF-16 bytes instead of as UTF-8 bytes.
    /// </summary>
    public class MockUTF16TermAttributeImpl : CharTermAttribute
    {
        //internal static readonly Charset Charset = Charset.forName("UTF-16LE");
        internal static readonly System.Text.Encoding Charset = System.Text.Encoding.Unicode;

        public override void FillBytesRef()
        {
            BytesRef bytes = BytesRef;
            var utf16 = Charset.GetBytes(this.ToString());
            bytes.Bytes = utf16;
            bytes.Offset = 0;
            bytes.Length = utf16.Length;
        }
    }
}