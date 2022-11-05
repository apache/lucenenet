using Lucene.Net.Diagnostics;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using Fields = Lucene.Net.Index.Fields;
    using MergeState = Lucene.Net.Index.MergeState;

    // javadocs
    using Terms = Lucene.Net.Index.Terms;

    /// <summary>
    /// Abstract API that consumes terms, doc, freq, prox, offset and
    /// payloads postings.  Concrete implementations of this
    /// actually do "something" with the postings (write it into
    /// the index in a specific format).
    /// <para/>
    /// The lifecycle is:
    /// <list type="number">
    ///   <item><description>FieldsConsumer is created by
    ///       <see cref="PostingsFormat.FieldsConsumer(Index.SegmentWriteState)"/>.</description></item>
    ///   <item><description>For each field, <see cref="AddField(FieldInfo)"/> is called,
    ///       returning a <see cref="TermsConsumer"/> for the field.</description></item>
    ///   <item><description>After all fields are added, the consumer is <see cref="Dispose()"/>d.</description></item>
    /// </list>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class FieldsConsumer : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected FieldsConsumer()
        {
        }

        /// <summary>
        /// Add a new field. </summary>
        public abstract TermsConsumer AddField(FieldInfo field);

        /// <summary>
        /// Called when we are done adding everything. </summary>
        // LUCENENET specific - implementing proper dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Implementations must override and should dispose all resources used by this instance.
        /// </summary>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Called during merging to merge all <see cref="Fields"/> from
        /// sub-readers.  this must recurse to merge all postings
        /// (terms, docs, positions, etc.).  A 
        /// <see cref="PostingsFormat"/> can override this default
        /// implementation to do its own merging.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void Merge(MergeState mergeState, Fields fields)
        {
            foreach (string field in fields)
            {
                FieldInfo info = mergeState.FieldInfos.FieldInfo(field);
                if (Debugging.AssertsEnabled) Debugging.Assert(info != null,"FieldInfo for field is null: {0}", field);
                Terms terms = fields.GetTerms(field);
                if (terms != null)
                {
                    TermsConsumer termsConsumer = AddField(info);
                    termsConsumer.Merge(mergeState, info.IndexOptions, terms.GetEnumerator());
                }
            }
        }
    }
}