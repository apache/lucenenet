/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Index
{
	/// <summary>
	/// Split an index based on a
	/// <see cref="Org.Apache.Lucene.Search.Filter">Org.Apache.Lucene.Search.Filter</see>
	/// .
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
		/// Split an index based on a
		/// <see cref="Org.Apache.Lucene.Search.Filter">Org.Apache.Lucene.Search.Filter</see>
		/// . All documents that match the filter
		/// are sent to dir1, remaining ones to dir2.
		/// </summary>
		public PKIndexSplitter(Version version, Directory input, Directory dir1, Directory
			 dir2, Filter docsInFirstIndex) : this(input, dir1, dir2, docsInFirstIndex, NewDefaultConfig
			(version), NewDefaultConfig(version))
		{
		}

		private static IndexWriterConfig NewDefaultConfig(Version version)
		{
			return new IndexWriterConfig(version, null).SetOpenMode(IndexWriterConfig.OpenMode
				.CREATE);
		}

		public PKIndexSplitter(Directory input, Directory dir1, Directory dir2, Filter docsInFirstIndex
			, IndexWriterConfig config1, IndexWriterConfig config2)
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
		/// and a 'middle' term.
		/// </summary>
		/// <remarks>
		/// Split an index based on a  given primary key term
		/// and a 'middle' term.  If the middle term is present, it's
		/// sent to dir2.
		/// </remarks>
		public PKIndexSplitter(Version version, Directory input, Directory dir1, Directory
			 dir2, Term midTerm) : this(version, input, dir1, dir2, new TermRangeFilter(midTerm
			.Field(), null, midTerm.Bytes(), true, false))
		{
		}

		public PKIndexSplitter(Directory input, Directory dir1, Directory dir2, Term midTerm
			, IndexWriterConfig config1, IndexWriterConfig config2) : this(input, dir1, dir2
			, new TermRangeFilter(midTerm.Field(), null, midTerm.Bytes(), true, false), config1
			, config2)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual void Split()
		{
			bool success = false;
			DirectoryReader reader = DirectoryReader.Open(input);
			try
			{
				// pass an individual config in here since one config can not be reused!
				CreateIndex(config1, dir1, reader, docsInFirstIndex, false);
				CreateIndex(config2, dir2, reader, docsInFirstIndex, true);
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(reader);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(reader);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		private void CreateIndex(IndexWriterConfig config, Directory target, IndexReader 
			reader, Filter preserveFilter, bool negateFilter)
		{
			bool success = false;
			IndexWriter w = new IndexWriter(target, config);
			try
			{
				IList<AtomicReaderContext> leaves = reader.Leaves();
				IndexReader[] subReaders = new IndexReader[leaves.Count];
				int i = 0;
				foreach (AtomicReaderContext ctx in leaves)
				{
					subReaders[i++] = new PKIndexSplitter.DocumentFilteredAtomicIndexReader(ctx, preserveFilter
						, negateFilter);
				}
				w.AddIndexes(subReaders);
				success = true;
			}
			finally
			{
				if (success)
				{
					IOUtils.Close(w);
				}
				else
				{
					IOUtils.CloseWhileHandlingException(w);
				}
			}
		}

		private class DocumentFilteredAtomicIndexReader : FilterAtomicReader
		{
			internal readonly Bits liveDocs;

			internal readonly int numDocs;

			/// <exception cref="System.IO.IOException"></exception>
			public DocumentFilteredAtomicIndexReader(AtomicReaderContext context, Filter preserveFilter
				, bool negateFilter) : base(((AtomicReader)context.Reader()))
			{
				int maxDoc = @in.MaxDoc();
				FixedBitSet bits = new FixedBitSet(maxDoc);
				// ignore livedocs here, as we filter them later:
				DocIdSet docs = preserveFilter.GetDocIdSet(context, null);
				if (docs != null)
				{
					DocIdSetIterator it = docs.Iterator();
					if (it != null)
					{
						bits.Or(it);
					}
				}
				if (negateFilter)
				{
					bits.Flip(0, maxDoc);
				}
				if (@in.HasDeletions())
				{
					Bits oldLiveDocs = @in.GetLiveDocs();
					DocIdSetIterator it = oldLiveDocs != null.Iterator();
					for (int i = it.NextDoc(); i < maxDoc; i = it.NextDoc())
					{
						if (!oldLiveDocs.Get(i))
						{
							// we can safely modify the current bit, as the iterator already stepped over it:
							bits.Clear(i);
						}
					}
				}
				this.liveDocs = bits;
				this.numDocs = bits.Cardinality();
			}

			public override int NumDocs()
			{
				return numDocs;
			}

			public override Bits GetLiveDocs()
			{
				return liveDocs;
			}
		}
	}
}
