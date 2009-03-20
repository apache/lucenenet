/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Lucene.Net.Documents;
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using TokenStream = Lucene.Net.Analysis.TokenStream;

namespace Lucene.Net.Index
{
	
	/// <summary> Class responsible for access to stored document fields.
	/// <p/>
	/// It uses &lt;segment&gt;.fdt and &lt;segment&gt;.fdx; files.
	/// 
	/// </summary>
	/// <version>  $Id: FieldsReader.java 620759 2008-02-12 11:10:21Z mikemccand $
	/// </version>
	public sealed class FieldsReader
	{
		private FieldInfos fieldInfos;
		
		// The main fieldStream, used only for cloning.
		private IndexInput cloneableFieldsStream;
		
		// This is a clone of cloneableFieldsStream used for reading documents.
		// It should not be cloned outside of a synchronized context.
		private IndexInput fieldsStream;
		
		private IndexInput indexStream;
		private int numTotalDocs;
		private int size;
		private bool closed;
		
		// The docID offset where our docs begin in the index
		// file.  This will be 0 if we have our own private file.
		private int docStoreOffset;
		
		private System.LocalDataStoreSlot fieldsStreamTL = System.Threading.Thread.AllocateDataSlot();
		
		public FieldsReader(Directory d, System.String segment, FieldInfos fn) : this(d, segment, fn, BufferedIndexInput.BUFFER_SIZE, - 1, 0)
		{
		}
		
		internal FieldsReader(Directory d, System.String segment, FieldInfos fn, int readBufferSize) : this(d, segment, fn, readBufferSize, - 1, 0)
		{
		}
		
		internal FieldsReader(Directory d, System.String segment, FieldInfos fn, int readBufferSize, int docStoreOffset, int size)
		{
			bool success = false;
			
			try
			{
				fieldInfos = fn;
				
				cloneableFieldsStream = d.OpenInput(segment + ".fdt", readBufferSize);
				fieldsStream = (IndexInput) cloneableFieldsStream.Clone();
				indexStream = d.OpenInput(segment + ".fdx", readBufferSize);
				
				if (docStoreOffset != - 1)
				{
					// We read only a slice out of this shared fields file
					this.docStoreOffset = docStoreOffset;
					this.size = size;
					
					// Verify the file is long enough to hold all of our
					// docs
					System.Diagnostics.Debug.Assert(((int)(indexStream.Length() / 8)) >= size + this.docStoreOffset);
				}
				else
				{
					this.docStoreOffset = 0;
					this.size = (int) (indexStream.Length() >> 3);
				}
				
				numTotalDocs = (int) (indexStream.Length() >> 3);
				success = true;
			}
			finally
			{
				// With lock-less commits, it's entirely possible (and
				// fine) to hit a FileNotFound exception above. In
				// this case, we want to explicitly close any subset
				// of things that were opened so that we don't have to
				// wait for a GC to do so.
				if (!success)
				{
					Close();
				}
			}
		}
		
		/// <throws>  AlreadyClosedException if this FieldsReader is closed </throws>
		internal void  EnsureOpen()
		{
			if (closed)
			{
				throw new AlreadyClosedException("this FieldsReader is closed");
			}
		}
		
		/// <summary> Closes the underlying {@link Lucene.Net.Store.IndexInput} streams, including any ones associated with a
		/// lazy implementation of a Field.  This means that the Fields values will not be accessible.
		/// 
		/// </summary>
		/// <throws>  IOException </throws>
		public void  Close()
		{
			if (!closed)
			{
				if (fieldsStream != null)
				{
					fieldsStream.Close();
				}
				if (cloneableFieldsStream != null)
				{
					cloneableFieldsStream.Close();
				}
				if (indexStream != null)
				{
					indexStream.Close();
				}
				IndexInput localFieldsStream = (IndexInput) System.Threading.Thread.GetData(fieldsStreamTL);
				if (localFieldsStream != null)
				{
					localFieldsStream.Close();
					System.Threading.Thread.SetData(fieldsStreamTL, null);
				}
				closed = true;
			}
		}
		
		public int Size()
		{
			return size;
		}
		
		public Document Doc(int n, FieldSelector fieldSelector)
		{
			indexStream.Seek((n + docStoreOffset) * 8L);
			long position = indexStream.ReadLong();
			fieldsStream.Seek(position);
			
			Document doc = new Document();
			int numFields = fieldsStream.ReadVInt();
			for (int i = 0; i < numFields; i++)
			{
				int fieldNumber = fieldsStream.ReadVInt();
				FieldInfo fi = fieldInfos.FieldInfo(fieldNumber);
				FieldSelectorResult acceptField = fieldSelector == null ? FieldSelectorResult.LOAD : fieldSelector.Accept(fi.name);
				
				byte bits = fieldsStream.ReadByte();
				System.Diagnostics.Debug.Assert(bits <= FieldsWriter.FIELD_IS_COMPRESSED + FieldsWriter.FIELD_IS_TOKENIZED + FieldsWriter.FIELD_IS_BINARY);

				bool compressed = (bits & FieldsWriter.FIELD_IS_COMPRESSED) != 0;
				bool tokenize = (bits & FieldsWriter.FIELD_IS_TOKENIZED) != 0;
				bool binary = (bits & FieldsWriter.FIELD_IS_BINARY) != 0;
				//TODO: Find an alternative approach here if this list continues to grow beyond the
				//list of 5 or 6 currently here.  See Lucene 762 for discussion
				if (acceptField.Equals(FieldSelectorResult.LOAD))
				{
					AddField(doc, fi, binary, compressed, tokenize);
				}
				else if (acceptField.Equals(FieldSelectorResult.LOAD_FOR_MERGE))
				{
					AddFieldForMerge(doc, fi, binary, compressed, tokenize);
				}
				else if (acceptField.Equals(FieldSelectorResult.LOAD_AND_BREAK))
				{
					AddField(doc, fi, binary, compressed, tokenize);
					break; //Get out of this loop
				}
				else if (acceptField.Equals(FieldSelectorResult.LAZY_LOAD))
				{
					AddFieldLazy(doc, fi, binary, compressed, tokenize);
				}
				else if (acceptField.Equals(FieldSelectorResult.SIZE))
				{
					SkipField(binary, compressed, AddFieldSize(doc, fi, binary, compressed));
				}
				else if (acceptField.Equals(FieldSelectorResult.SIZE_AND_BREAK))
				{
					AddFieldSize(doc, fi, binary, compressed);
					break;
				}
				else
				{
					SkipField(binary, compressed);
				}
			}
			
			return doc;
		}
		
		/// <summary>Returns the length in bytes of each raw document in a
		/// contiguous range of length numDocs starting with
		/// startDocID.  Returns the IndexInput (the fieldStream),
		/// already seeked to the starting point for startDocID.
		/// </summary>
		internal IndexInput RawDocs(int[] lengths, int startDocID, int numDocs)
		{
			indexStream.Seek((docStoreOffset + startDocID) * 8L);
			long startOffset = indexStream.ReadLong();
			long lastOffset = startOffset;
			int count = 0;
			while (count < numDocs)
			{
				long offset;
				int docID = docStoreOffset + startDocID + count + 1;
				System.Diagnostics.Debug.Assert(docID <= numTotalDocs);
				if (docID < numTotalDocs)
					offset = indexStream.ReadLong();
				else
					offset = fieldsStream.Length();
				lengths[count++] = (int) (offset - lastOffset);
				lastOffset = offset;
			}
			
			fieldsStream.Seek(startOffset);
			
			return fieldsStream;
		}
		
		/// <summary> Skip the field.  We still have to read some of the information about the field, but can skip past the actual content.
		/// This will have the most payoff on large fields.
		/// </summary>
		private void  SkipField(bool binary, bool compressed)
		{
			SkipField(binary, compressed, fieldsStream.ReadVInt());
		}
		
		private void  SkipField(bool binary, bool compressed, int toRead)
		{
			if (binary || compressed)
			{
				long pointer = fieldsStream.GetFilePointer();
				fieldsStream.Seek(pointer + toRead);
			}
			else
			{
				//We need to skip chars.  This will slow us down, but still better
				fieldsStream.SkipChars(toRead);
			}
		}
		
		private void  AddFieldLazy(Document doc, FieldInfo fi, bool binary, bool compressed, bool tokenize)
		{
			if (binary == true)
			{
				int toRead = fieldsStream.ReadVInt();
				long pointer = fieldsStream.GetFilePointer();
				if (compressed)
				{
					//was: doc.add(new Fieldable(fi.name, uncompress(b), Fieldable.Store.COMPRESS));
					doc.Add(new LazyField(this, fi.name, Field.Store.COMPRESS, toRead, pointer));
				}
				else
				{
					//was: doc.add(new Fieldable(fi.name, b, Fieldable.Store.YES));
					doc.Add(new LazyField(this, fi.name, Field.Store.YES, toRead, pointer));
				}
				//Need to move the pointer ahead by toRead positions
				fieldsStream.Seek(pointer + toRead);
			}
			else
			{
				Field.Store store = Field.Store.YES;
				Field.Index index = GetIndexType(fi, tokenize);
				Field.TermVector termVector = GetTermVectorType(fi);
				
				Fieldable f;
				if (compressed)
				{
					store = Field.Store.COMPRESS;
					int toRead = fieldsStream.ReadVInt();
					long pointer = fieldsStream.GetFilePointer();
					f = new LazyField(this, fi.name, store, toRead, pointer);
					//skip over the part that we aren't loading
					fieldsStream.Seek(pointer + toRead);
					f.SetOmitNorms(fi.omitNorms);
				}
				else
				{
					int length = fieldsStream.ReadVInt();
					long pointer = fieldsStream.GetFilePointer();
					//Skip ahead of where we are by the length of what is stored
					fieldsStream.SkipChars(length);
					f = new LazyField(this, fi.name, store, index, termVector, length, pointer);
					f.SetOmitNorms(fi.omitNorms);
				}
				doc.Add(f);
			}
		}
		
		// in merge mode we don't uncompress the data of a compressed field
		private void  AddFieldForMerge(Document doc, FieldInfo fi, bool binary, bool compressed, bool tokenize)
		{
			System.Object data;
			
			if (binary || compressed)
			{
				int toRead = fieldsStream.ReadVInt();
				byte[] b = new byte[toRead];
				fieldsStream.ReadBytes(b, 0, b.Length);
				data = b;
			}
			else
			{
				data = fieldsStream.ReadString();
			}
			
			doc.Add(new FieldForMerge(data, fi, binary, compressed, tokenize));
		}
		
		private void  AddField(Document doc, FieldInfo fi, bool binary, bool compressed, bool tokenize)
		{
			
			//we have a binary stored field, and it may be compressed
			if (binary)
			{
				int toRead = fieldsStream.ReadVInt();
				byte[] b = new byte[toRead];
				fieldsStream.ReadBytes(b, 0, b.Length);
				if (compressed)
					doc.Add(new Field(fi.name, Uncompress(b), Field.Store.COMPRESS));
				else
					doc.Add(new Field(fi.name, b, Field.Store.YES));
			}
			else
			{
				Field.Store store = Field.Store.YES;
				Field.Index index = GetIndexType(fi, tokenize);
				Field.TermVector termVector = GetTermVectorType(fi);
				
				Fieldable f;
				if (compressed)
				{
					store = Field.Store.COMPRESS;
					int toRead = fieldsStream.ReadVInt();
					
					byte[] b = new byte[toRead];
					fieldsStream.ReadBytes(b, 0, b.Length);
					f = new Field(fi.name, System.Text.Encoding.GetEncoding("UTF-8").GetString(Uncompress(b)), store, index, termVector);
					f.SetOmitNorms(fi.omitNorms);
				}
				else
				{
					f = new Field(fi.name, fieldsStream.ReadString(), store, index, termVector);
					f.SetOmitNorms(fi.omitNorms);
				}
				doc.Add(f);
			}
		}
		
		// Add the size of field as a byte[] containing the 4 bytes of the integer byte size (high order byte first; char = 2 bytes)
		// Read just the size -- caller must skip the field content to continue reading fields
		// Return the size in bytes or chars, depending on field type
		private int AddFieldSize(Document doc, FieldInfo fi, bool binary, bool compressed)
		{
			int size = fieldsStream.ReadVInt(), bytesize = binary || compressed ? size : 2 * size;
			byte[] sizebytes = new byte[4];
			sizebytes[0] = (byte) (SupportClass.Number.URShift(bytesize, 24));
			sizebytes[1] = (byte) (SupportClass.Number.URShift(bytesize, 16));
			sizebytes[2] = (byte) (SupportClass.Number.URShift(bytesize, 8));
			sizebytes[3] = (byte) bytesize;
			doc.Add(new Field(fi.name, sizebytes, Field.Store.YES));
			return size;
		}
		
		private Field.TermVector GetTermVectorType(FieldInfo fi)
		{
			Field.TermVector termVector = null;
			if (fi.storeTermVector)
			{
				if (fi.storeOffsetWithTermVector)
				{
					if (fi.storePositionWithTermVector)
					{
						termVector = Field.TermVector.WITH_POSITIONS_OFFSETS;
					}
					else
					{
						termVector = Field.TermVector.WITH_OFFSETS;
					}
				}
				else if (fi.storePositionWithTermVector)
				{
					termVector = Field.TermVector.WITH_POSITIONS;
				}
				else
				{
					termVector = Field.TermVector.YES;
				}
			}
			else
			{
				termVector = Field.TermVector.NO;
			}
			return termVector;
		}
		
		private Field.Index GetIndexType(FieldInfo fi, bool tokenize)
		{
			Field.Index index;
			if (fi.isIndexed && tokenize)
				index = Field.Index.TOKENIZED;
			else if (fi.isIndexed && !tokenize)
				index = Field.Index.UN_TOKENIZED;
			else
				index = Field.Index.NO;
			return index;
		}
		
		/// <summary> A Lazy implementation of Fieldable that differs loading of fields until asked for, instead of when the Document is
		/// loaded.
		/// </summary>
		[Serializable]
		private class LazyField:AbstractField, Fieldable
		{
			private void  InitBlock(FieldsReader enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private FieldsReader enclosingInstance;
			public FieldsReader Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private int toRead;
			private long pointer;
			
			public LazyField(FieldsReader enclosingInstance, System.String name, Field.Store store, int toRead, long pointer):base(name, store, Field.Index.NO, Field.TermVector.NO)
			{
				InitBlock(enclosingInstance);
				this.toRead = toRead;
				this.pointer = pointer;
				lazy = true;
			}
			
			public LazyField(FieldsReader enclosingInstance, System.String name, Field.Store store, Field.Index index, Field.TermVector termVector, int toRead, long pointer):base(name, store, index, termVector)
			{
				InitBlock(enclosingInstance);
				this.toRead = toRead;
				this.pointer = pointer;
				lazy = true;
			}
			
			private IndexInput GetFieldStream()
			{
				IndexInput localFieldsStream = (IndexInput) System.Threading.Thread.GetData(Enclosing_Instance.fieldsStreamTL);
				if (localFieldsStream == null)
				{
					localFieldsStream = (IndexInput) Enclosing_Instance.cloneableFieldsStream.Clone();
					System.Threading.Thread.SetData(Enclosing_Instance.fieldsStreamTL, localFieldsStream);
				}
				return localFieldsStream;
			}
			
			/// <summary>The value of the field in Binary, or null.  If null, the Reader value,
			/// String value, or TokenStream value is used. Exactly one of stringValue(), 
			/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
			/// </summary>
			public override byte[] BinaryValue()
			{
				Enclosing_Instance.EnsureOpen();
				if (fieldsData == null)
				{
					byte[] b = new byte[toRead];
					IndexInput localFieldsStream = GetFieldStream();
					//Throw this IO Exception since IndexREader.document does so anyway, so probably not that big of a change for people
					//since they are already handling this exception when getting the document
					try
					{
						localFieldsStream.Seek(pointer);
						localFieldsStream.ReadBytes(b, 0, b.Length);
						if (isCompressed == true)
						{
							fieldsData = Enclosing_Instance.Uncompress(b);
						}
						else
						{
							fieldsData = b;
						}
					}
					catch (System.IO.IOException e)
					{
						throw new FieldReaderException(e);
					}
				}
				return fieldsData is byte[] ? (byte[]) fieldsData : null;
			}
			
			/// <summary>The value of the field as a Reader, or null.  If null, the String value,
			/// binary value, or TokenStream value is used.  Exactly one of stringValue(), 
			/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
			/// </summary>
			public override System.IO.TextReader ReaderValue()
			{
				Enclosing_Instance.EnsureOpen();
				return fieldsData is System.IO.TextReader ? (System.IO.TextReader) fieldsData : null;
			}
			
			/// <summary>The value of the field as a TokesStream, or null.  If null, the Reader value,
			/// String value, or binary value is used. Exactly one of stringValue(), 
			/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
			/// </summary>
			public override TokenStream TokenStreamValue()
			{
				Enclosing_Instance.EnsureOpen();
				return fieldsData is TokenStream ? (TokenStream) fieldsData : null;
			}
			
			
			/// <summary>The value of the field as a String, or null.  If null, the Reader value,
			/// binary value, or TokenStream value is used.  Exactly one of stringValue(), 
			/// readerValue(), binaryValue(), and tokenStreamValue() must be set. 
			/// </summary>
			public override System.String StringValue()
			{
				Enclosing_Instance.EnsureOpen();
				if (fieldsData == null)
				{
					IndexInput localFieldsStream = GetFieldStream();
					try
					{
						localFieldsStream.Seek(pointer);
						if (isCompressed)
						{
							byte[] b = new byte[toRead];
							localFieldsStream.ReadBytes(b, 0, b.Length);
							fieldsData = System.Text.Encoding.GetEncoding("UTF-8").GetString(Enclosing_Instance.Uncompress(b));
						}
						else
						{
							//read in chars b/c we already know the length we need to read
							char[] chars = new char[toRead];
							localFieldsStream.ReadChars(chars, 0, toRead);
							fieldsData = new System.String(chars);
						}
					}
					catch (System.IO.IOException e)
					{
						throw new FieldReaderException(e);
					}
				}
				return fieldsData is System.String ? (System.String) fieldsData : null;
			}
			
			public long GetPointer()
			{
				Enclosing_Instance.EnsureOpen();
				return pointer;
			}
			
			public void  SetPointer(long pointer)
			{
				Enclosing_Instance.EnsureOpen();
				this.pointer = pointer;
			}
			
			public int GetToRead()
			{
				Enclosing_Instance.EnsureOpen();
				return toRead;
			}
			
			public void  SetToRead(int toRead)
			{
				Enclosing_Instance.EnsureOpen();
				this.toRead = toRead;
			}
		}
		
		private byte[] Uncompress(byte[] input)
		{
			return SupportClass.CompressionSupport.Uncompress(input);
        }
		
		// Instances of this class hold field properties and data
		// for merge
		[Serializable]
		public sealed class FieldForMerge : AbstractField
		{
			public override System.String StringValue()
			{
				return (System.String) this.fieldsData;
			}
			
			public override System.IO.TextReader ReaderValue()
			{
				// not needed for merge
				return null;
			}
			
			public override byte[] BinaryValue()
			{
				return (byte[]) this.fieldsData;
			}
			
			public override TokenStream TokenStreamValue()
			{
				// not needed for merge
				return null;
			}
			
			public FieldForMerge(System.Object value_Renamed, FieldInfo fi, bool binary, bool compressed, bool tokenize)
			{
				this.isStored = true;
				this.fieldsData = value_Renamed;
				this.isCompressed = compressed;
				this.isBinary = binary;
				this.isTokenized = tokenize;
				
				this.name = String.Intern(fi.name);
				this.isIndexed = fi.isIndexed;
				this.omitNorms = fi.omitNorms;
				this.storeOffsetWithTermVector = fi.storeOffsetWithTermVector;
				this.storePositionWithTermVector = fi.storePositionWithTermVector;
				this.storeTermVector = fi.storeTermVector;
			}
		}
	}
}
