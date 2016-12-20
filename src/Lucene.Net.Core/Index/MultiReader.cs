using System.IO;

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
    /// A <seealso cref="CompositeReader"/> which reads multiple indexes, appending
    ///  their content. It can be used to create a view on several
    ///  sub-readers (like <seealso cref="DirectoryReader"/>) and execute searches on it.
    ///
    /// <p> For efficiency, in this API documents are often referred to via
    /// <i>document numbers</i>, non-negative integers which each name a unique
    /// document in the index.  These document numbers are ephemeral -- they may change
    /// as documents are added to and deleted from an index.  Clients should thus not
    /// rely on a given document having the same number between sessions.
    ///
    /// <p><a name="thread-safety"></a><p><b>NOTE</b>: {@link
    /// IndexReader} instances are completely thread
    /// safe, meaning multiple threads can call any of its methods,
    /// concurrently.  If your application requires external
    /// synchronization, you should <b>not</b> synchronize on the
    /// <code>IndexReader</code> instance; use your own
    /// (non-Lucene) objects instead.
    /// </summary>
    public class MultiReader : BaseCompositeReader<IndexReader>
    {
        private readonly bool closeSubReaders;

        /// <summary>
        /// <p>Construct a MultiReader aggregating the named set of (sub)readers.
        /// <p>Note that all subreaders are closed if this Multireader is closed.</p> </summary>
        /// <param name="subReaders"> set of (sub)readers </param>
        public MultiReader(params IndexReader[] subReaders)
            : this(subReaders, true)
        {
        }

        /// <summary>
        /// <p>Construct a MultiReader aggregating the named set of (sub)readers. </summary>
        /// <param name="subReaders"> set of (sub)readers; this array will be cloned. </param>
        /// <param name="closeSubReaders"> indicates whether the subreaders should be closed
        /// when this MultiReader is closed </param>
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
            lock (this)
            {
                IOException ioe = null;
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
                    catch (IOException e)
                    {
                        if (ioe == null)
                        {
                            ioe = e;
                        }
                    }
                }
                // throw the first exception
                if (ioe != null)
                {
                    throw ioe;
                }
            }
        }
    }
}