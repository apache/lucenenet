// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using System.Text;

namespace Lucene.Net.Analysis.Payloads
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
    ///  Does nothing other than convert the char array to a byte array using the specified encoding.
    /// </summary>
    public class IdentityEncoder : AbstractEncoder, IPayloadEncoder
    {
        protected internal Encoding m_charset = Encoding.UTF8;

        public IdentityEncoder()
        {
        }

        public IdentityEncoder(Encoding charset)
        {
            this.m_charset = charset;
        }

        public override BytesRef Encode(char[] buffer, int offset, int length)
        {
            return new BytesRef(m_charset.GetBytes(buffer, offset, length));
        }
    }
}