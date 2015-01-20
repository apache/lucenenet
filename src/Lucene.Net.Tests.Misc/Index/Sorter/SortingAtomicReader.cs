/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Index.Sorter;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Store;
using Org.Apache.Lucene.Util;
using Org.Apache.Lucene.Util.Automaton;
using Sharpen;

namespace Org.Apache.Lucene.Index.Sorter
{
	/// <summary>
	/// An
	/// <see cref="Org.Apache.Lucene.Index.AtomicReader">Org.Apache.Lucene.Index.AtomicReader
	/// 	</see>
	/// which supports sorting documents by a given
	/// <see cref="Org.Apache.Lucene.Search.Sort">Org.Apache.Lucene.Search.Sort</see>
	/// . You can use this class to sort an index as follows:
	/// <pre class="prettyprint">
	/// IndexWriter writer; // writer to which the sorted index will be added
	/// DirectoryReader reader; // reader on the input index
	/// Sort sort; // determines how the documents are sorted
	/// AtomicReader sortingReader = SortingAtomicReader.wrap(SlowCompositeReaderWrapper.wrap(reader), sort);
	/// writer.addIndexes(reader);
	/// writer.close();
	/// reader.close();
	/// </pre>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class SortingAtomicReader : FilterAtomicReader
	{
		private class SortingFields : FilterAtomicReader.FilterFields
		{
			private readonly Sorter.DocMap docMap;

			private readonly FieldInfos infos;

			public SortingFields(Fields @in, FieldInfos infos, Sorter.DocMap docMap) : base(@in
				)
			{
				this.docMap = docMap;
				this.infos = infos;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override Org.Apache.Lucene.Index.Terms Terms(string field)
			{
				Org.Apache.Lucene.Index.Terms terms = @in.Terms(field);
				if (terms == null)
				{
					return null;
				}
				else
				{
					return new SortingAtomicReader.SortingTerms(terms, infos.FieldInfo(field).GetIndexOptions
						(), docMap);
				}
			}
		}

		private class SortingTerms : FilterAtomicReader.FilterTerms
		{
			private readonly Sorter.DocMap docMap;

			private readonly FieldInfo.IndexOptions indexOptions;

			public SortingTerms(Terms @in, FieldInfo.IndexOptions indexOptions, Sorter.DocMap
				 docMap) : base(@in)
			{
				this.docMap = docMap;
				this.indexOptions = indexOptions;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Iterator(TermsEnum reuse)
			{
				return new SortingAtomicReader.SortingTermsEnum(@in.Iterator(reuse), docMap, indexOptions
					);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override TermsEnum Intersect(CompiledAutomaton compiled, BytesRef startTerm
				)
			{
				return new SortingAtomicReader.SortingTermsEnum(@in.Intersect(compiled, startTerm
					), docMap, indexOptions);
			}
		}

		private class SortingTermsEnum : FilterAtomicReader.FilterTermsEnum
		{
			internal readonly Sorter.DocMap docMap;

			private readonly FieldInfo.IndexOptions indexOptions;

			public SortingTermsEnum(TermsEnum @in, Sorter.DocMap docMap, FieldInfo.IndexOptions
				 indexOptions) : base(@in)
			{
				// pkg-protected to avoid synthetic accessor methods
				this.docMap = docMap;
				this.indexOptions = indexOptions;
			}

			internal virtual Bits NewToOld(Bits liveDocs)
			{
				if (liveDocs == null)
				{
					return null;
				}
				return new _Bits_130(this, liveDocs);
			}

			private sealed class _Bits_130 : Bits
			{
				public _Bits_130(SortingTermsEnum _enclosing, Bits liveDocs)
				{
					this._enclosing = _enclosing;
					this.liveDocs = liveDocs;
				}

				public override bool Get(int index)
				{
					return liveDocs.Get(this._enclosing.docMap.OldToNew(index));
				}

				public override int Length()
				{
					return liveDocs.Length();
				}

				private readonly SortingTermsEnum _enclosing;

				private readonly Bits liveDocs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsEnum Docs(Bits liveDocs, DocsEnum reuse, int flags)
			{
				DocsEnum inReuse;
				SortingAtomicReader.SortingDocsEnum wrapReuse;
				if (reuse != null && reuse is SortingAtomicReader.SortingDocsEnum)
				{
					// if we're asked to reuse the given DocsEnum and it is Sorting, return
					// the wrapped one, since some Codecs expect it.
					wrapReuse = (SortingAtomicReader.SortingDocsEnum)reuse;
					inReuse = wrapReuse.GetWrapped();
				}
				else
				{
					wrapReuse = null;
					inReuse = reuse;
				}
				DocsEnum inDocs = @in.Docs(NewToOld(liveDocs), inReuse, flags);
				bool withFreqs = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS) >=
					 0 && (flags & DocsEnum.FLAG_FREQS) != 0;
				return new SortingAtomicReader.SortingDocsEnum(docMap.Size(), wrapReuse, inDocs, 
					withFreqs, docMap);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override DocsAndPositionsEnum DocsAndPositions(Bits liveDocs, DocsAndPositionsEnum
				 reuse, int flags)
			{
				DocsAndPositionsEnum inReuse;
				SortingAtomicReader.SortingDocsAndPositionsEnum wrapReuse;
				if (reuse != null && reuse is SortingAtomicReader.SortingDocsAndPositionsEnum)
				{
					// if we're asked to reuse the given DocsEnum and it is Sorting, return
					// the wrapped one, since some Codecs expect it.
					wrapReuse = (SortingAtomicReader.SortingDocsAndPositionsEnum)reuse;
					inReuse = wrapReuse.GetWrapped();
				}
				else
				{
					wrapReuse = null;
					inReuse = reuse;
				}
				DocsAndPositionsEnum inDocsAndPositions = @in.DocsAndPositions(NewToOld(liveDocs)
					, inReuse, flags);
				if (inDocsAndPositions == null)
				{
					return null;
				}
				// we ignore the fact that offsets may be stored but not asked for,
				// since this code is expected to be used during addIndexes which will
				// ask for everything. if that assumption changes in the future, we can
				// factor in whether 'flags' says offsets are not required.
				bool storeOffsets = indexOptions.CompareTo(FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
					) >= 0;
				return new SortingAtomicReader.SortingDocsAndPositionsEnum(docMap.Size(), wrapReuse
					, inDocsAndPositions, docMap, storeOffsets);
			}
		}

		private class SortingBinaryDocValues : BinaryDocValues
		{
			private readonly BinaryDocValues @in;

			private readonly Sorter.DocMap docMap;

			internal SortingBinaryDocValues(BinaryDocValues @in, Sorter.DocMap docMap)
			{
				this.@in = @in;
				this.docMap = docMap;
			}

			public override void Get(int docID, BytesRef result)
			{
				@in.Get(docMap.NewToOld(docID), result);
			}
		}

		private class SortingNumericDocValues : NumericDocValues
		{
			private readonly NumericDocValues @in;

			private readonly Sorter.DocMap docMap;

			public SortingNumericDocValues(NumericDocValues @in, Sorter.DocMap docMap)
			{
				this.@in = @in;
				this.docMap = docMap;
			}

			public override long Get(int docID)
			{
				return @in.Get(docMap.NewToOld(docID));
			}
		}

		private class SortingBits : Bits
		{
			private readonly Bits @in;

			private readonly Sorter.DocMap docMap;

			public SortingBits(Bits @in, Sorter.DocMap docMap)
			{
				this.@in = @in;
				this.docMap = docMap;
			}

			public override bool Get(int index)
			{
				return @in.Get(docMap.NewToOld(index));
			}

			public override int Length()
			{
				return @in.Length();
			}
		}

		private class SortingSortedDocValues : SortedDocValues
		{
			private readonly SortedDocValues @in;

			private readonly Sorter.DocMap docMap;

			internal SortingSortedDocValues(SortedDocValues @in, Sorter.DocMap docMap)
			{
				this.@in = @in;
				this.docMap = docMap;
			}

			public override int GetOrd(int docID)
			{
				return @in.GetOrd(docMap.NewToOld(docID));
			}

			public override void LookupOrd(int ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			public override int GetValueCount()
			{
				return @in.GetValueCount();
			}

			public override void Get(int docID, BytesRef result)
			{
				@in.Get(docMap.NewToOld(docID), result);
			}

			public override int LookupTerm(BytesRef key)
			{
				return @in.LookupTerm(key);
			}
		}

		private class SortingSortedSetDocValues : SortedSetDocValues
		{
			private readonly SortedSetDocValues @in;

			private readonly Sorter.DocMap docMap;

			internal SortingSortedSetDocValues(SortedSetDocValues @in, Sorter.DocMap docMap)
			{
				this.@in = @in;
				this.docMap = docMap;
			}

			public override long NextOrd()
			{
				return @in.NextOrd();
			}

			public override void SetDocument(int docID)
			{
				@in.SetDocument(docMap.NewToOld(docID));
			}

			public override void LookupOrd(long ord, BytesRef result)
			{
				@in.LookupOrd(ord, result);
			}

			public override long GetValueCount()
			{
				return @in.GetValueCount();
			}

			public override long LookupTerm(BytesRef key)
			{
				return @in.LookupTerm(key);
			}
		}

		internal class SortingDocsEnum : FilterAtomicReader.FilterDocsEnum
		{
			private sealed class DocFreqSorter : TimSorter
			{
				private int[] docs;

				private int[] freqs;

				private readonly int[] tmpDocs;

				private int[] tmpFreqs;

				protected DocFreqSorter(int maxDoc) : base(maxDoc / 64)
				{
					this.tmpDocs = new int[maxDoc / 64];
				}

				public void Reset(int[] docs, int[] freqs)
				{
					this.docs = docs;
					this.freqs = freqs;
					if (freqs != null && tmpFreqs == null)
					{
						tmpFreqs = new int[tmpDocs.Length];
					}
				}

				protected override int Compare(int i, int j)
				{
					return docs[i] - docs[j];
				}

				protected override void Swap(int i, int j)
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

				protected override void Copy(int src, int dest)
				{
					docs[dest] = docs[src];
					if (freqs != null)
					{
						freqs[dest] = freqs[src];
					}
				}

				protected override void Save(int i, int len)
				{
					System.Array.Copy(docs, i, tmpDocs, 0, len);
					if (freqs != null)
					{
						System.Array.Copy(freqs, i, tmpFreqs, 0, len);
					}
				}

				protected override void Restore(int i, int j)
				{
					docs[j] = tmpDocs[i];
					if (freqs != null)
					{
						freqs[j] = tmpFreqs[i];
					}
				}

				protected override int CompareSaved(int i, int j)
				{
					return tmpDocs[i] - docs[j];
				}
			}

			private readonly int maxDoc;

			private readonly SortingAtomicReader.SortingDocsEnum.DocFreqSorter sorter;

			private int[] docs;

			private int[] freqs;

			private int docIt = -1;

			private readonly int upto;

			private readonly bool withFreqs;

			/// <exception cref="System.IO.IOException"></exception>
			internal SortingDocsEnum(int maxDoc, SortingAtomicReader.SortingDocsEnum reuse, DocsEnum
				 @in, bool withFreqs, Sorter.DocMap docMap) : base(@in)
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
						sorter = new SortingAtomicReader.SortingDocsEnum.DocFreqSorter(maxDoc);
					}
					docs = reuse.docs;
					freqs = reuse.freqs;
				}
				else
				{
					// maybe null
					docs = new int[64];
					sorter = new SortingAtomicReader.SortingDocsEnum.DocFreqSorter(maxDoc);
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
					while ((doc = @in.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
					{
						if (i >= docs.Length)
						{
							docs = ArrayUtil.Grow(docs, docs.Length + 1);
							freqs = ArrayUtil.Grow(freqs, freqs.Length + 1);
						}
						docs[i] = docMap.OldToNew(doc);
						freqs[i] = @in.Freq();
						++i;
					}
				}
				else
				{
					freqs = null;
					while ((doc = @in.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
					{
						if (i >= docs.Length)
						{
							docs = ArrayUtil.Grow(docs, docs.Length + 1);
						}
						docs[i++] = docMap.OldToNew(doc);
					}
				}
				// TimSort can save much time compared to other sorts in case of
				// reverse sorting, or when sorting a concatenation of sorted readers
				sorter.Reset(docs, freqs);
				sorter.Sort(0, i);
				upto = i;
			}

			// for testing
			internal virtual bool Reused(DocsEnum other)
			{
				if (other == null || !(other is SortingAtomicReader.SortingDocsEnum))
				{
					return false;
				}
				return docs == ((SortingAtomicReader.SortingDocsEnum)other).docs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// need to support it for checkIndex, but in practice it won't be called, so
				// don't bother to implement efficiently for now.
				return SlowAdvance(target);
			}

			public override int DocID()
			{
				return docIt < 0 ? -1 : docIt >= upto ? NO_MORE_DOCS : docs[docIt];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return withFreqs && docIt < upto ? freqs[docIt] : 1;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				if (++docIt >= upto)
				{
					return NO_MORE_DOCS;
				}
				return docs[docIt];
			}

			/// <summary>
			/// Returns the wrapped
			/// <see cref="Org.Apache.Lucene.Index.DocsEnum">Org.Apache.Lucene.Index.DocsEnum</see>
			/// .
			/// </summary>
			internal virtual DocsEnum GetWrapped()
			{
				return @in;
			}
		}

		internal class SortingDocsAndPositionsEnum : FilterAtomicReader.FilterDocsAndPositionsEnum
		{
			/// <summary>
			/// A
			/// <see cref="Org.Apache.Lucene.Util.TimSorter">Org.Apache.Lucene.Util.TimSorter</see>
			/// which sorts two parallel arrays of doc IDs and
			/// offsets in one go. Everytime a doc ID is 'swapped', its correponding offset
			/// is swapped too.
			/// </summary>
			private sealed class DocOffsetSorter : TimSorter
			{
				private int[] docs;

				private long[] offsets;

				private readonly int[] tmpDocs;

				private readonly long[] tmpOffsets;

				protected DocOffsetSorter(int maxDoc) : base(maxDoc / 64)
				{
					this.tmpDocs = new int[maxDoc / 64];
					this.tmpOffsets = new long[maxDoc / 64];
				}

				public void Reset(int[] docs, long[] offsets)
				{
					this.docs = docs;
					this.offsets = offsets;
				}

				protected override int Compare(int i, int j)
				{
					return docs[i] - docs[j];
				}

				protected override void Swap(int i, int j)
				{
					int tmpDoc = docs[i];
					docs[i] = docs[j];
					docs[j] = tmpDoc;
					long tmpOffset = offsets[i];
					offsets[i] = offsets[j];
					offsets[j] = tmpOffset;
				}

				protected override void Copy(int src, int dest)
				{
					docs[dest] = docs[src];
					offsets[dest] = offsets[src];
				}

				protected override void Save(int i, int len)
				{
					System.Array.Copy(docs, i, tmpDocs, 0, len);
					System.Array.Copy(offsets, i, tmpOffsets, 0, len);
				}

				protected override void Restore(int i, int j)
				{
					docs[j] = tmpDocs[i];
					offsets[j] = tmpOffsets[i];
				}

				protected override int CompareSaved(int i, int j)
				{
					return tmpDocs[i] - docs[j];
				}
			}

			private readonly int maxDoc;

			private readonly SortingAtomicReader.SortingDocsAndPositionsEnum.DocOffsetSorter 
				sorter;

			private int[] docs;

			private long[] offsets;

			private readonly int upto;

			private readonly IndexInput postingInput;

			private readonly bool storeOffsets;

			private int docIt = -1;

			private int pos;

			private int startOffset = -1;

			private int endOffset = -1;

			private readonly BytesRef payload;

			private int currFreq;

			private readonly RAMFile file;

			/// <exception cref="System.IO.IOException"></exception>
			internal SortingDocsAndPositionsEnum(int maxDoc, SortingAtomicReader.SortingDocsAndPositionsEnum
				 reuse, DocsAndPositionsEnum @in, Sorter.DocMap docMap, bool storeOffsets) : base
				(@in)
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
						sorter = new SortingAtomicReader.SortingDocsAndPositionsEnum.DocOffsetSorter(maxDoc
							);
					}
				}
				else
				{
					docs = new int[32];
					offsets = new long[32];
					payload = new BytesRef(32);
					file = new RAMFile();
					sorter = new SortingAtomicReader.SortingDocsAndPositionsEnum.DocOffsetSorter(maxDoc
						);
				}
				IndexOutput @out = new RAMOutputStream(file);
				int doc;
				int i = 0;
				while ((doc = @in.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
				{
					if (i == docs.Length)
					{
						int newLength = ArrayUtil.Oversize(i + 1, 4);
						docs = Arrays.CopyOf(docs, newLength);
						offsets = Arrays.CopyOf(offsets, newLength);
					}
					docs[i] = docMap.OldToNew(doc);
					offsets[i] = @out.GetFilePointer();
					AddPositions(@in, @out);
					i++;
				}
				upto = i;
				sorter.Reset(docs, offsets);
				sorter.Sort(0, upto);
				@out.Close();
				this.postingInput = new RAMInputStream(string.Empty, file);
			}

			// for testing
			internal virtual bool Reused(DocsAndPositionsEnum other)
			{
				if (other == null || !(other is SortingAtomicReader.SortingDocsAndPositionsEnum))
				{
					return false;
				}
				return docs == ((SortingAtomicReader.SortingDocsAndPositionsEnum)other).docs;
			}

			/// <exception cref="System.IO.IOException"></exception>
			private void AddPositions(DocsAndPositionsEnum @in, IndexOutput @out)
			{
				int freq = @in.Freq();
				@out.WriteVInt(freq);
				int previousPosition = 0;
				int previousEndOffset = 0;
				for (int i = 0; i < freq; i++)
				{
					int pos = @in.NextPosition();
					BytesRef payload = @in.GetPayload();
					// The low-order bit of token is set only if there is a payload, the
					// previous bits are the delta-encoded position. 
					int token = (pos - previousPosition) << 1 | (payload == null ? 0 : 1);
					@out.WriteVInt(token);
					previousPosition = pos;
					if (storeOffsets)
					{
						// don't encode offsets if they are not stored
						int startOffset = @in.StartOffset();
						int endOffset = @in.EndOffset();
						@out.WriteVInt(startOffset - previousEndOffset);
						@out.WriteVInt(endOffset - startOffset);
						previousEndOffset = endOffset;
					}
					if (payload != null)
					{
						@out.WriteVInt(payload.length);
						@out.WriteBytes(payload.bytes, payload.offset, payload.length);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				// need to support it for checkIndex, but in practice it won't be called, so
				// don't bother to implement efficiently for now.
				return SlowAdvance(target);
			}

			public override int DocID()
			{
				return docIt < 0 ? -1 : docIt >= upto ? NO_MORE_DOCS : docs[docIt];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int EndOffset()
			{
				return endOffset;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return currFreq;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef GetPayload()
			{
				return payload.length == 0 ? null : payload;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				if (++docIt >= upto)
				{
					return DocIdSetIterator.NO_MORE_DOCS;
				}
				postingInput.Seek(offsets[docIt]);
				currFreq = postingInput.ReadVInt();
				// reset variables used in nextPosition
				pos = 0;
				endOffset = 0;
				return docs[docIt];
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextPosition()
			{
				int token = postingInput.ReadVInt();
				pos += (int)(((uint)token) >> 1);
				if (storeOffsets)
				{
					startOffset = endOffset + postingInput.ReadVInt();
					endOffset = startOffset + postingInput.ReadVInt();
				}
				if ((token & 1) != 0)
				{
					payload.offset = 0;
					payload.length = postingInput.ReadVInt();
					if (payload.length > payload.bytes.Length)
					{
						payload.bytes = new byte[ArrayUtil.Oversize(payload.length, 1)];
					}
					postingInput.ReadBytes(payload.bytes, 0, payload.length);
				}
				else
				{
					payload.length = 0;
				}
				return pos;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int StartOffset()
			{
				return startOffset;
			}

			/// <summary>
			/// Returns the wrapped
			/// <see cref="Org.Apache.Lucene.Index.DocsAndPositionsEnum">Org.Apache.Lucene.Index.DocsAndPositionsEnum
			/// 	</see>
			/// .
			/// </summary>
			internal virtual DocsAndPositionsEnum GetWrapped()
			{
				return @in;
			}
		}

		/// <summary>
		/// Return a sorted view of <code>reader</code> according to the order
		/// defined by <code>sort</code>.
		/// </summary>
		/// <remarks>
		/// Return a sorted view of <code>reader</code> according to the order
		/// defined by <code>sort</code>. If the reader is already sorted, this
		/// method might return the reader as-is.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static AtomicReader Wrap(AtomicReader reader, Sort sort)
		{
			return Wrap(reader, new Org.Apache.Lucene.Index.Sorter.Sorter(sort).Sort(reader));
		}

		/// <summary>
		/// Expert: same as
		/// <see cref="Wrap(Org.Apache.Lucene.Index.AtomicReader, Org.Apache.Lucene.Search.Sort)
		/// 	">Wrap(Org.Apache.Lucene.Index.AtomicReader, Org.Apache.Lucene.Search.Sort)</see>
		/// but operates directly on a
		/// <see cref="DocMap">DocMap</see>
		/// .
		/// </summary>
		internal static AtomicReader Wrap(AtomicReader reader, Sorter.DocMap docMap)
		{
			if (docMap == null)
			{
				// the reader is already sorter
				return reader;
			}
			if (reader.MaxDoc() != docMap.Size())
			{
				throw new ArgumentException("reader.maxDoc() should be equal to docMap.size(), got"
					 + reader.MaxDoc() + " != " + docMap.Size());
			}
			return new SortingAtomicReader(Org.Apache.Lucene.Index.Sorter.Sorter.IsConsistent
				(docMap), docMap);
		}

		internal readonly Sorter.DocMap docMap;

		private SortingAtomicReader(AtomicReader @in, Sorter.DocMap docMap) : base(@in)
		{
			// pkg-protected to avoid synthetic accessor methods
			this.docMap = docMap;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Document(int docID, StoredFieldVisitor visitor)
		{
			@in.Document(docMap.NewToOld(docID), visitor);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Fields Fields()
		{
			Fields fields = @in.Fields();
			if (fields == null)
			{
				return null;
			}
			else
			{
				return new SortingAtomicReader.SortingFields(fields, @in.GetFieldInfos(), docMap);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override BinaryDocValues GetBinaryDocValues(string field)
		{
			BinaryDocValues oldDocValues = @in.GetBinaryDocValues(field);
			if (oldDocValues == null)
			{
				return null;
			}
			else
			{
				return new SortingAtomicReader.SortingBinaryDocValues(oldDocValues, docMap);
			}
		}

		public override Bits GetLiveDocs()
		{
			Bits inLiveDocs = @in.GetLiveDocs();
			if (inLiveDocs == null)
			{
				return null;
			}
			else
			{
				return new SortingAtomicReader.SortingBits(inLiveDocs, docMap);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNormValues(string field)
		{
			NumericDocValues norm = @in.GetNormValues(field);
			if (norm == null)
			{
				return null;
			}
			else
			{
				return new SortingAtomicReader.SortingNumericDocValues(norm, docMap);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override NumericDocValues GetNumericDocValues(string field)
		{
			NumericDocValues oldDocValues = @in.GetNumericDocValues(field);
			if (oldDocValues == null)
			{
				return null;
			}
			return new SortingAtomicReader.SortingNumericDocValues(oldDocValues, docMap);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedDocValues GetSortedDocValues(string field)
		{
			SortedDocValues sortedDV = @in.GetSortedDocValues(field);
			if (sortedDV == null)
			{
				return null;
			}
			else
			{
				return new SortingAtomicReader.SortingSortedDocValues(sortedDV, docMap);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override SortedSetDocValues GetSortedSetDocValues(string field)
		{
			SortedSetDocValues sortedSetDV = @in.GetSortedSetDocValues(field);
			if (sortedSetDV == null)
			{
				return null;
			}
			else
			{
				return new SortingAtomicReader.SortingSortedSetDocValues(sortedSetDV, docMap);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Bits GetDocsWithField(string field)
		{
			Bits bits = @in.GetDocsWithField(field);
			if (bits == null || bits is Bits.MatchAllBits || bits is Bits.MatchNoBits)
			{
				return bits;
			}
			else
			{
				return new SortingAtomicReader.SortingBits(bits, docMap);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override Fields GetTermVectors(int docID)
		{
			return @in.GetTermVectors(docMap.NewToOld(docID));
		}
	}
}
