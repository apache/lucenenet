using Lucene.Net.Support;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
    /// the purpose of this charfilter is to send offsets out of bounds
    ///  if the analyzer doesn't use correctOffset or does incorrect offset math.
    /// </summary>
    public class MockCharFilter : CharFilter
    {
        internal readonly int Remainder;

        // for testing only
        public MockCharFilter(TextReader @in, int remainder)
            : base(@in)
        {
            // TODO: instead of fixed remainder... maybe a fixed
            // random seed?
            this.Remainder = remainder;
            if (remainder < 0 || remainder >= 10)
            {
                throw new System.ArgumentException("invalid remainder parameter (must be 0..10): " + remainder);
            }
        }

        // for testing only, uses a remainder of 0
        public MockCharFilter(TextReader @in)
            : this(@in, 0)
        {
        }

        internal int CurrentOffset = -1;
        internal int Delta = 0;
        internal int BufferedCh = -1;

        public override int Read()
        {
            // we have a buffered character, add an offset correction and return it
            if (BufferedCh >= 0)
            {
                int ch = BufferedCh;
                BufferedCh = -1;
                CurrentOffset++;

                AddOffCorrectMap(CurrentOffset, Delta - 1);
                Delta--;
                return ch;
            }

            // otherwise actually read one
            int c = m_input.Read();
            if (c < 0)
            {
                return c;
            }

            CurrentOffset++;
            if ((c % 10) != Remainder || char.IsHighSurrogate((char)c) || char.IsLowSurrogate((char)c))
            {
                return c;
            }

            // we will double this character, so buffer it.
            BufferedCh = c;
            return c;
        }

        public override int Read(char[] cbuf, int off, int len)
        {
            // Java returns -1, maintain compat.
            int numRead = base.Read(cbuf, off, len);
            return numRead == 0 ? -1 : numRead;
        }

        protected override int Correct(int currentOff)
        {
            KeyValuePair<int, int> lastEntry = CollectionsHelper.LowerEntry(Corrections, currentOff + 1);
            int ret = lastEntry.Equals(default(KeyValuePair<int, int>)) ? currentOff : currentOff + lastEntry.Value;
            Debug.Assert(ret >= 0, "currentOff=" + currentOff + ",diff=" + (ret - currentOff));
            return ret;
        }

        protected internal virtual void AddOffCorrectMap(int off, int cumulativeDiff)
        {
            Corrections[off] = cumulativeDiff;
        }

        internal SortedDictionary<int, int> Corrections = new SortedDictionary<int, int>();
    }
}