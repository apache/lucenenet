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
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;

namespace Lucene.Net.Index
{
	
	/// <summary>An IndexReader which reads multiple, parallel indexes.  Each index added
	/// must have the same number of documents, but typically each contains
	/// different fields.  Each document contains the union of the fields of all
	/// documents with the same document number.  When searching, matches for a
	/// query term are from the first index added that has the field.
	/// 
	/// <p>This is useful, e.g., with collections that have large fields which
	/// change rarely and small fields that change more frequently.  The smaller
	/// fields may be re-indexed in a new index and both indexes may be searched
	/// together.
	/// 
	/// <p><strong>Warning:</strong> It is up to you to make sure all indexes
	/// are created and modified the same way. For example, if you add
	/// documents to one index, you need to add the same documents in the
	/// same order to the other indexes. <em>Failure to do so will result in
	/// undefined behavior</em>.
	/// </summary>
	public class ParallelReader : IndexReader
	{
		private System.Collections.ArrayList readers = new System.Collections.ArrayList();
		private System.Collections.SortedList fieldToReader = new System.Collections.SortedList();
		private System.Collections.ArrayList storedFieldReaders = new System.Collections.ArrayList();
		
		private int maxDoc;
		private int numDocs;
		private bool hasDeletions;
		
		/// <summary>Construct a ParallelReader. </summary>
		public ParallelReader() : base(null)
		{
		}
		
		/// <summary>Add an IndexReader. </summary>
		public virtual void  Add(IndexReader reader)
		{
			Add(reader, false);
		}
		
		/// <summary>Add an IndexReader whose stored fields will not be returned.  This can
		/// accellerate search when stored fields are only needed from a subset of
		/// the IndexReaders.
		/// 
		/// </summary>
		/// <throws>  IllegalArgumentException if not all indexes contain the same number  </throws>
		/// <summary>     of documents
		/// </summary>
		/// <throws>  IllegalArgumentException if not all indexes have the same value  </throws>
		/// <summary>     of {@link IndexReader#MaxDoc()}
		/// </summary>
		public virtual void  Add(IndexReader reader, bool ignoreStoredFields)
		{
			
			if (readers.Count == 0)
			{
				this.maxDoc = reader.MaxDoc();
				this.numDocs = reader.NumDocs();
				this.hasDeletions = reader.HasDeletions();
			}
			
			if (reader.MaxDoc() != maxDoc)
			// check compatibility
				throw new System.ArgumentException("All readers must have same maxDoc: " + maxDoc + "!=" + reader.MaxDoc());
			if (reader.NumDocs() != numDocs)
				throw new System.ArgumentException("All readers must have same numDocs: " + numDocs + "!=" + reader.NumDocs());
			
			System.Collections.IEnumerator i = reader.GetFieldNames(IndexReader.FieldOption.ALL).GetEnumerator();
			while (i.MoveNext())
			{
                System.Collections.DictionaryEntry fi = (System.Collections.DictionaryEntry) i.Current;

				// update fieldToReader map
				System.String field = fi.Key.ToString();
				if (fieldToReader[field] == null)
					fieldToReader[field] = reader;
			}
			
			if (!ignoreStoredFields)
				storedFieldReaders.Add(reader); // add to storedFieldReaders
			readers.Add(reader);
		}
		
		public override int NumDocs()
		{
			return numDocs;
		}
		
		public override int MaxDoc()
		{
			return maxDoc;
		}
		
		public override bool HasDeletions()
		{
			return hasDeletions;
		}
		
		// check first reader
		public override bool IsDeleted(int n)
		{
			if (readers.Count > 0)
				return ((IndexReader) readers[0]).IsDeleted(n);
			return false;
		}
		
		// delete in all readers
		protected internal override void  DoDelete(int n)
		{
			for (int i = 0; i < readers.Count; i++)
			{
				((IndexReader) readers[i]).DeleteDocument(n);
			}
			hasDeletions = true;
		}
		
		// undeleteAll in all readers
		protected internal override void  DoUndeleteAll()
		{
			for (int i = 0; i < readers.Count; i++)
			{
				((IndexReader) readers[i]).UndeleteAll();
			}
			hasDeletions = false;
		}
		
		// append fields from storedFieldReaders
		public override Document Document(int n)
		{
			Document result = new Document();
			for (int i = 0; i < storedFieldReaders.Count; i++)
			{
				IndexReader reader = (IndexReader) storedFieldReaders[i];
                foreach(Field field in reader.Document(n).Fields())
				{
					result.Add(field);
				}
			}
			return result;
		}
		
		// get all vectors
		public override TermFreqVector[] GetTermFreqVectors(int n)
		{
			System.Collections.ArrayList results = new System.Collections.ArrayList();
			System.Collections.IEnumerator i = new System.Collections.Hashtable(fieldToReader).GetEnumerator();
			while (i.MoveNext())
			{
				System.Collections.DictionaryEntry e = (System.Collections.DictionaryEntry) i.Current;
				//IndexReader reader = (IndexReader) e.Key;         // {{Aroush}} which is right, those two lines?
				//System.String field = (System.String) e.Value;
                System.String field = (System.String) e.Key;        // {{Aroush-2.0}} or those two lines?
                IndexReader reader = (IndexReader) e.Value;
                TermFreqVector vector = reader.GetTermFreqVector(n, field);
				if (vector != null)
					results.Add(vector);
			}
			return (TermFreqVector[]) (results.ToArray(typeof(TermFreqVector)));
		}
		
		public override TermFreqVector GetTermFreqVector(int n, System.String field)
		{
            IndexReader reader = ((IndexReader) fieldToReader[field]);
            return reader == null ? null : reader.GetTermFreqVector(n, field);
        }
		
		public override bool HasNorms(System.String field)
		{
            IndexReader reader = ((IndexReader) fieldToReader[field]);
            return reader == null ? false : reader.HasNorms(field);
        }
		
		public override byte[] Norms(System.String field)
		{
            IndexReader reader = ((IndexReader) fieldToReader[field]);
            return reader == null ? null : reader.Norms(field);
        }
		
		public override void  Norms(System.String field, byte[] result, int offset)
		{
            IndexReader reader = ((IndexReader) fieldToReader[field]);
            if (reader != null)
                reader.Norms(field, result, offset);
        }
		
		protected internal override void  DoSetNorm(int n, System.String field, byte value_Renamed)
		{
            IndexReader reader = ((IndexReader) fieldToReader[field]);
            if (reader != null)
                reader.DoSetNorm(n, field, value_Renamed);
        }
		
		public override TermEnum Terms()
		{
			return new ParallelTermEnum(this);
		}
		
		public override TermEnum Terms(Term term)
		{
			return new ParallelTermEnum(this, term);
		}
		
		public override int DocFreq(Term term)
		{
            IndexReader reader = ((IndexReader) fieldToReader[term.Field()]);
            return reader == null ? 0 : reader.DocFreq(term);
        }
		
		public override TermDocs TermDocs(Term term)
		{
			return new ParallelTermDocs(this, term);
		}
		
		public override TermDocs TermDocs()
		{
			return new ParallelTermDocs(this);
		}
		
		public override TermPositions TermPositions(Term term)
		{
			return new ParallelTermPositions(this, term);
		}
		
		public override TermPositions TermPositions()
		{
			return new ParallelTermPositions(this);
		}
		
		protected internal override void  DoCommit()
		{
			for (int i = 0; i < readers.Count; i++)
				((IndexReader) readers[i]).Commit();
		}
		
		protected internal override void  DoClose()
		{
			lock (this)
			{
				for (int i = 0; i < readers.Count; i++)
					((IndexReader) readers[i]).Close();
			}
		}
		
		
        public override System.Collections.ICollection GetFieldNames(IndexReader.FieldOption fieldNames)
		{
            System.Collections.Hashtable fieldSet = new System.Collections.Hashtable();
            for (int i = 0; i < readers.Count; i++)
            {
                IndexReader reader = ((IndexReader) readers[i]);
                System.Collections.ICollection names = reader.GetFieldNames(fieldNames);
                for (System.Collections.IEnumerator iterator = names.GetEnumerator(); iterator.MoveNext(); )
                {
                    System.Collections.DictionaryEntry fi = (System.Collections.DictionaryEntry) iterator.Current;
                    System.String s = fi.Key.ToString();
                    if (fieldSet.ContainsKey(s) == false)
                    {
                        fieldSet.Add(s, s);
                    }
                }
            }
            return fieldSet;
		}
		
		private class ParallelTermEnum : TermEnum
		{
			private void  InitBlock(ParallelReader enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ParallelReader enclosingInstance;
			public ParallelReader Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private System.String field;
			private TermEnum termEnum;
			
			public ParallelTermEnum(ParallelReader enclosingInstance)
			{
				InitBlock(enclosingInstance);
				field = ((System.String) Enclosing_Instance.fieldToReader.GetKey(0));
				if (field != null)
					termEnum = ((IndexReader) Enclosing_Instance.fieldToReader[field]).Terms();
			}
			
			public ParallelTermEnum(ParallelReader enclosingInstance, Term term)
			{
				InitBlock(enclosingInstance);
				field = term.Field();
                IndexReader reader = ((IndexReader) Enclosing_Instance.fieldToReader[field]);
                if (reader != null)
                    termEnum = reader.Terms(term);
            }
			
			public override bool Next()
			{
				if (termEnum == null)
					return false;
				
				bool next = termEnum.Next();
				
				// still within field?
				if (next && (System.Object) termEnum.Term().Field() == (System.Object) field)
					return true; // yes, keep going
				
				termEnum.Close(); // close old termEnum
				
				// find the next field, if any
				field = ((System.String) SupportClass.TailMap(Enclosing_Instance.fieldToReader, field).GetKey(0));
				if (field != null)
				{
					termEnum = ((IndexReader) Enclosing_Instance.fieldToReader[field]).Terms();
					return true;
				}
				
				return false; // no more fields
			}
			
			public override Term Term()
			{
                if (termEnum == null)
                    return null;
				
                return termEnum.Term();
			}
			public override int DocFreq()
			{
                if (termEnum == null)
                    return 0;
				
                return termEnum.DocFreq();
			}
			public override void  Close()
			{
                if (termEnum != null)
                    termEnum.Close();
			}
		}
		
		// wrap a TermDocs in order to support seek(Term)
		private class ParallelTermDocs : TermDocs
		{
			private void  InitBlock(ParallelReader enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ParallelReader enclosingInstance;
			public ParallelReader Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			protected internal TermDocs termDocs;
			
			public ParallelTermDocs(ParallelReader enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			public ParallelTermDocs(ParallelReader enclosingInstance, Term term)
			{
				InitBlock(enclosingInstance);
				Seek(term);
			}
			
			public virtual int Doc()
			{
				return termDocs.Doc();
			}
			public virtual int Freq()
			{
				return termDocs.Freq();
			}
			
			public virtual void  Seek(Term term)
			{
                IndexReader reader = ((IndexReader) Enclosing_Instance.fieldToReader[term.Field()]);
                termDocs = reader != null ? reader.TermDocs(term) : null;
            }
			
			public virtual void  Seek(TermEnum termEnum)
			{
				Seek(termEnum.Term());
			}
			
			public virtual bool Next()
			{
                if (termDocs == null)
                    return false;
				
                return termDocs.Next();
			}
			
			public virtual int Read(int[] docs, int[] freqs)
			{
                if (termDocs == null)
                    return 0;
				
                return termDocs.Read(docs, freqs);
			}
			
			public virtual bool SkipTo(int target)
			{
                if (termDocs == null)
                    return false;
				
                return termDocs.SkipTo(target);
			}
			
			public virtual void  Close()
			{
                if (termDocs != null)
                    termDocs.Close();
			}
		}
		
		private class ParallelTermPositions : ParallelTermDocs, TermPositions
		{
			private void  InitBlock(ParallelReader enclosingInstance)
			{
				this.enclosingInstance = enclosingInstance;
			}
			private ParallelReader enclosingInstance;
			public new ParallelReader Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			
			public ParallelTermPositions(ParallelReader enclosingInstance) : base(enclosingInstance)
			{
				InitBlock(enclosingInstance);
			}
			public ParallelTermPositions(ParallelReader enclosingInstance, Term term) : base(enclosingInstance)
			{
				InitBlock(enclosingInstance);
				Seek(term);
			}
			
			public override void  Seek(Term term)
			{
                IndexReader reader = ((IndexReader) Enclosing_Instance.fieldToReader[term.Field()]);
                termDocs = reader != null ? reader.TermPositions(term) : null;
            }
			
			public virtual int NextPosition()
			{
				// It is an error to call this if there is no next position, e.g. if termDocs==null
				return ((TermPositions) termDocs).NextPosition();
			}
		}
	}
}