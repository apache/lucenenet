using Lucene.Net.Support.Threading;
using System;
using System.IO;
using System.Runtime.ExceptionServices;

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
    /// A <see cref="CompositeReader"/> which reads multiple indexes, appending
    /// their content. It can be used to create a view on several
    /// sub-readers (like <see cref="DirectoryReader"/>) and execute searches on it.
    ///
    /// <para/> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <para/><a name="thread-safety"></a><b>NOTE</b>: 
    /// <see cref="IndexReader"/> instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <see cref="IndexReader"/> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public class MultiReader : BaseCompositeReader<IndexReader>
    {
        private readonly bool closeSubReaders;

        /// <summary>
        /// <para>Construct a <see cref="MultiReader"/> aggregating the named set of (sub)readers.</para>
        /// <para>Note that all subreaders are closed if this Multireader is closed.</para> </summary>
        /// <param name="subReaders"> set of (sub)readers </param>
        public MultiReader(params IndexReader[] subReaders)
            : this(subReaders, true)
        {
        }

        /// <summary>
        /// Construct a <see cref="MultiReader"/> aggregating the named set of (sub)readers. </summary>
        /// <param name="subReaders"> set of (sub)readers; this array will be cloned. </param>
        /// <param name="closeSubReaders"> indicates whether the subreaders should be disposed
        /// when this <see cref="MultiReader"/> is disposed </param>
        public MultiReader(IndexReader[] subReaders, bool closeSubReaders)
            : base((IndexReader[])subReaders.Clone())
        {
            this.closeSubReaders = closeSubReaders;
            if (!closeSubReaders)
            {
                for (int i = 0; i < subReaders.Length; i++)
                {
                    subReaders[i].IncRef();
                }
            }
        }

        protected internal override void DoClose()
        {
            UninterruptableMonitor.Enter(this);
            try
            {
                Exception ioe = null; // LUCENENET: No need to cast to IOExcpetion
                foreach (IndexReader r in GetSequentialSubReaders())
                {
                    try
                    {
                        if (closeSubReaders)
                        {
                            r.Dispose();
                        }
                        else
                        {
                            r.DecRef();
                        }
                    }
                    catch (Exception e) when (e.IsIOException())
                    {
                        if (ioe is null)
                        {
                            ioe = e;
                        }
                    }
                }
                // throw the first exception
                if (ioe != null)
                {
                    ExceptionDispatchInfo.Capture(ioe).Throw(); // LUCENENET: Rethrow to preserve stack details from the original throw
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(this);
            }
        }
    }
}