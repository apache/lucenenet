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
	using Document = Lucene.Net.Document.Document;
	using Lucene.Net.Search; // javadocs
	using Query = Lucene.Net.Search.Query;
	using Directory = Lucene.Net.Store.Directory;

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
	  private readonly IndexWriter Writer;
	  private readonly AtomicLong IndexingGen = new AtomicLong(1);

	  /// <summary>
	  /// Create a {@code TrackingIndexWriter} wrapping the
	  ///  provided <seealso cref="IndexWriter"/>. 
	  /// </summary>
	  public TrackingIndexWriter(IndexWriter writer)
	  {
		this.Writer = writer;
	  }

	  /// <summary>
	  /// Calls {@link
	  ///  IndexWriter#updateDocument(Term,Iterable,Analyzer)}
	  ///  and returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long updateDocument<T1>(Term t, IEnumerable<T1> d, Analyzer a) where T1 : IndexableField
	  {
		Writer.UpdateDocument(t, d, a);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls {@link
	  ///  IndexWriter#updateDocument(Term,Iterable)} and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long updateDocument<T1>(Term t, IEnumerable<T1> d) where T1 : IndexableField
	  {
		Writer.UpdateDocument(t, d);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls {@link
	  ///  IndexWriter#updateDocuments(Term,Iterable,Analyzer)}
	  ///  and returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long updateDocuments<T1>(Term t, IEnumerable<T1> docs, Analyzer a) where T1 : Iterable<T1 extends IndexableField>
	  {
		Writer.UpdateDocuments(t, docs, a);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls {@link
	  ///  IndexWriter#updateDocuments(Term,Iterable)} and returns
	  ///  the generation that reflects this change. 
	  /// </summary>
	  public virtual long updateDocuments<T1>(Term t, IEnumerable<T1> docs) where T1 : Iterable<T1 extends IndexableField>
	  {
		Writer.UpdateDocuments(t, docs);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#deleteDocuments(Term)"/> and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long DeleteDocuments(Term t)
	  {
		Writer.DeleteDocuments(t);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#deleteDocuments(Term...)"/> and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long DeleteDocuments(params Term[] terms)
	  {
		Writer.DeleteDocuments(terms);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#deleteDocuments(Query)"/> and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long DeleteDocuments(Query q)
	  {
		Writer.DeleteDocuments(q);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#deleteDocuments(Query...)"/>
	  ///  and returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long DeleteDocuments(params Query[] queries)
	  {
		Writer.DeleteDocuments(queries);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#deleteAll"/> and returns the
	  ///  generation that reflects this change. 
	  /// </summary>
	  public virtual long DeleteAll()
	  {
		Writer.DeleteAll();
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls {@link
	  ///  IndexWriter#addDocument(Iterable,Analyzer)} and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long addDocument<T1>(IEnumerable<T1> d, Analyzer a) where T1 : IndexableField
	  {
		Writer.AddDocument(d, a);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls {@link
	  ///  IndexWriter#addDocuments(Iterable,Analyzer)} and
	  ///  returns the generation that reflects this change.  
	  /// </summary>
	  public virtual long addDocuments<T1>(IEnumerable<T1> docs, Analyzer a) where T1 : Iterable<T1 extends IndexableField>
	  {
		Writer.AddDocuments(docs, a);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#addDocument(Iterable)"/>
	  ///  and returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long addDocument<T1>(IEnumerable<T1> d) where T1 : IndexableField
	  {
		Writer.AddDocument(d);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#addDocuments(Iterable)"/> and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long addDocuments<T1>(IEnumerable<T1> docs) where T1 : Iterable<T1 extends IndexableField>
	  {
		Writer.AddDocuments(docs);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#addIndexes(Directory...)"/> and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long AddIndexes(params Directory[] dirs)
	  {
		Writer.AddIndexes(dirs);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Calls <seealso cref="IndexWriter#addIndexes(IndexReader...)"/>
	  ///  and returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long AddIndexes(params IndexReader[] readers)
	  {
		Writer.AddIndexes(readers);
		// Return gen as of when indexing finished:
		return IndexingGen.get();
	  }

	  /// <summary>
	  /// Return the current generation being indexed. </summary>
	  public virtual long Generation
	  {
		  get
		  {
			return IndexingGen.get();
		  }
	  }

	  /// <summary>
	  /// Return the wrapped <seealso cref="IndexWriter"/>. </summary>
	  public virtual IndexWriter IndexWriter
	  {
		  get
		  {
			return Writer;
		  }
	  }

	  /// <summary>
	  /// Return and increment current gen.
	  /// 
	  /// @lucene.internal 
	  /// </summary>
	  public virtual long AndIncrementGeneration
	  {
		  get
		  {
			return IndexingGen.AndIncrement;
		  }
	  }

	  /// <summary>
	  /// Cals {@link
	  ///  IndexWriter#tryDeleteDocument(IndexReader,int)} and
	  ///  returns the generation that reflects this change. 
	  /// </summary>
	  public virtual long TryDeleteDocument(IndexReader reader, int docID)
	  {
		if (Writer.TryDeleteDocument(reader, docID))
		{
		  return IndexingGen.get();
		}
		else
		{
		  return -1;
		}
	  }
	}


}