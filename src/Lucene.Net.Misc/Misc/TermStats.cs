using Lucene.Net.Util;

namespace Lucene.Net.Misc
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
    /// Holder for a term along with its statistics
    /// (<see cref="DocFreq"/> and <see cref="TotalTermFreq"/>).
    /// </summary>
    public sealed class TermStats
    {
        internal readonly BytesRef termtext;
        public string Field { get; set; }
        public int DocFreq { get; set; }
        public long TotalTermFreq { get; set; }

        internal TermStats(string field, BytesRef termtext, int df, long tf)
        {
            this.termtext = BytesRef.DeepCopyOf(termtext);
            this.Field = field;
            this.DocFreq = df;
            this.TotalTermFreq = tf;
        }

        internal string GetTermText()
        {
            return termtext.Utf8ToString();
        }

        public override string ToString()
        {
            return ("TermStats: Term=" + termtext.Utf8ToString() + " DocFreq=" + DocFreq + " TotalTermFreq=" + TotalTermFreq);
        }
    }
}