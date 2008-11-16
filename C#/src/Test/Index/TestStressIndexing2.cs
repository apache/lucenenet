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
using Lucene.Net.Store;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
using Lucene.Net.Analysis;

using NUnit.Framework;

namespace Lucene.Net.Index
{
	
	[TestFixture]
	public class TestStressIndexing2 : LuceneTestCase
	{
		internal class AnonymousClassComparator : System.Collections.IComparer
		{
            Fieldable f1, f2;
			public virtual int Compare(System.Object o1, System.Object o2)
			{
                if (o1 == o2) return 0;
                f1 = (Fieldable)o1;
                f2 = (Fieldable)o2;
                return String.CompareOrdinal(f1.Name() + f1.StringValue(), f2.Name() + f2.StringValue());
			}
		}
		internal static int maxFields = 4;
		internal static int bigFieldSize = 10;
		internal static bool sameFieldOrder = false;
		internal static bool autoCommit = false;
		internal static int mergeFactor = 3;
		internal static int maxBufferedDocs = 3;
		internal static int seed = 0;
		
		internal static System.Random r = new System.Random((System.Int32) 0);
		
		[Test]
		public virtual void  TestRandom()
		{
			Directory dir1 = new MockRAMDirectory();
			Directory dir2 = new MockRAMDirectory();
            System.Collections.IDictionary docs = IndexRandom(10, 100, 100, dir1);
			IndexSerial(docs, dir2);
			VerifyEquals(dir1, dir2, "id");
		}
		
		[Test]
		public virtual void  TestMultiConfig()
		{
			// test lots of smaller different params together
			for (int i = 0; i < 100; i++)
			{
				// increase iterations for better testing
				sameFieldOrder = r.NextDouble() > 0.5;
				autoCommit = r.NextDouble() > 0.5;
				mergeFactor = r.Next(3) + 2;
				maxBufferedDocs = r.Next(3) + 2;
				seed++;
				
				int nThreads = r.Next(5) + 1;
				int iter = r.Next(10) + 1;
				int range = r.Next(20) + 1;
				
				Directory dir1 = new MockRAMDirectory();
				Directory dir2 = new MockRAMDirectory();
                System.Collections.IDictionary docs = IndexRandom(nThreads, iter, range, dir1);
				IndexSerial(docs, dir2);
				VerifyEquals(dir1, dir2, "id");
			}
		}
		
		
		internal static Term idTerm = new Term("id", "");
		internal IndexingThread[] threads;
		internal static System.Collections.IComparer fieldNameComparator;
		
		// This test avoids using any extra synchronization in the multiple
		// indexing threads to test that IndexWriter does correctly synchronize
		// everything.
		
		public virtual System.Collections.IDictionary IndexRandom(int nThreads, int iterations, int range, Directory dir)
		{
			IndexWriter w = new IndexWriter(dir, autoCommit, new WhitespaceAnalyzer(), true);
			w.SetUseCompoundFile(false);
			
			// force many merges
			w.SetMergeFactor(mergeFactor);
			w.SetRAMBufferSizeMB(.1);
			w.SetMaxBufferedDocs(maxBufferedDocs);
			
			threads = new IndexingThread[nThreads];
			for (int i = 0; i < threads.Length; i++)
			{
				IndexingThread th = new IndexingThread();
				th.w = w;
				th.base_Renamed = 1000000 * i;
				th.range = range;
				th.iterations = iterations;
				threads[i] = th;
			}
			
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Start();
			}
			for (int i = 0; i < threads.Length; i++)
			{
				threads[i].Join();
			}
			
			// w.optimize();
			w.Close();
			
			System.Collections.IDictionary docs = new System.Collections.Hashtable();
			for (int i = 0; i < threads.Length; i++)
			{
				IndexingThread th = threads[i];
				lock (th)
				{
					System.Collections.IEnumerator e = th.docs.Keys.GetEnumerator();
					while (e.MoveNext())
					{
						docs[e.Current] = th.docs[e.Current];
					}
				}
			}
			
			return docs;
		}
		
		
		public static void  IndexSerial(System.Collections.IDictionary docs, Directory dir)
		{
			IndexWriter w = new IndexWriter(dir, new WhitespaceAnalyzer());
			
			// index all docs in a single thread
			System.Collections.IEnumerator iter = docs.Values.GetEnumerator();
			while (iter.MoveNext())
			{
				Document d = (Document) iter.Current;
				System.Collections.ArrayList fields = new System.Collections.ArrayList();
				fields.AddRange(d.GetFields());
                
                // nonono - can't do this (below)
                //
                // if multiple fields w/ same name, each instance must be
                // added in the same order as orginal doc, as the fields
                // are effectively concatendated
                //
                // term position/offset information must be maintained 
                
                // put fields in same order each time
                //fields.Sort(fieldNameComparator);

				Document d1 = new Document();
				d1.SetBoost(d.GetBoost());
				for (int i = 0; i < fields.Count; i++)
				{
					d1.Add((Fieldable) fields[i]);
				}
				w.AddDocument(d1);
				// System.out.println("indexing "+d1);
			}
			
			w.Close();
		}
		
		public static void  VerifyEquals(Directory dir1, Directory dir2, System.String idField)
		{
			IndexReader r1 = IndexReader.Open(dir1);
			IndexReader r2 = IndexReader.Open(dir2);
			VerifyEquals(r1, r2, idField);
			r1.Close();
			r2.Close();
		}
		
		
		public static void  VerifyEquals(IndexReader r1, IndexReader r2, System.String idField)
		{
			Assert.AreEqual(r1.NumDocs(), r2.NumDocs());
			bool hasDeletes = !(r1.MaxDoc() == r2.MaxDoc() && r1.NumDocs() == r1.MaxDoc());
			
			int[] r2r1 = new int[r2.MaxDoc()]; // r2 id to r1 id mapping
			
			TermDocs termDocs1 = r1.TermDocs();
			TermDocs termDocs2 = r2.TermDocs();
			
			// create mapping from id2 space to id2 based on idField
			idField = String.Intern(idField);
			TermEnum termEnum = r1.Terms(new Term(idField, ""));
			do 
			{
				Term term = termEnum.Term();
				if (term == null || (System.Object) term.Field() != (System.Object) idField)
					break;
				
				termDocs1.Seek(termEnum);
				Assert.IsTrue(termDocs1.Next());
				int id1 = termDocs1.Doc();
				Assert.IsFalse(termDocs1.Next());
				
				termDocs2.Seek(termEnum);
				Assert.IsTrue(termDocs2.Next());
				int id2 = termDocs2.Doc();
				Assert.IsFalse(termDocs2.Next());
				
				r2r1[id2] = id1;
				
				// verify stored fields are equivalent
				VerifyEquals(r1.Document(id1), r2.Document(id2));
				
				try
				{
					// verify term vectors are equivalent        
					VerifyEquals(r1.GetTermFreqVectors(id1), r2.GetTermFreqVectors(id2));
				}
				catch (System.ApplicationException e)
				{
					System.Console.Out.WriteLine("FAILED id=" + term + " id1=" + id1 + " id2=" + id2);
					TermFreqVector[] tv1 = r1.GetTermFreqVectors(id1);
					System.Console.Out.WriteLine("  d1=" + tv1);
					if (tv1 != null)
						for (int i = 0; i < tv1.Length; i++)
						{
							System.Console.Out.WriteLine("    " + i + ": " + tv1[i]);
						}
					
					TermFreqVector[] tv2 = r2.GetTermFreqVectors(id2);
					System.Console.Out.WriteLine("  d2=" + tv2);
					if (tv2 != null)
						for (int i = 0; i < tv2.Length; i++)
						{
							System.Console.Out.WriteLine("    " + i + ": " + tv2[i]);
						}
					
					throw e;
				}
			}
			while (termEnum.Next());
			
			termEnum.Close();
			
			// Verify postings
			TermEnum termEnum1 = r1.Terms(new Term("", ""));
			TermEnum termEnum2 = r2.Terms(new Term("", ""));
			
			// pack both doc and freq into single element for easy sorting
			long[] info1 = new long[r1.NumDocs()];
			long[] info2 = new long[r2.NumDocs()];
			
			for (; ; )
			{
				Term term1, term2;
				
				// iterate until we get some docs
				int len1;
				for (; ; )
				{
					len1 = 0;
					term1 = termEnum1.Term();
					if (term1 == null)
						break;
					termDocs1.Seek(termEnum1);
					while (termDocs1.Next())
					{
						int d1 = termDocs1.Doc();
						int f1 = termDocs1.Freq();
						info1[len1] = (((long) d1) << 32) | f1;
						len1++;
					}
					if (len1 > 0)
						break;
					if (!termEnum1.Next())
						break;
				}
				
				// iterate until we get some docs
				int len2;
				for (; ; )
				{
					len2 = 0;
					term2 = termEnum2.Term();
					if (term2 == null)
						break;
					termDocs2.Seek(termEnum2);
					while (termDocs2.Next())
					{
						int d2 = termDocs2.Doc();
						int f2 = termDocs2.Freq();
						info2[len2] = (((long) r2r1[d2]) << 32) | f2;
						len2++;
					}
					if (len2 > 0)
						break;
					if (!termEnum2.Next())
						break;
				}
				
				if (!hasDeletes)
					Assert.AreEqual(termEnum1.DocFreq(), termEnum2.DocFreq());
				
				Assert.AreEqual(len1, len2);
				if (len1 == 0)
					break; // no more terms
				
				Assert.AreEqual(term1, term2);
				
				// sort info2 to get it into ascending docid
				System.Array.Sort(info2, 0, len2 - 0);
				
				// now compare
				for (int i = 0; i < len1; i++)
				{
					Assert.AreEqual(info1[i], info2[i]);
				}
				
				termEnum1.Next();
				termEnum2.Next();
			}
		}
		
		public static void  VerifyEquals(Document d1, Document d2)
		{
			System.Collections.ArrayList ff1 = new System.Collections.ArrayList(d1.GetFields());
			System.Collections.ArrayList ff2 = new System.Collections.ArrayList(d2.GetFields());

			ff1.Sort(fieldNameComparator);
			ff2.Sort(fieldNameComparator);
			
			if (ff1.Count != ff2.Count)
			{
                // print out whole doc on error
                System.Console.Write("Doc 1:");
                for (int j = 0; j < ff1.Count; j++)
                {
                    Fieldable field = (Fieldable)ff1[j];
                    System.Console.Write(" {0}={1};", field.Name(), field.StringValue());
                }
                System.Console.WriteLine();
                System.Console.Write("Doc 2:");
                for (int j = 0; j < ff2.Count; j++)
                {
                    Fieldable field = (Fieldable)ff2[j];
                    System.Console.Write(" {0}={1};", field.Name(), field.StringValue());
                }
                System.Console.WriteLine(); Assert.AreEqual(ff1.Count, ff2.Count);
			}			
			
			for (int i = 0; i < ff1.Count; i++)
			{
				Fieldable f1 = (Fieldable) ff1[i];
				Fieldable f2 = (Fieldable) ff2[i];
				if (f1.IsBinary())
				{
					System.Diagnostics.Debug.Assert(f2.IsBinary());
					//TODO
				}
				else
				{
					System.String s1 = f1.StringValue();
					System.String s2 = f2.StringValue();
					if (!s1.Equals(s2))
					{
						// print out whole doc on error
                        System.Console.Write("Doc 1:");
                        for (int j = 0; j < ff1.Count; j++)
                        {
                            Fieldable field = (Fieldable)ff1[j];
                            System.Console.Write(" {0}={1};", field.Name(), field.StringValue());
                        }
                        System.Console.WriteLine();
                        System.Console.Write("Doc 2:");
                        for (int j = 0; j < ff2.Count; j++)
                        {
                            Fieldable field = (Fieldable)ff2[j];
                            System.Console.Write(" {0}={1};", field.Name(), field.StringValue());
                        }
                        System.Console.WriteLine();
                        Assert.AreEqual(s1, s2);
					}
				}
			}
		}
		
		public static void  VerifyEquals(TermFreqVector[] d1, TermFreqVector[] d2)
		{
			if (d1 == null)
			{
				Assert.IsTrue(d2 == null);
				return ;
			}
			Assert.IsTrue(d2 != null);
			
			Assert.AreEqual(d1.Length, d2.Length);
			for (int i = 0; i < d1.Length; i++)
			{
				TermFreqVector v1 = d1[i];
				TermFreqVector v2 = d2[i];
				Assert.AreEqual(v1.Size(), v2.Size());
				int numTerms = v1.Size();
				System.String[] terms1 = v1.GetTerms();
				System.String[] terms2 = v2.GetTerms();
				int[] freq1 = v1.GetTermFrequencies();
				int[] freq2 = v2.GetTermFrequencies();
				for (int j = 0; j < numTerms; j++)
				{
					if (!terms1[j].Equals(terms2[j]))
						Assert.AreEqual(terms1[j], terms2[j]);
					Assert.AreEqual(freq1[j], freq2[j]);
				}
				if (v1 is TermPositionVector)
				{
					Assert.IsTrue(v2 is TermPositionVector);
					TermPositionVector tpv1 = (TermPositionVector) v1;
					TermPositionVector tpv2 = (TermPositionVector) v2;
					for (int j = 0; j < numTerms; j++)
					{
						int[] pos1 = tpv1.GetTermPositions(j);
						int[] pos2 = tpv2.GetTermPositions(j);
						Assert.AreEqual(pos1.Length, pos2.Length);
						TermVectorOffsetInfo[] offsets1 = tpv1.GetOffsets(j);
						TermVectorOffsetInfo[] offsets2 = tpv2.GetOffsets(j);
						if (offsets1 == null)
							Assert.IsTrue(offsets2 == null);
						else
							Assert.IsTrue(offsets2 != null);
						for (int k = 0; k < pos1.Length; k++)
						{
                            Assert.AreEqual(pos1[k], pos2[k]);
							if (offsets1 != null)
							{
								Assert.AreEqual(offsets1[k].GetStartOffset(), offsets2[k].GetStartOffset());
								Assert.AreEqual(offsets1[k].GetEndOffset(), offsets2[k].GetEndOffset());
							}
						}
					}
				}
			}
		}
		
		internal class IndexingThread : SupportClass.ThreadClass
		{
			internal IndexWriter w;
			internal int base_Renamed;
			internal int range;
			internal int iterations;
			internal System.Collections.IDictionary docs = new System.Collections.Hashtable(); // Map<String,Document>
			internal System.Random r;
			
			public virtual int NextInt(int lim)
			{
				return r.Next(lim);
			}
			
			public virtual System.String GetString(int nTokens)
			{
				nTokens = nTokens != 0?nTokens:r.Next(4) + 1;
				// avoid StringBuffer because it adds extra synchronization.
				char[] arr = new char[nTokens * 2];
				for (int i = 0; i < nTokens; i++)
				{
					arr[i * 2] = (char) ('A' + r.Next(10));
					arr[i * 2 + 1] = ' ';
				}
				return new System.String(arr);
			}
			
			
			public virtual void  IndexDoc()
			{
				Document d = new Document();
				
				System.Collections.ArrayList fields = new System.Collections.ArrayList();
				int id = base_Renamed + NextInt(range);
				System.String idString = "" + id;
				Field idField = new Field("id", idString, Field.Store.YES, Field.Index.NO_NORMS);
				fields.Add(idField);
				
				int nFields = NextInt(Lucene.Net.Index.TestStressIndexing2.maxFields);
				for (int i = 0; i < nFields; i++)
				{
					
					Field.TermVector tvVal = Field.TermVector.NO;
                    switch (NextInt(4))
                    {

                        case 0:
                            tvVal = Field.TermVector.NO;
                            break;

                        case 1:
                            tvVal = Field.TermVector.YES;
                            break;

                        case 2:
                            tvVal = Field.TermVector.WITH_POSITIONS;
                            break;

                        case 3:
                            tvVal = Field.TermVector.WITH_POSITIONS_OFFSETS;
                            break;
                    }

                    switch (NextInt(4))
                    {

                        case 0:
                            fields.Add(new Field("f0", GetString(1), Field.Store.YES, Field.Index.NO_NORMS, tvVal));
                            break;

                        case 1:
                            fields.Add(new Field("f1", GetString(0), Field.Store.NO, Field.Index.TOKENIZED, tvVal));
                            break;

                        case 2:
                            fields.Add(new Field("f2", GetString(0), Field.Store.YES, Field.Index.NO, Field.TermVector.NO));
                            break;

                        case 3:
                            fields.Add(new Field("f3", GetString(Lucene.Net.Index.TestStressIndexing2.bigFieldSize), Field.Store.YES, Field.Index.TOKENIZED, tvVal));
                            break;
                    }
                }
				
				if (Lucene.Net.Index.TestStressIndexing2.sameFieldOrder)
				{
					fields.Sort(Lucene.Net.Index.TestStressIndexing2.fieldNameComparator);
				}
				else
				{
					// random placement of id field also
                    int index = NextInt(fields.Count);
                    fields[0] = fields[index];
                    fields[index] = idField;
				}
				
				for (int i = 0; i < fields.Count; i++)
				{
					d.Add((Fieldable) fields[i]);
				}
				w.UpdateDocument(Lucene.Net.Index.TestStressIndexing2.idTerm.CreateTerm(idString), d);
				// System.out.println("indexing "+d);
				docs[idString] = d;
			}
			
			override public void  Run()
			{
				try
				{
					r = new System.Random((System.Int32) (base_Renamed + range + Lucene.Net.Index.TestStressIndexing2.seed));
					for (int i = 0; i < iterations; i++)
					{
						IndexDoc();
					}
				}
				catch (System.Exception e)
				{
					System.Console.Error.WriteLine(e.StackTrace);
					Assert.Fail(e.ToString());
				}
				
				lock (this)
				{
					int generatedAux = docs.Count;
				}
			}
		}
		static TestStressIndexing2()
		{
			fieldNameComparator = new AnonymousClassComparator();
		}
	}
}
