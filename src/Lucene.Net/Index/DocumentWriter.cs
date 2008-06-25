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
// {{Aroush-2.3.1}} remove this file from SVN
/*
using System;
using Analyzer = Lucene.Net.Analysis.Analyzer;
using Token = Lucene.Net.Analysis.Token;
using TokenStream = Lucene.Net.Analysis.TokenStream;
using Document = Lucene.Net.Documents.Document;
using Fieldable = Lucene.Net.Documents.Fieldable;
using Similarity = Lucene.Net.Search.Similarity;
using Directory = Lucene.Net.Store.Directory;
using IndexOutput = Lucene.Net.Store.IndexOutput;

namespace Lucene.Net.Index
{
	
	public sealed class DocumentWriter
	{
		private void  InitBlock()
		{
			termIndexInterval = IndexWriter.DEFAULT_TERM_INDEX_INTERVAL;
		}
		private Analyzer analyzer;
		private Directory directory;
		private Similarity similarity;
		private FieldInfos fieldInfos;
		private int maxFieldLength;
		private int termIndexInterval;
		private System.IO.TextWriter infoStream;
		
		/// <summary>This ctor used by test code only.
		/// 
		/// </summary>
		/// <param name="directory">The directory to write the document information to
		/// </param>
		/// <param name="analyzer">The analyzer to use for the document
		/// </param>
		/// <param name="similarity">The Similarity function
		/// </param>
		/// <param name="maxFieldLength">The maximum number of tokens a field may have
		/// </param>
		public DocumentWriter(Directory directory, Analyzer analyzer, Similarity similarity, int maxFieldLength)
		{
			InitBlock();
			this.directory = directory;
			this.analyzer = analyzer;
			this.similarity = similarity;
			this.maxFieldLength = maxFieldLength;
		}
		
		public DocumentWriter(Directory directory, Analyzer analyzer, IndexWriter writer)
		{
			InitBlock();
			this.directory = directory;
			this.analyzer = analyzer;
			this.similarity = writer.GetSimilarity();
			this.maxFieldLength = writer.GetMaxFieldLength();
			this.termIndexInterval = writer.GetTermIndexInterval();
		}
		
		public void  AddDocument(System.String segment, Document doc)
		{
			// write field names
			fieldInfos = new FieldInfos();
			fieldInfos.Add(doc);
			fieldInfos.Write(directory, segment + ".fnm");
			
			// write field values
			FieldsWriter fieldsWriter = new FieldsWriter(directory, segment, fieldInfos);
			try
			{
				fieldsWriter.AddDocument(doc);
			}
			finally
			{
				fieldsWriter.Close();
			}
			
			// invert doc into postingTable
			postingTable.Clear(); // clear postingTable
			fieldLengths = new int[fieldInfos.Size()]; // init fieldLengths
			fieldPositions = new int[fieldInfos.Size()]; // init fieldPositions
			fieldOffsets = new int[fieldInfos.Size()]; // init fieldOffsets
			
			fieldBoosts = new float[fieldInfos.Size()]; // init fieldBoosts
			float boost = doc.GetBoost();
			for (int i = 0; i < fieldBoosts.Length; i++)
			{
				fieldBoosts[i] = boost;
			}
			
			InvertDocument(doc);
			
			// sort postingTable into an array
			Posting[] postings = SortPostingTable();
			
			/*
			for (int i = 0; i < postings.length; i++) {
			Posting posting = postings[i];
			System.out.print(posting.term);
			System.out.print(" freq=" + posting.freq);
			System.out.print(" pos=");
			System.out.print(posting.positions[0]);
			for (int j = 1; j < posting.freq; j++)
			System.out.print("," + posting.positions[j]);
			System.out.println("");
			}
			*/
			
			// write postings
			WritePostings(postings, segment);
			
			// write norms of indexed fields
			WriteNorms(segment);
		}
		
		// Keys are Terms, values are Postings.
		// Used to buffer a document before it is written to the index.
		private System.Collections.Hashtable postingTable = System.Collections.Hashtable.Synchronized(new System.Collections.Hashtable());
		private int[] fieldLengths;
		private int[] fieldPositions;
		private int[] fieldOffsets;
		private float[] fieldBoosts;
		
		// Tokenizes the fields of a document into Postings.
		private void  InvertDocument(Document doc)
		{
			System.Collections.IEnumerator fieldIterator = doc.GetFields().GetEnumerator();
			while (fieldIterator.MoveNext())
			{
				Fieldable field = (Fieldable) fieldIterator.Current;
				System.String fieldName = field.Name();
				int fieldNumber = fieldInfos.FieldNumber(fieldName);
				
				int length = fieldLengths[fieldNumber]; // length of field
				int position = fieldPositions[fieldNumber]; // position in field
				if (length > 0)
					position += analyzer.GetPositionIncrementGap(fieldName);
				int offset = fieldOffsets[fieldNumber]; // offset field
				
				if (field.IsIndexed())
				{
					if (!field.IsTokenized())
					{
						// un-tokenized field
						System.String stringValue = field.StringValue();
						if (field.IsStoreOffsetWithTermVector())
							AddPosition(fieldName, stringValue, position++, new TermVectorOffsetInfo(offset, offset + stringValue.Length));
						else
							AddPosition(fieldName, stringValue, position++, null);
						offset += stringValue.Length;
						length++;
					}
					else
					{
						System.IO.TextReader reader; // find or make Reader
						if (field.ReaderValue() != null)
							reader = field.ReaderValue();
						else if (field.StringValue() != null)
							reader = new System.IO.StringReader(field.StringValue());
						else
							throw new System.ArgumentException("field must have either String or Reader value");
						
						// Tokenize field and add to postingTable
						TokenStream stream = analyzer.TokenStream(fieldName, reader);
						try
						{
							Token lastToken = null;
							for (Token t = stream.Next(); t != null; t = stream.Next())
							{
								position += (t.GetPositionIncrement() - 1);
								
								if (field.IsStoreOffsetWithTermVector())
									AddPosition(fieldName, t.TermText(), position++, new TermVectorOffsetInfo(offset + t.StartOffset(), offset + t.EndOffset()));
								else
									AddPosition(fieldName, t.TermText(), position++, null);
								
								lastToken = t;
								if (++length >= maxFieldLength)
								{
									if (infoStream != null)
										infoStream.WriteLine("maxFieldLength " + maxFieldLength + " reached, ignoring following tokens");
									break;
								}
							}
							
							if (lastToken != null)
								offset += lastToken.EndOffset() + 1;
						}
						finally
						{
							stream.Close();
						}
					}
					
					fieldLengths[fieldNumber] = length; // save field length
					fieldPositions[fieldNumber] = position; // save field position
					fieldBoosts[fieldNumber] *= field.GetBoost();
					fieldOffsets[fieldNumber] = offset;
				}
			}
		}
		
		private Term termBuffer = new Term("", ""); // avoid consing
		
		private void  AddPosition(System.String field, System.String text, int position, TermVectorOffsetInfo offset)
		{
			termBuffer.Set(field, text);
			//System.out.println("Offset: " + offset);
			Posting ti = (Posting) postingTable[termBuffer];
			if (ti != null)
			{
				// word seen before
				int freq = ti.freq;
				if (ti.positions.Length == freq)
				{
					// positions array is full
					int[] newPositions = new int[freq * 2]; // double size
					int[] positions = ti.positions;
					Array.Copy(positions, 0, newPositions, 0, freq);
					ti.positions = newPositions;
				}
				ti.positions[freq] = position; // add new position
				
				if (offset != null)
				{
					if (ti.offsets.Length == freq)
					{
						TermVectorOffsetInfo[] newOffsets = new TermVectorOffsetInfo[freq * 2];
						TermVectorOffsetInfo[] offsets = ti.offsets;
						Array.Copy(offsets, 0, newOffsets, 0, freq);
						ti.offsets = newOffsets;
					}
					ti.offsets[freq] = offset;
				}
				ti.freq = freq + 1; // update frequency
			}
			else
			{
				// word not seen before
				Term term = new Term(field, text, false);
				postingTable[term] = new Posting(term, position, offset);
			}
		}
		
		private Posting[] SortPostingTable()
		{
			// copy postingTable into an array
			Posting[] array = new Posting[postingTable.Count];
			System.Collections.IEnumerator postings = postingTable.Values.GetEnumerator();
			for (int i = 0; postings.MoveNext(); i++)
			{
				array[i] = (Posting) postings.Current;
			}
			
			// sort the array
			QuickSort(array, 0, array.Length - 1);
			
			return array;
		}
		
		private static void  QuickSort(Posting[] postings, int lo, int hi)
		{
			if (lo >= hi)
				return ;
			
			int mid = (lo + hi) / 2;
			
			if (postings[lo].term.CompareTo(postings[mid].term) > 0)
			{
				Posting tmp = postings[lo];
				postings[lo] = postings[mid];
				postings[mid] = tmp;
			}
			
			if (postings[mid].term.CompareTo(postings[hi].term) > 0)
			{
				Posting tmp = postings[mid];
				postings[mid] = postings[hi];
				postings[hi] = tmp;
				
				if (postings[lo].term.CompareTo(postings[mid].term) > 0)
				{
					Posting tmp2 = postings[lo];
					postings[lo] = postings[mid];
					postings[mid] = tmp2;
				}
			}
			
			int left = lo + 1;
			int right = hi - 1;
			
			if (left >= right)
				return ;
			
			Term partition = postings[mid].term;
			
			for (; ; )
			{
				while (postings[right].term.CompareTo(partition) > 0)
					--right;
				
				while (left < right && postings[left].term.CompareTo(partition) <= 0)
					++left;
				
				if (left < right)
				{
					Posting tmp = postings[left];
					postings[left] = postings[right];
					postings[right] = tmp;
					--right;
				}
				else
				{
					break;
				}
			}
			
			QuickSort(postings, lo, left);
			QuickSort(postings, left + 1, hi);
		}
		
		private void  WritePostings(Posting[] postings, System.String segment)
		{
			IndexOutput freq = null, prox = null;
			TermInfosWriter tis = null;
			TermVectorsWriter termVectorWriter = null;
			try
			{
				//open files for inverse index storage
				freq = directory.CreateOutput(segment + ".frq");
				prox = directory.CreateOutput(segment + ".prx");
				tis = new TermInfosWriter(directory, segment, fieldInfos, termIndexInterval);
				TermInfo ti = new TermInfo();
				System.String currentField = null;
				
				for (int i = 0; i < postings.Length; i++)
				{
					Posting posting = postings[i];
					
					// add an entry to the dictionary with pointers to prox and freq files
					ti.Set(1, freq.GetFilePointer(), prox.GetFilePointer(), - 1);
					tis.Add(posting.term, ti);
					
					// add an entry to the freq file
					int postingFreq = posting.freq;
					if (postingFreq == 1)
					// optimize freq=1
						freq.WriteVInt(1);
					// set low bit of doc num.
					else
					{
						freq.WriteVInt(0); // the document number
						freq.WriteVInt(postingFreq); // frequency in doc
					}
					
					int lastPosition = 0; // write positions
					int[] positions = posting.positions;
					for (int j = 0; j < postingFreq; j++)
					{
						// use delta-encoding
						int position = positions[j];
						prox.WriteVInt(position - lastPosition);
						lastPosition = position;
					}
					// check to see if we switched to a new field
					System.String termField = posting.term.Field();
					if (currentField != termField)
					{
						// changing field - see if there is something to save
						currentField = termField;
						FieldInfo fi = fieldInfos.FieldInfo(currentField);
						if (fi.storeTermVector)
						{
							if (termVectorWriter == null)
							{
								termVectorWriter = new TermVectorsWriter(directory, segment, fieldInfos);
								termVectorWriter.OpenDocument();
							}
							termVectorWriter.OpenField(currentField);
						}
						else if (termVectorWriter != null)
						{
							termVectorWriter.CloseField();
						}
					}
					if (termVectorWriter != null && termVectorWriter.IsFieldOpen())
					{
						termVectorWriter.AddTerm(posting.term.Text(), postingFreq, posting.positions, posting.offsets);
					}
				}
				if (termVectorWriter != null)
					termVectorWriter.CloseDocument();
			}
			finally
			{
				// make an effort to close all streams we can but remember and re-throw
				// the first exception encountered in this process
				System.IO.IOException keep = null;
				if (freq != null)
					try
					{
						freq.Close();
					}
					catch (System.IO.IOException e)
					{
						if (keep == null)
							keep = e;
					}
				if (prox != null)
					try
					{
						prox.Close();
					}
					catch (System.IO.IOException e)
					{
						if (keep == null)
							keep = e;
					}
				if (tis != null)
					try
					{
						tis.Close();
					}
					catch (System.IO.IOException e)
					{
						if (keep == null)
							keep = e;
					}
				if (termVectorWriter != null)
					try
					{
						termVectorWriter.Close();
					}
					catch (System.IO.IOException e)
					{
						if (keep == null)
							keep = e;
					}
				if (keep != null)
				{
					throw new System.IO.IOException(keep.StackTrace);
				}
			}
		}
		
		private void  WriteNorms(System.String segment)
		{
			for (int n = 0; n < fieldInfos.Size(); n++)
			{
				FieldInfo fi = fieldInfos.FieldInfo(n);
				if (fi.isIndexed && !fi.omitNorms)
				{
					float norm = fieldBoosts[n] * similarity.LengthNorm(fi.name, fieldLengths[n]);
					IndexOutput norms = directory.CreateOutput(segment + ".f" + n);
					try
					{
						norms.WriteByte(Similarity.EncodeNorm(norm));
					}
					finally
					{
						norms.Close();
					}
				}
			}
		}
		
		/// <summary>If non-null, a message will be printed to this if maxFieldLength is reached.</summary>
		internal void  SetInfoStream(System.IO.TextWriter infoStream)
		{
			this.infoStream = infoStream;
		}
	}
	
	sealed class Posting
	{
		// info about a Term in a doc
		internal Term term; // the Term
		internal int freq; // its frequency in doc
		internal int[] positions; // positions it occurs at
		internal TermVectorOffsetInfo[] offsets;
		
		internal Posting(Term t, int position, TermVectorOffsetInfo offset)
		{
			term = t;
			freq = 1;
			positions = new int[1];
			positions[0] = position;
			if (offset != null)
			{
				offsets = new TermVectorOffsetInfo[1];
				offsets[0] = offset;
			}
			else
				offsets = null;
		}
	}
}*/