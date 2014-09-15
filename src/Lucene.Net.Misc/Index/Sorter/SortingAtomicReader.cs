using System;
using System.Diagnostics;

namespace org.apache.lucene.index.sorter
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


	using IndexOptions = org.apache.lucene.index.FieldInfo.IndexOptions;
	using DocIdSetIterator = org.apache.lucene.search.DocIdSetIterator;
	using Sort = org.apache.lucene.search.Sort;
	using IndexInput = org.apache.lucene.store.IndexInput;
	using IndexOutput = org.apache.lucene.store.IndexOutput;
	using RAMFile = org.apache.lucene.store.RAMFile;
	using RAMInputStream = org.apache.lucene.store.RAMInputStream;
	using RAMOutputStream = org.apache.lucene.store.RAMOutputStream;
	using ArrayUtil = org.apache.lucene.util.ArrayUtil;
	using Bits = org.apache.lucene.util.Bits;
	using BytesRef = org.apache.lucene.util.BytesRef;
	using TimSorter = org.apache.lucene.util.TimSorter;
	using CompiledAutomaton = org.apache.lucene.util.automaton.CompiledAutomaton;

	/// <summary>
	/// An <seealso cref="AtomicReader"/> which supports sorting documents by a given
	/// <seealso cref="Sort"/>. You can use this class to sort an index as follows:
	/// 
	/// <pre class="prettyprint">
	/// IndexWriter writer; // writer to which the sorted index will be added
	/// DirectoryReader reader; // reader on the input index
	/// Sort sort; // determines how the documents are sorted
	/// AtomicReader sortingReader = SortingAtomicReader.wrap(SlowCompositeReaderWrapper.wrap(reader), sort);
	/// writer.addIndexes(reader);
	/// writer.close();
	/// reader.close();
	/// </pre>
	/// 
	/// @lucene.experimental
	/// </summary>
	public class SortingAtomicReader : FilterAtomicReader
	{

	  private class SortingFields : FilterFields
	  {

		internal readonly Sorter.DocMap docMap;
		internal readonly FieldInfos infos;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public SortingFields(final org.apache.lucene.index.Fields in, org.apache.lucene.index.FieldInfos infos, Sorter.DocMap docMap)
		public SortingFields(Fields @in, FieldInfos infos, Sorter.DocMap docMap) : base(@in)
		{
		  this.docMap = docMap;
		  this.infos = infos;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.Terms terms(final String field) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public override Terms terms(string field)
		{
		  Terms terms = @in.terms(field);
		  if (terms == null)
		  {
			return null;
		  }
		  else
		  {
			return new SortingTerms(terms, infos.fieldInfo(field).IndexOptions, docMap);
		  }
		}

	  }

	  private class SortingTerms : FilterTerms
	  {

		internal readonly Sorter.DocMap docMap;
		internal readonly IndexOptions indexOptions;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public SortingTerms(final org.apache.lucene.index.Terms in, org.apache.lucene.index.FieldInfo.IndexOptions indexOptions, final Sorter.DocMap docMap)
		public SortingTerms(Terms @in, IndexOptions indexOptions, Sorter.DocMap docMap) : base(@in)
		{
		  this.docMap = docMap;
		  this.indexOptions = indexOptions;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.TermsEnum iterator(final org.apache.lucene.index.TermsEnum reuse) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public override TermsEnum iterator(TermsEnum reuse)
		{
		  return new SortingTermsEnum(@in.iterator(reuse), docMap, indexOptions);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.TermsEnum intersect(org.apache.lucene.util.automaton.CompiledAutomaton compiled, org.apache.lucene.util.BytesRef startTerm) throws java.io.IOException
		public override TermsEnum intersect(CompiledAutomaton compiled, BytesRef startTerm)
		{
		  return new SortingTermsEnum(@in.intersect(compiled, startTerm), docMap, indexOptions);
		}

	  }

	  private class SortingTermsEnum : FilterTermsEnum
	  {

		internal readonly Sorter.DocMap docMap; // pkg-protected to avoid synthetic accessor methods
		internal readonly IndexOptions indexOptions;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public SortingTermsEnum(final org.apache.lucene.index.TermsEnum in, Sorter.DocMap docMap, org.apache.lucene.index.FieldInfo.IndexOptions indexOptions)
		public SortingTermsEnum(TermsEnum @in, Sorter.DocMap docMap, IndexOptions indexOptions) : base(@in)
		{
		  this.docMap = docMap;
		  this.indexOptions = indexOptions;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: org.apache.lucene.util.Bits newToOld(final org.apache.lucene.util.Bits liveDocs)
		internal virtual Bits newToOld(Bits liveDocs)
		{
		  if (liveDocs == null)
		  {
			return null;
		  }
		  return new BitsAnonymousInnerClassHelper(this, liveDocs);
		}

		private class BitsAnonymousInnerClassHelper : Bits
		{
			private readonly SortingTermsEnum outerInstance;

			private Bits liveDocs;

			public BitsAnonymousInnerClassHelper(SortingTermsEnum outerInstance, Bits liveDocs)
			{
				this.outerInstance = outerInstance;
				this.liveDocs = liveDocs;
			}


			public override bool get(int index)
			{
			  return liveDocs.get(outerInstance.docMap.oldToNew(index));
			}

			public override int length()
			{
			  return liveDocs.length();
			}

		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.DocsEnum docs(org.apache.lucene.util.Bits liveDocs, org.apache.lucene.index.DocsEnum reuse, final int flags) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public override DocsEnum docs(Bits liveDocs, DocsEnum reuse, int flags)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.DocsEnum inReuse;
		  DocsEnum inReuse;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SortingDocsEnum wrapReuse;
		  SortingDocsEnum wrapReuse;
		  if (reuse != null && reuse is SortingDocsEnum)
		  {
			// if we're asked to reuse the given DocsEnum and it is Sorting, return
			// the wrapped one, since some Codecs expect it.
			wrapReuse = (SortingDocsEnum) reuse;
			inReuse = wrapReuse.Wrapped;
		  }
		  else
		  {
			wrapReuse = null;
			inReuse = reuse;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.DocsEnum inDocs = in.docs(newToOld(liveDocs), inReuse, flags);
		  DocsEnum inDocs = @in.docs(newToOld(liveDocs), inReuse, flags);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean withFreqs = indexOptions.compareTo(org.apache.lucene.index.FieldInfo.IndexOptions.DOCS_AND_FREQS) >=0 && (flags & org.apache.lucene.index.DocsEnum.FLAG_FREQS) != 0;
		  bool withFreqs = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS) >= 0 && (flags & DocsEnum.FLAG_FREQS) != 0;
		  return new SortingDocsEnum(docMap.size(), wrapReuse, inDocs, withFreqs, docMap);
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.DocsAndPositionsEnum docsAndPositions(org.apache.lucene.util.Bits liveDocs, org.apache.lucene.index.DocsAndPositionsEnum reuse, final int flags) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public override DocsAndPositionsEnum docsAndPositions(Bits liveDocs, DocsAndPositionsEnum reuse, int flags)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.DocsAndPositionsEnum inReuse;
		  DocsAndPositionsEnum inReuse;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SortingDocsAndPositionsEnum wrapReuse;
		  SortingDocsAndPositionsEnum wrapReuse;
		  if (reuse != null && reuse is SortingDocsAndPositionsEnum)
		  {
			// if we're asked to reuse the given DocsEnum and it is Sorting, return
			// the wrapped one, since some Codecs expect it.
			wrapReuse = (SortingDocsAndPositionsEnum) reuse;
			inReuse = wrapReuse.Wrapped;
		  }
		  else
		  {
			wrapReuse = null;
			inReuse = reuse;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.DocsAndPositionsEnum inDocsAndPositions = in.docsAndPositions(newToOld(liveDocs), inReuse, flags);
		  DocsAndPositionsEnum inDocsAndPositions = @in.docsAndPositions(newToOld(liveDocs), inReuse, flags);
		  if (inDocsAndPositions == null)
		  {
			return null;
		  }

		  // we ignore the fact that offsets may be stored but not asked for,
		  // since this code is expected to be used during addIndexes which will
		  // ask for everything. if that assumption changes in the future, we can
		  // factor in whether 'flags' says offsets are not required.
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final boolean storeOffsets = indexOptions.compareTo(org.apache.lucene.index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		  bool storeOffsets = indexOptions.compareTo(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS) >= 0;
		  return new SortingDocsAndPositionsEnum(docMap.size(), wrapReuse, inDocsAndPositions, docMap, storeOffsets);
		}

	  }

	  private class SortingBinaryDocValues : BinaryDocValues
	  {

		internal readonly BinaryDocValues @in;
		internal readonly Sorter.DocMap docMap;

		internal SortingBinaryDocValues(BinaryDocValues @in, Sorter.DocMap docMap)
		{
		  this.@in = @in;
		  this.docMap = docMap;
		}

		public override void get(int docID, BytesRef result)
		{
		  @in.get(docMap.newToOld(docID), result);
		}
	  }

	  private class SortingNumericDocValues : NumericDocValues
	  {

		internal readonly NumericDocValues @in;
		internal readonly Sorter.DocMap docMap;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public SortingNumericDocValues(final org.apache.lucene.index.NumericDocValues in, Sorter.DocMap docMap)
		public SortingNumericDocValues(NumericDocValues @in, Sorter.DocMap docMap)
		{
		  this.@in = @in;
		  this.docMap = docMap;
		}

		public override long get(int docID)
		{
		  return @in.get(docMap.newToOld(docID));
		}
	  }

	  private class SortingBits : Bits
	  {

		internal readonly Bits @in;
		internal readonly Sorter.DocMap docMap;

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public SortingBits(final org.apache.lucene.util.Bits in, Sorter.DocMap docMap)
		public SortingBits(Bits @in, Sorter.DocMap docMap)
		{
		  this.@in = @in;
		  this.docMap = docMap;
		}

		public override bool get(int index)
		{
		  return @in.get(docMap.newToOld(index));
		}

		public override int length()
		{
		  return @in.length();
		}
	  }

	  private class SortingSortedDocValues : SortedDocValues
	  {

		internal readonly SortedDocValues @in;
		internal readonly Sorter.DocMap docMap;

		internal SortingSortedDocValues(SortedDocValues @in, Sorter.DocMap docMap)
		{
		  this.@in = @in;
		  this.docMap = docMap;
		}

		public override int getOrd(int docID)
		{
		  return @in.getOrd(docMap.newToOld(docID));
		}

		public override void lookupOrd(int ord, BytesRef result)
		{
		  @in.lookupOrd(ord, result);
		}

		public override int ValueCount
		{
			get
			{
			  return @in.ValueCount;
			}
		}

		public override void get(int docID, BytesRef result)
		{
		  @in.get(docMap.newToOld(docID), result);
		}

		public override int lookupTerm(BytesRef key)
		{
		  return @in.lookupTerm(key);
		}
	  }

	  private class SortingSortedSetDocValues : SortedSetDocValues
	  {

		internal readonly SortedSetDocValues @in;
		internal readonly Sorter.DocMap docMap;

		internal SortingSortedSetDocValues(SortedSetDocValues @in, Sorter.DocMap docMap)
		{
		  this.@in = @in;
		  this.docMap = docMap;
		}

		public override long nextOrd()
		{
		  return @in.nextOrd();
		}

		public override int Document
		{
			set
			{
			  @in.Document = docMap.newToOld(value);
			}
		}

		public override void lookupOrd(long ord, BytesRef result)
		{
		  @in.lookupOrd(ord, result);
		}

		public override long ValueCount
		{
			get
			{
			  return @in.ValueCount;
			}
		}

		public override long lookupTerm(BytesRef key)
		{
		  return @in.lookupTerm(key);
		}
	  }

	  internal class SortingDocsEnum : FilterDocsEnum
	  {

		private sealed class DocFreqSorter : TimSorter
		{

		  internal int[] docs;
		  internal int[] freqs;
		  internal readonly int[] tmpDocs;
		  internal int[] tmpFreqs;

		  public DocFreqSorter(int maxDoc) : base(maxDoc / 64)
		  {
			this.tmpDocs = new int[maxDoc / 64];
		  }

		  public void reset(int[] docs, int[] freqs)
		  {
			this.docs = docs;
			this.freqs = freqs;
			if (freqs != null && tmpFreqs == null)
			{
			  tmpFreqs = new int[tmpDocs.Length];
			}
		  }

		  protected internal override int compare(int i, int j)
		  {
			return docs[i] - docs[j];
		  }

		  protected internal override void swap(int i, int j)
		  {
			int tmpDoc = docs[i];
			docs[i] = docs[j];
			docs[j] = tmpDoc;

			if (freqs != null)
			{
			  int tmpFreq = freqs[i];
			  freqs[i] = freqs[j];
			  freqs[j] = tmpFreq;
			}
		  }

		  protected internal override void copy(int src, int dest)
		  {
			docs[dest] = docs[src];
			if (freqs != null)
			{
			  freqs[dest] = freqs[src];
			}
		  }

		  protected internal override void save(int i, int len)
		  {
			Array.Copy(docs, i, tmpDocs, 0, len);
			if (freqs != null)
			{
			  Array.Copy(freqs, i, tmpFreqs, 0, len);
			}
		  }

		  protected internal override void restore(int i, int j)
		  {
			docs[j] = tmpDocs[i];
			if (freqs != null)
			{
			  freqs[j] = tmpFreqs[i];
			}
		  }

		  protected internal override int compareSaved(int i, int j)
		  {
			return tmpDocs[i] - docs[j];
		  }
		}

		internal readonly int maxDoc;
		internal readonly DocFreqSorter sorter;
		internal int[] docs;
		internal int[] freqs;
		internal int docIt = -1;
		internal readonly int upto;
		internal readonly bool withFreqs;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: SortingDocsEnum(int maxDoc, SortingDocsEnum reuse, final org.apache.lucene.index.DocsEnum in, boolean withFreqs, final Sorter.DocMap docMap) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		internal SortingDocsEnum(int maxDoc, SortingDocsEnum reuse, DocsEnum @in, bool withFreqs, Sorter.DocMap docMap) : base(@in)
		{
		  this.maxDoc = maxDoc;
		  this.withFreqs = withFreqs;
		  if (reuse != null)
		  {
			if (reuse.maxDoc == maxDoc)
			{
			  sorter = reuse.sorter;
			}
			else
			{
			  sorter = new DocFreqSorter(maxDoc);
			}
			docs = reuse.docs;
			freqs = reuse.freqs; // maybe null
		  }
		  else
		  {
			docs = new int[64];
			sorter = new DocFreqSorter(maxDoc);
		  }
		  docIt = -1;
		  int i = 0;
		  int doc;
		  if (withFreqs)
		  {
			if (freqs == null || freqs.Length < docs.Length)
			{
			  freqs = new int[docs.Length];
			}
			while ((doc = @in.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
			  if (i >= docs.Length)
			  {
				docs = ArrayUtil.grow(docs, docs.Length + 1);
				freqs = ArrayUtil.grow(freqs, freqs.Length + 1);
			  }
			  docs[i] = docMap.oldToNew(doc);
			  freqs[i] = @in.freq();
			  ++i;
			}
		  }
		  else
		  {
			freqs = null;
			while ((doc = @in.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
			  if (i >= docs.Length)
			  {
				docs = ArrayUtil.grow(docs, docs.Length + 1);
			  }
			  docs[i++] = docMap.oldToNew(doc);
			}
		  }
		  // TimSort can save much time compared to other sorts in case of
		  // reverse sorting, or when sorting a concatenation of sorted readers
		  sorter.reset(docs, freqs);
		  sorter.sort(0, i);
		  upto = i;
		}

		// for testing
		internal virtual bool reused(DocsEnum other)
		{
		  if (other == null || !(other is SortingDocsEnum))
		  {
			return false;
		  }
		  return docs == ((SortingDocsEnum) other).docs;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(final int target) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public override int advance(int target)
		{
		  // need to support it for checkIndex, but in practice it won't be called, so
		  // don't bother to implement efficiently for now.
		  return slowAdvance(target);
		}

		public override int docID()
		{
		  return docIt < 0 ? - 1 : docIt >= upto ? NO_MORE_DOCS : docs[docIt];
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
		public override int freq()
		{
		  return withFreqs && docIt < upto ? freqs[docIt] : 1;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int nextDoc() throws java.io.IOException
		public override int nextDoc()
		{
		  if (++docIt >= upto)
		  {
			  return NO_MORE_DOCS;
		  }
		  return docs[docIt];
		}

		/// <summary>
		/// Returns the wrapped <seealso cref="DocsEnum"/>. </summary>
		internal virtual DocsEnum Wrapped
		{
			get
			{
			  return @in;
			}
		}
	  }

	  internal class SortingDocsAndPositionsEnum : FilterDocsAndPositionsEnum
	  {

		/// <summary>
		/// A <seealso cref="TimSorter"/> which sorts two parallel arrays of doc IDs and
		/// offsets in one go. Everytime a doc ID is 'swapped', its correponding offset
		/// is swapped too.
		/// </summary>
		private sealed class DocOffsetSorter : TimSorter
		{

		  internal int[] docs;
		  internal long[] offsets;
		  internal readonly int[] tmpDocs;
		  internal readonly long[] tmpOffsets;

		  public DocOffsetSorter(int maxDoc) : base(maxDoc / 64)
		  {
			this.tmpDocs = new int[maxDoc / 64];
			this.tmpOffsets = new long[maxDoc / 64];
		  }

		  public void reset(int[] docs, long[] offsets)
		  {
			this.docs = docs;
			this.offsets = offsets;
		  }

		  protected internal override int compare(int i, int j)
		  {
			return docs[i] - docs[j];
		  }

		  protected internal override void swap(int i, int j)
		  {
			int tmpDoc = docs[i];
			docs[i] = docs[j];
			docs[j] = tmpDoc;

			long tmpOffset = offsets[i];
			offsets[i] = offsets[j];
			offsets[j] = tmpOffset;
		  }

		  protected internal override void copy(int src, int dest)
		  {
			docs[dest] = docs[src];
			offsets[dest] = offsets[src];
		  }

		  protected internal override void save(int i, int len)
		  {
			Array.Copy(docs, i, tmpDocs, 0, len);
			Array.Copy(offsets, i, tmpOffsets, 0, len);
		  }

		  protected internal override void restore(int i, int j)
		  {
			docs[j] = tmpDocs[i];
			offsets[j] = tmpOffsets[i];
		  }

		  protected internal override int compareSaved(int i, int j)
		  {
			return tmpDocs[i] - docs[j];
		  }
		}

		internal readonly int maxDoc;
		internal readonly DocOffsetSorter sorter;
		internal int[] docs;
		internal long[] offsets;
		internal readonly int upto;

		internal readonly IndexInput postingInput;
		internal readonly bool storeOffsets;

		internal int docIt = -1;
		internal int pos;
		internal int startOffset_Renamed = -1;
		internal int endOffset_Renamed = -1;
		internal readonly BytesRef payload;
		internal int currFreq;

		internal readonly RAMFile file;

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: SortingDocsAndPositionsEnum(int maxDoc, SortingDocsAndPositionsEnum reuse, final org.apache.lucene.index.DocsAndPositionsEnum in, Sorter.DocMap docMap, boolean storeOffsets) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		internal SortingDocsAndPositionsEnum(int maxDoc, SortingDocsAndPositionsEnum reuse, DocsAndPositionsEnum @in, Sorter.DocMap docMap, bool storeOffsets) : base(@in)
		{
		  this.maxDoc = maxDoc;
		  this.storeOffsets = storeOffsets;
		  if (reuse != null)
		  {
			docs = reuse.docs;
			offsets = reuse.offsets;
			payload = reuse.payload;
			file = reuse.file;
			if (reuse.maxDoc == maxDoc)
			{
			  sorter = reuse.sorter;
			}
			else
			{
			  sorter = new DocOffsetSorter(maxDoc);
			}
		  }
		  else
		  {
			docs = new int[32];
			offsets = new long[32];
			payload = new BytesRef(32);
			file = new RAMFile();
			sorter = new DocOffsetSorter(maxDoc);
		  }
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.store.IndexOutput out = new org.apache.lucene.store.RAMOutputStream(file);
		  IndexOutput @out = new RAMOutputStream(file);
		  int doc;
		  int i = 0;
		  while ((doc = @in.nextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
		  {
			if (i == docs.Length)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int newLength = org.apache.lucene.util.ArrayUtil.oversize(i + 1, 4);
			  int newLength = ArrayUtil.oversize(i + 1, 4);
			  docs = Arrays.copyOf(docs, newLength);
			  offsets = Arrays.copyOf(offsets, newLength);
			}
			docs[i] = docMap.oldToNew(doc);
			offsets[i] = @out.FilePointer;
			addPositions(@in, @out);
			i++;
		  }
		  upto = i;
		  sorter.reset(docs, offsets);
		  sorter.sort(0, upto);
		  @out.close();
		  this.postingInput = new RAMInputStream("", file);
		}

		// for testing
		internal virtual bool reused(DocsAndPositionsEnum other)
		{
		  if (other == null || !(other is SortingDocsAndPositionsEnum))
		  {
			return false;
		  }
		  return docs == ((SortingDocsAndPositionsEnum) other).docs;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private void addPositions(final org.apache.lucene.index.DocsAndPositionsEnum in, final org.apache.lucene.store.IndexOutput out) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		internal virtual void addPositions(DocsAndPositionsEnum @in, IndexOutput @out)
		{
		  int freq = @in.freq();
		  @out.writeVInt(freq);
		  int previousPosition = 0;
		  int previousEndOffset = 0;
		  for (int i = 0; i < freq; i++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int pos = in.nextPosition();
			int pos = @in.nextPosition();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.BytesRef payload = in.getPayload();
			BytesRef payload = @in.Payload;
			// The low-order bit of token is set only if there is a payload, the
			// previous bits are the delta-encoded position. 
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int token = (pos - previousPosition) << 1 | (payload == null ? 0 : 1);
			int token = (pos - previousPosition) << 1 | (payload == null ? 0 : 1);
			@out.writeVInt(token);
			previousPosition = pos;
			if (storeOffsets) // don't encode offsets if they are not stored
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int startOffset = in.startOffset();
			  int startOffset = @in.startOffset();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int endOffset = in.endOffset();
			  int endOffset = @in.endOffset();
			  @out.writeVInt(startOffset - previousEndOffset);
			  @out.writeVInt(endOffset - startOffset);
			  previousEndOffset = endOffset;
			}
			if (payload != null)
			{
			  @out.writeVInt(payload.length);
			  @out.writeBytes(payload.bytes, payload.offset, payload.length);
			}
		  }
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int advance(final int target) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
		public override int advance(int target)
		{
		  // need to support it for checkIndex, but in practice it won't be called, so
		  // don't bother to implement efficiently for now.
		  return slowAdvance(target);
		}

		public override int docID()
		{
		  return docIt < 0 ? - 1 : docIt >= upto ? NO_MORE_DOCS : docs[docIt];
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int endOffset() throws java.io.IOException
		public override int endOffset()
		{
		  return endOffset_Renamed;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int freq() throws java.io.IOException
		public override int freq()
		{
		  return currFreq;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.BytesRef getPayload() throws java.io.IOException
		public override BytesRef Payload
		{
			get
			{
			  return payload.length == 0 ? null : payload;
			}
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int nextDoc() throws java.io.IOException
		public override int nextDoc()
		{
		  if (++docIt >= upto)
		  {
			  return DocIdSetIterator.NO_MORE_DOCS;
		  }
		  postingInput.seek(offsets[docIt]);
		  currFreq = postingInput.readVInt();
		  // reset variables used in nextPosition
		  pos = 0;
		  endOffset_Renamed = 0;
		  return docs[docIt];
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int nextPosition() throws java.io.IOException
		public override int nextPosition()
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int token = postingInput.readVInt();
		  int token = postingInput.readVInt();
		  pos += (int)((uint)token >> 1);
		  if (storeOffsets)
		  {
			startOffset_Renamed = endOffset_Renamed + postingInput.readVInt();
			endOffset_Renamed = startOffset_Renamed + postingInput.readVInt();
		  }
		  if ((token & 1) != 0)
		  {
			payload.offset = 0;
			payload.length = postingInput.readVInt();
			if (payload.length > payload.bytes.length)
			{
			  payload.bytes = new sbyte[ArrayUtil.oversize(payload.length, 1)];
			}
			postingInput.readBytes(payload.bytes, 0, payload.length);
		  }
		  else
		  {
			payload.length = 0;
		  }
		  return pos;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public int startOffset() throws java.io.IOException
		public override int startOffset()
		{
		  return startOffset_Renamed;
		}

		/// <summary>
		/// Returns the wrapped <seealso cref="DocsAndPositionsEnum"/>. </summary>
		internal virtual DocsAndPositionsEnum Wrapped
		{
			get
			{
			  return @in;
			}
		}
	  }

	  /// <summary>
	  /// Return a sorted view of <code>reader</code> according to the order
	  ///  defined by <code>sort</code>. If the reader is already sorted, this
	  ///  method might return the reader as-is. 
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public static org.apache.lucene.index.AtomicReader wrap(org.apache.lucene.index.AtomicReader reader, org.apache.lucene.search.Sort sort) throws java.io.IOException
	  public static AtomicReader wrap(AtomicReader reader, Sort sort)
	  {
		return wrap(reader, (new Sorter(sort)).sort(reader));
	  }

	  /// <summary>
	  /// Expert: same as <seealso cref="#wrap(AtomicReader, Sort)"/> but operates directly on a <seealso cref="Sorter.DocMap"/>. </summary>
	  internal static AtomicReader wrap(AtomicReader reader, Sorter.DocMap docMap)
	  {
		if (docMap == null)
		{
		  // the reader is already sorter
		  return reader;
		}
		if (reader.maxDoc() != docMap.size())
		{
		  throw new System.ArgumentException("reader.maxDoc() should be equal to docMap.size(), got" + reader.maxDoc() + " != " + docMap.size());
		}
		Debug.Assert(Sorter.isConsistent(docMap));
		return new SortingAtomicReader(reader, docMap);
	  }

	  internal readonly Sorter.DocMap docMap; // pkg-protected to avoid synthetic accessor methods

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: private SortingAtomicReader(final org.apache.lucene.index.AtomicReader in, final Sorter.DocMap docMap)
	  private SortingAtomicReader(AtomicReader @in, Sorter.DocMap docMap) : base(@in)
	  {
		this.docMap = docMap;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void document(final int docID, final org.apache.lucene.index.StoredFieldVisitor visitor) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public override void document(int docID, StoredFieldVisitor visitor)
	  {
		@in.document(docMap.newToOld(docID), visitor);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.Fields fields() throws java.io.IOException
	  public override Fields fields()
	  {
		Fields fields = @in.fields();
		if (fields == null)
		{
		  return null;
		}
		else
		{
		  return new SortingFields(fields, @in.FieldInfos, docMap);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.BinaryDocValues getBinaryDocValues(String field) throws java.io.IOException
	  public override BinaryDocValues getBinaryDocValues(string field)
	  {
		BinaryDocValues oldDocValues = @in.getBinaryDocValues(field);
		if (oldDocValues == null)
		{
		  return null;
		}
		else
		{
		  return new SortingBinaryDocValues(oldDocValues, docMap);
		}
	  }

	  public override Bits LiveDocs
	  {
		  get
		  {
	//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
	//ORIGINAL LINE: final org.apache.lucene.util.Bits inLiveDocs = in.getLiveDocs();
			Bits inLiveDocs = @in.LiveDocs;
			if (inLiveDocs == null)
			{
			  return null;
			}
			else
			{
			  return new SortingBits(inLiveDocs, docMap);
			}
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.NumericDocValues getNormValues(String field) throws java.io.IOException
	  public override NumericDocValues getNormValues(string field)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.NumericDocValues norm = in.getNormValues(field);
		NumericDocValues norm = @in.getNormValues(field);
		if (norm == null)
		{
		  return null;
		}
		else
		{
		  return new SortingNumericDocValues(norm, docMap);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.NumericDocValues getNumericDocValues(String field) throws java.io.IOException
	  public override NumericDocValues getNumericDocValues(string field)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.NumericDocValues oldDocValues = in.getNumericDocValues(field);
		NumericDocValues oldDocValues = @in.getNumericDocValues(field);
		if (oldDocValues == null)
		{
			return null;
		}
		return new SortingNumericDocValues(oldDocValues, docMap);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.SortedDocValues getSortedDocValues(String field) throws java.io.IOException
	  public override SortedDocValues getSortedDocValues(string field)
	  {
		SortedDocValues sortedDV = @in.getSortedDocValues(field);
		if (sortedDV == null)
		{
		  return null;
		}
		else
		{
		  return new SortingSortedDocValues(sortedDV, docMap);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.SortedSetDocValues getSortedSetDocValues(String field) throws java.io.IOException
	  public override SortedSetDocValues getSortedSetDocValues(string field)
	  {
		SortedSetDocValues sortedSetDV = @in.getSortedSetDocValues(field);
		if (sortedSetDV == null)
		{
		  return null;
		}
		else
		{
		  return new SortingSortedSetDocValues(sortedSetDV, docMap);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.util.Bits getDocsWithField(String field) throws java.io.IOException
	  public override Bits getDocsWithField(string field)
	  {
		Bits bits = @in.getDocsWithField(field);
		if (bits == null || bits is Bits.MatchAllBits || bits is Bits.MatchNoBits)
		{
		  return bits;
		}
		else
		{
		  return new SortingBits(bits, docMap);
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.index.Fields getTermVectors(final int docID) throws java.io.IOException
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
	  public override Fields getTermVectors(int docID)
	  {
		return @in.getTermVectors(docMap.newToOld(docID));
	  }

	}

}