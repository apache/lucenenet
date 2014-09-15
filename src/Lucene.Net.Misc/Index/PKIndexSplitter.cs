using System.Diagnostics;
using System.Collections.Generic;

namespace org.apache.lucene.index
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


	using OpenMode = org.apache.lucene.index.IndexWriterConfig.OpenMode;
	using DocIdSet = org.apache.lucene.search.DocIdSet;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using Filter = org.apache.lucene.search.Filter;
	using TermRangeFilter = org.apache.lucene.search.TermRangeFilter;
	using Directory = org.apache.lucene.store.Directory;
	using Bits = org.apache.lucene.util.Bits;
	using FixedBitSet = org.apache.lucene.util.FixedBitSet;
	using IOUtils = org.apache.lucene.util.IOUtils;
	using Version = org.apache.lucene.util.Version;

	/// <summary>
	/// Split an index based on a <seealso cref="Filter"/>.
	/// </summary>

	public class PKIndexSplitter
	{
	  private readonly Filter docsInFirstIndex;
	  private readonly Directory input;
	  private readonly Directory dir1;
	  private readonly Directory dir2;
	  private readonly IndexWriterConfig config1;
	  private readonly IndexWriterConfig config2;

	  /// <summary>
	  /// Split an index based on a <seealso cref="Filter"/>. All documents that match the filter
	  /// are sent to dir1, remaining ones to dir2.
	  /// </summary>
	  public PKIndexSplitter(Version version, Directory input, Directory dir1, Directory dir2, Filter docsInFirstIndex) : this(input, dir1, dir2, docsInFirstIndex, newDefaultConfig(version), newDefaultConfig(version))
	  {
	  }

	  private static IndexWriterConfig newDefaultConfig(Version version)
	  {
		return (new IndexWriterConfig(version, null)).setOpenMode(OpenMode.CREATE);
	  }

	  public PKIndexSplitter(Directory input, Directory dir1, Directory dir2, Filter docsInFirstIndex, IndexWriterConfig config1, IndexWriterConfig config2)
	  {
		this.input = input;
		this.dir1 = dir1;
		this.dir2 = dir2;
		this.docsInFirstIndex = docsInFirstIndex;
		this.config1 = config1;
		this.config2 = config2;
	  }

	  /// <summary>
	  /// Split an index based on a  given primary key term 
	  /// and a 'middle' term.  If the middle term is present, it's
	  /// sent to dir2.
	  /// </summary>
	  public PKIndexSplitter(Version version, Directory input, Directory dir1, Directory dir2, Term midTerm) : this(version, input, dir1, dir2, new TermRangeFilter(midTerm.field(), null, midTerm.bytes(), true, false))
	  {
	  }

	  public PKIndexSplitter(Directory input, Directory dir1, Directory dir2, Term midTerm, IndexWriterConfig config1, IndexWriterConfig config2) : this(input, dir1, dir2, new TermRangeFilter(midTerm.field(), null, midTerm.bytes(), true, false), config1, config2)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void split() throws java.io.IOException
	  public virtual void Split()
	  {
		bool success = false;
		DirectoryReader reader = DirectoryReader.open(input);
		try
		{
		  // pass an individual config in here since one config can not be reused!
		  createIndex(config1, dir1, reader, docsInFirstIndex, false);
		  createIndex(config2, dir2, reader, docsInFirstIndex, true);
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(reader);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(reader);
		  }
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void createIndex(IndexWriterConfig config, org.apache.lucene.store.Directory target, IndexReader reader, org.apache.lucene.search.Filter preserveFilter, boolean negateFilter) throws java.io.IOException
	  private void createIndex(IndexWriterConfig config, Directory target, IndexReader reader, Filter preserveFilter, bool negateFilter)
	  {
		bool success = false;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IndexWriter w = new IndexWriter(target, config);
		IndexWriter w = new IndexWriter(target, config);
		try
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<AtomicReaderContext> leaves = reader.leaves();
		  IList<AtomicReaderContext> leaves = reader.leaves();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IndexReader[] subReaders = new IndexReader[leaves.size()];
		  IndexReader[] subReaders = new IndexReader[leaves.Count];
		  int i = 0;
		  foreach (AtomicReaderContext ctx in leaves)
		  {
			subReaders[i++] = new DocumentFilteredAtomicIndexReader(ctx, preserveFilter, negateFilter);
		  }
		  w.addIndexes(subReaders);
		  success = true;
		}
		finally
		{
		  if (success)
		  {
			IOUtils.close(w);
		  }
		  else
		  {
			IOUtils.closeWhileHandlingException(w);
		  }
		}
	  }

	  private class DocumentFilteredAtomicIndexReader : FilterAtomicReader
	  {
		internal readonly Bits liveDocs;
		internal readonly int numDocs_Renamed;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public DocumentFilteredAtomicIndexReader(AtomicReaderContext context, org.apache.lucene.search.Filter preserveFilter, boolean negateFilter) throws java.io.IOException
		public DocumentFilteredAtomicIndexReader(AtomicReaderContext context, Filter preserveFilter, bool negateFilter) : base(context.reader())
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int maxDoc = in.maxDoc();
		  int maxDoc = @in.maxDoc();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.FixedBitSet bits = new org.apache.lucene.util.FixedBitSet(maxDoc);
		  FixedBitSet bits = new FixedBitSet(maxDoc);
		  // ignore livedocs here, as we filter them later:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.DocIdSet docs = preserveFilter.getDocIdSet(context, null);
		  DocIdSet docs = preserveFilter.getDocIdSet(context, null);
		  if (docs != null)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.DocIdSetIterator it = docs.iterator();
			DocIdSetIterator it = docs.GetEnumerator();
			if (it != null)
			{
			  bits.or(it);
			}
		  }
		  if (negateFilter)
		  {
			bits.flip(0, maxDoc);
		  }

		  if (@in.hasDeletions())
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits oldLiveDocs = in.getLiveDocs();
			Bits oldLiveDocs = @in.LiveDocs;
			Debug.Assert(oldLiveDocs != null);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.DocIdSetIterator it = bits.iterator();
			DocIdSetIterator it = bits.GetEnumerator();
			for (int i = it.nextDoc(); i < maxDoc; i = it.nextDoc())
			{
			  if (!oldLiveDocs.get(i))
			  {
				// we can safely modify the current bit, as the iterator already stepped over it:
				bits.clear(i);
			  }
			}
		  }

		  this.liveDocs = bits;
		  this.numDocs_Renamed = bits.cardinality();
		}

		public override int numDocs()
		{
		  return numDocs_Renamed;
		}

		public override Bits LiveDocs
		{
			get
			{
			  return liveDocs;
			}
		}
	  }
	}

}