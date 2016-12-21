using System;
using System.Diagnostics;

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
    /// <p>
    /// The lifecycle is:
    /// <ol>
    ///   <li>FieldsConsumer is created by
    ///       <seealso cref="PostingsFormat#fieldsConsumer(SegmentWriteState)"/>.
    ///   <li>For each field, <seealso cref="#addField(FieldInfo)"/> is called,
    ///       returning a <seealso cref="TermsConsumer"/> for the field.
    ///   <li>After all fields are added, the consumer is <seealso cref="#close"/>d.
    /// </ol>
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class FieldsConsumer : IDisposable
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        ///  constructors, typically implicit.)
        /// </summary>
        protected internal FieldsConsumer()
        {
        }

        /// <summary>
        /// Add a new field </summary>
        public abstract TermsConsumer AddField(FieldInfo field);

        /// <summary>
        /// Called when we are done adding everything. </summary>
        public abstract void Dispose();

        /// <summary>
        /// Called during merging to merge all <seealso cref="Fields"/> from
        ///  sub-readers.  this must recurse to merge all postings
        ///  (terms, docs, positions, etc.).  A {@link
        ///  PostingsFormat} can override this default
        ///  implementation to do its own merging.
        /// </summary>
        public virtual void Merge(MergeState mergeState, Fields fields)
        {
            foreach (string field in fields)
            {
                FieldInfo info = mergeState.FieldInfos.FieldInfo(field);
                Debug.Assert(info != null, "FieldInfo for field is null: " + field);
                Terms terms = fields.Terms(field);
                if (terms != null)
                {
                    TermsConsumer termsConsumer = AddField(info);
                    termsConsumer.Merge(mergeState, info.IndexOptions, terms.Iterator(null));
                }
            }
        }
    }
}