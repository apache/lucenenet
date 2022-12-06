using System;
using System.Collections.Generic;

namespace Lucene.Net.Index
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
    /// A struct like class that represents a hierarchical relationship between
    /// <see cref="IndexReader"/> instances.
    /// </summary>
    public abstract class IndexReaderContext
    {
        /// <summary>
        /// The reader context for this reader's immediate parent, or null if none </summary>
        public CompositeReaderContext Parent { get; private set; }

        /// <summary>
        /// <c>true</c> if this context struct represents the top level reader within the hierarchical context </summary>
        public bool IsTopLevel { get; private set; }

        /// <summary>
        /// the doc base for this reader in the parent, <c>0</c> if parent is <c>null</c> </summary>
        public int DocBaseInParent { get; private set; }

        /// <summary>
        /// the ord for this reader in the parent, <c>0</c> if parent is <c>null</c> </summary>
        public int OrdInParent { get; private set; }

        private protected IndexReaderContext(CompositeReaderContext parent, int ordInParent, int docBaseInParent) // LUCENENET: Changed from internal to private protected
        {
            if (!(this is CompositeReaderContext || this is AtomicReaderContext))
            {
                throw Error.Create("this class should never be extended by custom code!");
            }
            this.Parent = parent;
            this.DocBaseInParent = docBaseInParent;
            this.OrdInParent = ordInParent;
            this.IsTopLevel = parent is null;
        }

        /// <summary>
        /// Returns the <see cref="IndexReader"/>, this context represents. </summary>
        public abstract IndexReader Reader { get; }

        /// <summary>
        /// Returns the context's leaves if this context is a top-level context.
        /// For convenience, if this is an <see cref="AtomicReaderContext"/> this
        /// returns itself as the only leaf.
        /// <para/>Note: this is convenience method since leaves can always be obtained by
        /// walking the context tree using <see cref="Children"/>. </summary>
        /// <exception cref="InvalidOperationException"> if this is not a top-level context. </exception>
        /// <seealso cref="Children"/>
        public abstract IList<AtomicReaderContext> Leaves { get; }

        /// <summary>
        /// Returns the context's children iff this context is a composite context
        /// otherwise <c>null</c>.
        /// </summary>
        public abstract IList<IndexReaderContext> Children { get; }
    }
}