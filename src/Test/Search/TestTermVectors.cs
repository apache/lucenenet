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
using NUnit.Framework;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Lucene.Net.Index;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;
using English = Lucene.Net.Util.English;

namespace Lucene.Net.Search
{
	[TestFixture]
	public class TestTermVectors
	{
		private IndexSearcher searcher;
		private RAMDirectory directory = new RAMDirectory();

		
		[SetUp]
        public virtual void  SetUp()
		{
			IndexWriter writer = new IndexWriter(directory, new SimpleAnalyzer(), true);
			//writer.setUseCompoundFile(true);
			//writer.infoStream = System.out;
			for (int i = 0; i < 1000; i++)
			{
				Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
				Field.TermVector termVector;
				int mod3 = i % 3;
				int mod2 = i % 2;
				if (mod2 == 0 && mod3 == 0)
				{
					termVector = Field.TermVector.WITH_POSITIONS_OFFSETS;
				}
				else if (mod2 == 0)
				{
					termVector = Field.TermVector.WITH_POSITIONS;
				}
				else if (mod3 == 0)
				{
					termVector = Field.TermVector.WITH_OFFSETS;
				}
				else
				{
					termVector = Field.TermVector.YES;
				}
				doc.Add(new Field("field", English.IntToEnglish(i), Field.Store.YES, Field.Index.TOKENIZED, termVector));
				writer.AddDocument(doc);
			}
			writer.Close();
			searcher = new IndexSearcher(directory);
		}
		
		[TearDown]
        public virtual void  TearDown()
		{
			
		}
		
		[Test]
        public virtual void  Test()
		{
			Assert.IsTrue(searcher != null);
		}
		
		[Test]
        public virtual void  TestTermVectors_Renamed_Method()
		{
			Query query = new TermQuery(new Term("field", "seventy"));
			try
			{
				Hits hits = searcher.Search(query);
				Assert.AreEqual(100, hits.Length());
				
				for (int i = 0; i < hits.Length(); i++)
				{
					TermFreqVector[] vector = searcher.Reader.GetTermFreqVectors(hits.Id(i));
					Assert.IsTrue(vector != null);
					Assert.IsTrue(vector.Length == 1);
				}
			}
			catch (System.IO.IOException e)
			{
				Assert.IsTrue(false);
			}
		}
		
		[Test]
        public virtual void  TestTermPositionVectors()
		{
			Query query = new TermQuery(new Term("field", "zero"));
			try
			{
				Hits hits = searcher.Search(query);
				Assert.AreEqual(1, hits.Length());
				
				for (int i = 0; i < hits.Length(); i++)
				{
					TermFreqVector[] vector = searcher.Reader.GetTermFreqVectors(hits.Id(i));
					Assert.IsTrue(vector != null);
					Assert.IsTrue(vector.Length == 1);
					
					bool shouldBePosVector = (hits.Id(i) % 2 == 0)?true:false;
					Assert.IsTrue((shouldBePosVector == false) || (shouldBePosVector == true && (vector[0] is TermPositionVector == true)));
					
					bool shouldBeOffVector = (hits.Id(i) % 3 == 0)?true:false;
					Assert.IsTrue((shouldBeOffVector == false) || (shouldBeOffVector == true && (vector[0] is TermPositionVector == true)));
					
					if (shouldBePosVector || shouldBeOffVector)
					{
						TermPositionVector posVec = (TermPositionVector) vector[0];
						System.String[] terms = posVec.GetTerms();
						Assert.IsTrue(terms != null && terms.Length > 0);
						
						for (int j = 0; j < terms.Length; j++)
						{
							int[] positions = posVec.GetTermPositions(j);
							TermVectorOffsetInfo[] offsets = posVec.GetOffsets(j);
							
							if (shouldBePosVector)
							{
								Assert.IsTrue(positions != null);
								Assert.IsTrue(positions.Length > 0);
							}
							else
								Assert.IsTrue(positions == null);
							
							if (shouldBeOffVector)
							{
								Assert.IsTrue(offsets != null);
								Assert.IsTrue(offsets.Length > 0);
							}
							else
								Assert.IsTrue(offsets == null);
						}
					}
					else
					{
						try
						{
							TermPositionVector posVec = (TermPositionVector) vector[0];
							Assert.IsTrue(false);
						}
						catch (System.InvalidCastException ignore)
						{
							TermFreqVector freqVec = vector[0];
							System.String[] terms = freqVec.GetTerms();
							Assert.IsTrue(terms != null && terms.Length > 0);
						}
					}
				}
			}
			catch (System.IO.IOException e)
			{
				Assert.IsTrue(false);
			}
		}
		
		[Test]
        public virtual void  TestTermOffsetVectors()
		{
			Query query = new TermQuery(new Term("field", "fifty"));
			try
			{
				Hits hits = searcher.Search(query);
				Assert.AreEqual(100, hits.Length());
				
				for (int i = 0; i < hits.Length(); i++)
				{
					TermFreqVector[] vector = searcher.Reader.GetTermFreqVectors(hits.Id(i));
					Assert.IsTrue(vector != null);
					Assert.IsTrue(vector.Length == 1);
					
					//Assert.IsTrue();
				}
			}
			catch (System.IO.IOException e)
			{
				Assert.IsTrue(false);
			}
		}
		
		[Test]
        public virtual void  TestKnownSetOfDocuments()
		{
			System.String test1 = "eating chocolate in a computer lab"; //6 terms
			System.String test2 = "computer in a computer lab"; //5 terms
			System.String test3 = "a chocolate lab grows old"; //5 terms
			System.String test4 = "eating chocolate with a chocolate lab in an old chocolate colored computer lab"; //13 terms
			System.Collections.IDictionary test4Map = new System.Collections.Hashtable();
			test4Map["chocolate"] = 3;
			test4Map["lab"] = 2;
			test4Map["eating"] = 1;
			test4Map["computer"] = 1;
			test4Map["with"] = 1;
			test4Map["a"] = 1;
			test4Map["colored"] = 1;
			test4Map["in"] = 1;
			test4Map["an"] = 1;
			test4Map["computer"] = 1;
			test4Map["old"] = 1;
			
			Lucene.Net.Documents.Document testDoc1 = new Lucene.Net.Documents.Document();
			SetupDoc(testDoc1, test1);
			Lucene.Net.Documents.Document testDoc2 = new Lucene.Net.Documents.Document();
			SetupDoc(testDoc2, test2);
			Lucene.Net.Documents.Document testDoc3 = new Lucene.Net.Documents.Document();
			SetupDoc(testDoc3, test3);
			Lucene.Net.Documents.Document testDoc4 = new Lucene.Net.Documents.Document();
			SetupDoc(testDoc4, test4);
			
			Directory dir = new RAMDirectory();
			
			try
			{
				IndexWriter writer = new IndexWriter(dir, new SimpleAnalyzer(), true);
				Assert.IsTrue(writer != null);
				writer.AddDocument(testDoc1);
				writer.AddDocument(testDoc2);
				writer.AddDocument(testDoc3);
				writer.AddDocument(testDoc4);
				writer.Close();
				IndexSearcher knownSearcher = new IndexSearcher(dir);
				TermEnum termEnum = knownSearcher.Reader.Terms();
				TermDocs termDocs = knownSearcher.Reader.TermDocs();
				//System.out.println("Terms: " + termEnum.size() + " Orig Len: " + termArray.length);
				
				Similarity sim = knownSearcher.GetSimilarity();
				while (termEnum.Next() == true)
				{
					Term term = termEnum.Term();
					//System.out.println("Term: " + term);
					termDocs.Seek(term);
					while (termDocs.Next())
					{
						int docId = termDocs.Doc();
						int freq = termDocs.Freq();
						//System.out.println("Doc Id: " + docId + " freq " + freq);
						TermFreqVector vector = knownSearcher.Reader.GetTermFreqVector(docId, "field");
						float tf = sim.Tf(freq);
						float idf = sim.Idf(term, knownSearcher);
						//float qNorm = sim.queryNorm()
						//This is fine since we don't have stop words
						float lNorm = sim.LengthNorm("field", vector.GetTerms().Length);
						//float coord = sim.coord()
						//System.out.println("TF: " + tf + " IDF: " + idf + " LenNorm: " + lNorm);
						Assert.IsTrue(vector != null);
						System.String[] vTerms = vector.GetTerms();
						int[] freqs = vector.GetTermFrequencies();
						for (int i = 0; i < vTerms.Length; i++)
						{
							if (term.Text().Equals(vTerms[i]))
							{
								Assert.IsTrue(freqs[i] == freq);
							}
						}
					}
					//System.out.println("--------");
				}
				Query query = new TermQuery(new Term("field", "chocolate"));
				Hits hits = knownSearcher.Search(query);
				//doc 3 should be the first hit b/c it is the shortest match
				Assert.IsTrue(hits.Length() == 3);
				float score = hits.Score(0);
				/*System.out.println("Hit 0: " + hits.id(0) + " Score: " + hits.score(0) + " String: " + hits.doc(0).toString());
				System.out.println("Explain: " + knownSearcher.explain(query, hits.id(0)));
				System.out.println("Hit 1: " + hits.id(1) + " Score: " + hits.score(1) + " String: " + hits.doc(1).toString());
				System.out.println("Explain: " + knownSearcher.explain(query, hits.id(1)));
				System.out.println("Hit 2: " + hits.id(2) + " Score: " + hits.score(2) + " String: " +  hits.doc(2).toString());
				System.out.println("Explain: " + knownSearcher.explain(query, hits.id(2)));*/
				Assert.IsTrue(hits.Id(0) == 2);
				Assert.IsTrue(hits.Id(1) == 3);
				Assert.IsTrue(hits.Id(2) == 0);
				TermFreqVector vector2 = knownSearcher.Reader.GetTermFreqVector(hits.Id(1), "field");
				Assert.IsTrue(vector2 != null);
				//System.out.println("Vector: " + vector);
				System.String[] terms = vector2.GetTerms();
				int[] freqs2 = vector2.GetTermFrequencies();
				Assert.IsTrue(terms != null && terms.Length == 10);
				for (int i = 0; i < terms.Length; i++)
				{
					System.String term = terms[i];
					//System.out.println("Term: " + term);
					int freq = freqs2[i];
					Assert.IsTrue(test4.IndexOf(term) != - 1);
                    System.Int32 freqInt = -1;
                    try
                    {
                        freqInt = (System.Int32) test4Map[term];
                    }
                    catch (Exception)
                    {
                        Assert.IsTrue(false);
                    }
                    Assert.IsTrue(freqInt == freq);
				}
				knownSearcher.Close();
			}
			catch (System.IO.IOException e)
			{
                System.Console.Error.WriteLine(e.StackTrace);
				Assert.IsTrue(false);
			}
		}
		
		private void  SetupDoc(Lucene.Net.Documents.Document doc, System.String text)
		{
			doc.Add(new Field("field", text, Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.YES));
			//System.out.println("Document: " + doc);
		}
	}
}