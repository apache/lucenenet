// Lucene version compatibility level 4.8.1
using System.IO;

namespace Lucene.Net.Analysis.Fa
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
    /// <see cref="CharFilter"/> that replaces instances of Zero-width non-joiner with an
    /// ordinary space.
    /// </summary>
    public class PersianCharFilter : CharFilter
    {
        public PersianCharFilter(TextReader @in)
              : base(@in)
        {
        }

        public override int Read(char[] cbuf, int off, int len)
        {
            int charsRead = m_input.Read(cbuf, off, len);
            if (charsRead > 0)
            {
                int end = off + charsRead;
                while (off < end)
                {
                    if (cbuf[off] == '\u200C')
                    {
                        cbuf[off] = ' ';
                    }
                    off++;
                }
            }
            return charsRead;
        }

        // optimized impl: some other charfilters consume with read()
        public override int Read()
        {
            int ch = m_input.Read();
            if (ch == '\u200C')
            {
                return ' ';
            }
            else
            {
                return ch;
            }
        }
        protected override int Correct(int currentOff)
        {
            return currentOff; // we don't change the length of the string
        }
    }
}