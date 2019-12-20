using J2N.Threading.Atomic;
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using Directory = Lucene.Net.Store.Directory;
    using Query = Lucene.Net.Search.Query;

    /// <summary>
    /// Class that tracks changes to a delegated
    /// <see cref="Index.IndexWriter"/>, used by 
    /// <see cref="Search.ControlledRealTimeReopenThread{T}"/> to ensure specific
    /// changes are visible.   Create this class (passing your
    /// <see cref="Index.IndexWriter"/>), and then pass this class to
    /// <see cref="Search.ControlledRealTimeReopenThread{T}"/>.
    /// Be sure to make all changes via the
    /// <see cref="TrackingIndexWriter"/>, otherwise
    /// <see cref="Search.ControlledRealTimeReopenThread{T}"/> won't know about the changes.
    /// <para/>
    /// @lucene.experimental
    /// </summary>

    public class TrackingIndexWriter
    {
        private readonly IndexWriter writer;
        private readonly AtomicInt64 indexingGen = new AtomicInt64(1);

        /// <summary>
        /// Create a <see cref="TrackingIndexWriter"/> wrapping the
        ///  provided <see cref="Index.IndexWriter"/>.
        /// </summary>
        public TrackingIndexWriter(IndexWriter writer)
        {
            this.writer = writer;
        }

        /// <summary>
        /// Calls 
        /// <see cref="IndexWriter.UpdateDocument(Term, IEnumerable{IIndexableField}, Analyzer)"/>
        /// and returns the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocument(Term t, IEnumerable<IIndexableField> d, Analyzer a)
        {
            writer.UpdateDocument(t, d, a);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls 
        /// <see cref="IndexWriter.UpdateDocument(Term, IEnumerable{IIndexableField})"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocument(Term t, IEnumerable<IIndexableField> d)
        {
            writer.UpdateDocument(t, d);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls 
        /// <see cref="IndexWriter.UpdateDocuments(Term, IEnumerable{IEnumerable{IIndexableField}}, Analyzer)"/>
        /// and returns the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocuments(Term t, IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer a)
        {
            writer.UpdateDocuments(t, docs, a);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls
        /// <see cref="IndexWriter.UpdateDocuments(Term, IEnumerable{IEnumerable{IIndexableField}})"/> and returns
        /// the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocuments(Term t, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            writer.UpdateDocuments(t, docs);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.DeleteDocuments(Term)"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(Term t)
        {
            writer.DeleteDocuments(t);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.DeleteDocuments(Term[])"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(params Term[] terms)
        {
            writer.DeleteDocuments(terms);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.DeleteDocuments(Query)"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(Query q)
        {
            writer.DeleteDocuments(q);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.DeleteDocuments(Query[])"/>
        /// and returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(params Query[] queries)
        {
            writer.DeleteDocuments(queries);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.DeleteAll()"/> and returns the
        /// generation that reflects this change.
        /// </summary>
        public virtual long DeleteAll()
        {
            writer.DeleteAll();
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls 
        /// <see cref="IndexWriter.AddDocument(IEnumerable{IIndexableField}, Analyzer)"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocument(IEnumerable<IIndexableField> d, Analyzer a)
        {
            writer.AddDocument(d, a);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls 
        /// <see cref="IndexWriter.AddDocuments(IEnumerable{IEnumerable{IIndexableField}}, Analyzer)"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer a)
        {
            writer.AddDocuments(docs, a);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.AddDocument(IEnumerable{IIndexableField})"/>
        /// and returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocument(IEnumerable<IIndexableField> d)
        {
            writer.AddDocument(d);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.AddDocuments(IEnumerable{IEnumerable{IIndexableField}})"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            writer.AddDocuments(docs);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.AddIndexes(Directory[])"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long AddIndexes(params Directory[] dirs)
        {
            writer.AddIndexes(dirs);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Calls <see cref="IndexWriter.AddIndexes(IndexReader[])"/>
        /// and returns the generation that reflects this change.
        /// </summary>
        public virtual long AddIndexes(params IndexReader[] readers)
        {
            writer.AddIndexes(readers);
            // Return gen as of when indexing finished:
            return indexingGen;
        }

        /// <summary>
        /// Return the current generation being indexed. </summary>
        public virtual long Generation => indexingGen;

        /// <summary>
        /// Return the wrapped <see cref="Index.IndexWriter"/>. </summary>
        public virtual IndexWriter IndexWriter => writer;

        /// <summary>
        /// Return and increment current gen.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public virtual long GetAndIncrementGeneration()
        {
            return indexingGen.GetAndIncrement();
        }

        /// <summary>
        /// Cals
        /// <see cref="IndexWriter.TryDeleteDocument(IndexReader, int)"/> and
        /// returns the generation that reflects this change.
        /// </summary>
        public virtual long TryDeleteDocument(IndexReader reader, int docID)
        {
            if (writer.TryDeleteDocument(reader, docID))
            {
                return indexingGen;
            }
            else
            {
                return -1;
            }
        }
    }
}