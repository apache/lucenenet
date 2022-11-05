using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

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

    // javadocs

    /// <summary>
    /// Instances of this reader type can only
    /// be used to get stored fields from the underlying <see cref="AtomicReader"/>s,
    /// but it is not possible to directly retrieve postings. To do that, get
    /// the <see cref="AtomicReaderContext"/> for all sub-readers via <see cref="AtomicReaderContext.Leaves"/>.
    /// Alternatively, you can mimic an <see cref="AtomicReader"/> (with a serious slowdown),
    /// by wrapping composite readers with <see cref="SlowCompositeReaderWrapper"/>.
    ///
    /// <para/><see cref="IndexReader"/> instances for indexes on disk are usually constructed
    /// with a call to one of the static <c>DirectoryReader.Open()</c> methods,
    /// e.g. <see cref="DirectoryReader.Open(Store.Directory)"/>. <see cref="DirectoryReader"/> implements
    /// the <see cref="CompositeReader"/> interface, it is not possible to directly get postings.
    /// <para/> Concrete subclasses of <see cref="IndexReader"/> are usually constructed with a call to
    /// one of the static <c>Open()</c> methods, e.g. <see cref="DirectoryReader.Open(Store.Directory)"/>.
    ///
    /// <para/> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <para/>
    /// <b>NOTE</b>: 
    /// <see cref="IndexReader"/> instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <see cref="IndexReader"/> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public abstract class CompositeReader : IndexReader
    {
        private volatile CompositeReaderContext readerContext = null; // lazy init

        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected CompositeReader()
            : base()
        {
        }

        public override string ToString()
        {
            StringBuilder buffer = new StringBuilder();
            // walk up through class hierarchy to get a non-empty simple name (anonymous classes have no name):
            for (Type clazz = this.GetType(); clazz != null; clazz = clazz.BaseType)
            {
                if (clazz.Name != null)
                {
                    buffer.Append(clazz.Name);
                    break;
                }
            }
            buffer.Append('(');
            var subReaders = GetSequentialSubReaders();
            if (Debugging.AssertsEnabled) Debugging.Assert(subReaders != null);
            if (subReaders.Count > 0)
            {
                buffer.Append(subReaders[0]);
                for (int i = 1, c = subReaders.Count; i < c; ++i)
                {
                    buffer.Append(' ').Append(subReaders[i]);
                }
            }
            buffer.Append(')');
            return buffer.ToString();
        }

        /// <summary>
        /// Expert: returns the sequential sub readers that this
        /// reader is logically composed of. This method may not
        /// return <c>null</c>.
        ///
        /// <para/><b>NOTE:</b> In contrast to previous Lucene versions this method
        /// is no longer public, code that wants to get all <see cref="AtomicReader"/>s
        /// this composite is composed of should use <see cref="IndexReader.Leaves"/>. </summary>
        /// <seealso cref="IndexReader.Leaves"/>
        protected internal abstract IList<IndexReader> GetSequentialSubReaders();

        public override sealed IndexReaderContext Context
        {
            get
            {
                EnsureOpen();
                // lazy init without thread safety for perf reasons: Building the readerContext twice does not hurt!
                if (readerContext is null)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(GetSequentialSubReaders() != null);
                    readerContext = CompositeReaderContext.Create(this);
                }
                return readerContext;
            }
        }
    }
}