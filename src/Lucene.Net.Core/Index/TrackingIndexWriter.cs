namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using System.Collections.Generic;

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

    // javadocs
    using Query = Lucene.Net.Search.Query;

    /// <summary>
    /// Class that tracks changes to a delegated
    ///  IndexWriter, used by {@link
    ///  ControlledRealTimeReopenThread} to ensure specific
    ///  changes are visible.   Create this class (passing your
    ///  IndexWriter), and then pass this class to {@link
    ///  ControlledRealTimeReopenThread}.
    ///  Be sure to make all changes via the
    ///  TrackingIndexWriter, otherwise {@link
    ///  ControlledRealTimeReopenThread} won't know about the changes.
    ///
    /// @lucene.experimental
    /// </summary>

    public class TrackingIndexWriter
    {
        private readonly IndexWriter writer;
        private readonly AtomicLong indexingGen = new AtomicLong(1);

        /// <summary>
        /// Create a {@code TrackingIndexWriter} wrapping the
        ///  provided <seealso cref="IndexWriter"/>.
        /// </summary>
        public TrackingIndexWriter(IndexWriter writer)
        {
            this.writer = writer;
        }

        /// <summary>
        /// Calls {@link
        ///  IndexWriter#updateDocument(Term,Iterable,Analyzer)}
        ///  and returns the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocument(Term t, IEnumerable<IIndexableField> d, Analyzer a)
        {
            writer.UpdateDocument(t, d, a);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls {@link
        ///  IndexWriter#updateDocument(Term,Iterable)} and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocument(Term t, IEnumerable<IIndexableField> d)
        {
            writer.UpdateDocument(t, d);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls {@link
        ///  IndexWriter#updateDocuments(Term,Iterable,Analyzer)}
        ///  and returns the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocuments(Term t, IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer a)
        {
            writer.UpdateDocuments(t, docs, a);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls {@link
        ///  IndexWriter#updateDocuments(Term,Iterable)} and returns
        ///  the generation that reflects this change.
        /// </summary>
        public virtual long UpdateDocuments(Term t, IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            writer.UpdateDocuments(t, docs);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#deleteDocuments(Term)"/> and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(Term t)
        {
            writer.DeleteDocuments(t);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#deleteDocuments(Term...)"/> and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(params Term[] terms)
        {
            writer.DeleteDocuments(terms);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#deleteDocuments(Query)"/> and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(Query q)
        {
            writer.DeleteDocuments(q);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#deleteDocuments(Query...)"/>
        ///  and returns the generation that reflects this change.
        /// </summary>
        public virtual long DeleteDocuments(params Query[] queries)
        {
            writer.DeleteDocuments(queries);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#deleteAll"/> and returns the
        ///  generation that reflects this change.
        /// </summary>
        public virtual long DeleteAll()
        {
            writer.DeleteAll();
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls {@link
        ///  IndexWriter#addDocument(Iterable,Analyzer)} and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocument(IEnumerable<IIndexableField> d, Analyzer a)
        {
            writer.AddDocument(d, a);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls {@link
        ///  IndexWriter#addDocuments(Iterable,Analyzer)} and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs, Analyzer a)
        {
            writer.AddDocuments(docs, a);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#addDocument(Iterable)"/>
        ///  and returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocument(IEnumerable<IIndexableField> d)
        {
            writer.AddDocument(d);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#addDocuments(Iterable)"/> and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long AddDocuments(IEnumerable<IEnumerable<IIndexableField>> docs)
        {
            writer.AddDocuments(docs);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#addIndexes(Directory...)"/> and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long AddIndexes(params Directory[] dirs)
        {
            writer.AddIndexes(dirs);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Calls <seealso cref="IndexWriter#addIndexes(IndexReader...)"/>
        ///  and returns the generation that reflects this change.
        /// </summary>
        public virtual long AddIndexes(params IndexReader[] readers)
        {
            writer.AddIndexes(readers);
            // Return gen as of when indexing finished:
            return indexingGen.Get();
        }

        /// <summary>
        /// Return the current generation being indexed. </summary>
        public virtual long Generation
        {
            get
            {
                return indexingGen.Get();
            }
        }

        /// <summary>
        /// Return the wrapped <seealso cref="IndexWriter"/>. </summary>
        public virtual IndexWriter IndexWriter
        {
            get
            {
                return writer;
            }
        }

        /// <summary>
        /// Return and increment current gen.
        ///
        /// @lucene.internal
        /// </summary>
        public virtual long AndIncrementGeneration // LUCENENET TODO: Make method GetAndIncrementGeneration()
        {
            get
            {
                return indexingGen.IncrementAndGet();
            }
        }

        /// <summary>
        /// Cals {@link
        ///  IndexWriter#tryDeleteDocument(IndexReader,int)} and
        ///  returns the generation that reflects this change.
        /// </summary>
        public virtual long TryDeleteDocument(IndexReader reader, int docID)
        {
            if (writer.TryDeleteDocument(reader, docID))
            {
                return indexingGen.Get();
            }
            else
            {
                return -1;
            }
        }
    }
}