using System;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Codecs.Lucene3x
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
    /// A <see cref="TermInfo"/> is the record of information stored for a
    /// term. </summary>
    [Obsolete("(4.0) this class is no longer used in flexible indexing.")]
    internal class TermInfo
    {
        /// <summary>
        /// The number of documents which contain the term. </summary>
        public int DocFreq { get; set; }

        public long FreqPointer { get; set; }
        public long ProxPointer { get; set; }
        public int SkipOffset { get; set; }

        public TermInfo()
        {
            DocFreq = 0;
            FreqPointer = 0;
            ProxPointer = 0;
        }

        public TermInfo(int df, long fp, long pp)
        {
            DocFreq = df;
            FreqPointer = fp;
            ProxPointer = pp;
        }

        public TermInfo(TermInfo ti)
        {
            DocFreq = ti.DocFreq;
            FreqPointer = ti.FreqPointer;
            ProxPointer = ti.ProxPointer;
            SkipOffset = ti.SkipOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int docFreq, long freqPointer, long proxPointer, int skipOffset)
        {
            this.DocFreq = docFreq;
            this.FreqPointer = freqPointer;
            this.ProxPointer = proxPointer;
            this.SkipOffset = skipOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(TermInfo ti)
        {
            DocFreq = ti.DocFreq;
            FreqPointer = ti.FreqPointer;
            ProxPointer = ti.ProxPointer;
            SkipOffset = ti.SkipOffset;
        }
    }
}