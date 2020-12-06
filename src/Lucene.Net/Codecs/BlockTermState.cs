using Lucene.Net.Diagnostics;

namespace Lucene.Net.Codecs
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

    using DocsEnum = Lucene.Net.Index.DocsEnum; // javadocs
    using OrdTermState = Lucene.Net.Index.OrdTermState;
    using TermState = Lucene.Net.Index.TermState;

    /// <summary>
    /// Holds all state required for <see cref="PostingsReaderBase"/>
    /// to produce a <see cref="DocsEnum"/> without re-seeking the
    /// terms dict.
    /// </summary>
    public class BlockTermState : OrdTermState
    {
        /// <summary>
        /// How many docs have this term? </summary>
        public int DocFreq { get; set; }

        /// <summary>
        /// Total number of occurrences of this term. </summary>
        public long TotalTermFreq { get; set; }

        /// <summary>
        /// The term's ord in the current block. </summary>
        public int TermBlockOrd { get; set; }

        /// <summary>
        /// File pointer into the terms dict primary file (_X.tim) that holds this term. </summary>
        // TODO: update BTR to nuke this
        public long BlockFilePointer { get; set; }

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected internal BlockTermState()
        {
        }

        public override void CopyFrom(TermState other)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(other is BlockTermState, "can not copy from {0}", other.GetType().Name);
            BlockTermState other2 = (BlockTermState)other;
            base.CopyFrom(other);
            DocFreq = other2.DocFreq;
            TotalTermFreq = other2.TotalTermFreq;
            TermBlockOrd = other2.TermBlockOrd;
            BlockFilePointer = other2.BlockFilePointer;
        }

        public override string ToString()
        {
            return "docFreq=" + DocFreq + " totalTermFreq=" + TotalTermFreq + " termBlockOrd=" + TermBlockOrd + " blockFP=" + BlockFilePointer;
        }
    }
}