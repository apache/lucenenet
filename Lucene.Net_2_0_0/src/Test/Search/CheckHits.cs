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

namespace Lucene.Net.Search
{
	
	public class CheckHits
	{
		/// <summary>Tests that a query has expected document number results.</summary>
		public static void  CheckHits_Renamed_Method(Query query, System.String defaultFieldName, Searcher searcher, int[] results)
		{
			Hits hits = searcher.Search(query);
			
			System.Collections.Hashtable correct = new System.Collections.Hashtable();
			for (int i = 0; i < results.Length; i++)
			{
				correct.Add((System.Int32) results[i], (System.Int32) results[i]);
			}
			
			System.Collections.Hashtable actual = new System.Collections.Hashtable();
			for (int i = 0; i < hits.Length(); i++)
			{
				actual.Add((System.Int32) hits.Id(i), (System.Int32) hits.Id(i));
			}
			
            if (correct.Count != 0)
            {
                System.Collections.IDictionaryEnumerator iter = correct.GetEnumerator();
                System.Collections.IDictionaryEnumerator iter2 = actual.GetEnumerator();
                bool status = true;
                while (iter2.MoveNext() && iter.MoveNext())
                {
                    if (iter2.Key.ToString() != iter.Key.ToString())
                    {
                        status = false;
                        break;
                    }
                }
                Assert.IsTrue(status, query.ToString(defaultFieldName));
            }
		}
		
		/// <summary>Tests that a Hits has an expected order of documents </summary>
		public static void  CheckDocIds(System.String mes, int[] results, Hits hits)
		{
			Assert.AreEqual(results.Length, hits.Length(), mes + " nr of hits");
			for (int i = 0; i < results.Length; i++)
			{
				Assert.AreEqual(results[i], hits.Id(i), mes + " doc nrs for hit " + i);
			}
		}
		
		/// <summary>Tests that two queries have an expected order of documents,
		/// and that the two queries have the same score values.
		/// </summary>
		public static void  CheckHitsQuery(Query query, Hits hits1, Hits hits2, int[] results)
		{
			
			CheckDocIds("hits1", results, hits1);
			CheckDocIds("hits2", results, hits2);
			CheckEqual(query, hits1, hits2);
		}
		
		public static void  CheckEqual(Query query, Hits hits1, Hits hits2)
		{
			float scoreTolerance = 1.0e-6f;
			if (hits1.Length() != hits2.Length())
			{
				Assert.Fail("Unequal lengths: hits1=" + hits1.Length() + ",hits2=" + hits2.Length());
			}
			for (int i = 0; i < hits1.Length(); i++)
			{
				if (hits1.Id(i) != hits2.Id(i))
				{
					Assert.Fail("Hit " + i + " docnumbers don't match\n" + Hits2str(hits1, hits2, 0, 0) + "for query:" + query.ToString());
				}
				
				if ((hits1.Id(i) != hits2.Id(i)) || System.Math.Abs(hits1.Score(i) - hits2.Score(i)) > scoreTolerance)
				{
					Assert.Fail("Hit " + i + ", doc nrs " + hits1.Id(i) + " and " + hits2.Id(i) + "\nunequal       : " + hits1.Score(i) + "\n           and: " + hits2.Score(i) + "\nfor query:" + query.ToString());
				}
			}
		}
		
		public static System.String Hits2str(Hits hits1, Hits hits2, int start, int end)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			int len1 = hits1 == null?0:hits1.Length();
			int len2 = hits2 == null?0:hits2.Length();
			if (end <= 0)
			{
				end = System.Math.Max(len1, len2);
			}
			
			sb.Append("Hits length1=" + len1 + "\tlength2=" + len2);
			
			sb.Append("\n");
			for (int i = start; i < end; i++)
			{
				sb.Append("hit=" + i + ":");
				if (i < len1)
				{
					sb.Append(" doc" + hits1.Id(i) + "=" + hits1.Score(i));
				}
				else
				{
					sb.Append("               ");
				}
				sb.Append(",\t");
				if (i < len2)
				{
					sb.Append(" doc" + hits2.Id(i) + "=" + hits2.Score(i));
				}
				sb.Append("\n");
			}
			return sb.ToString();
		}
		
		
		public static System.String TopdocsString(TopDocs docs, int start, int end)
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append("TopDocs totalHits=" + docs.totalHits + " top=" + docs.scoreDocs.Length + "\n");
			if (end <= 0)
				end = docs.scoreDocs.Length;
			else
				end = System.Math.Min(end, docs.scoreDocs.Length);
			for (int i = start; i < end; i++)
			{
				sb.Append("\t");
				sb.Append(i);
				sb.Append(") doc=");
				sb.Append(docs.scoreDocs[i].doc);
				sb.Append("\tscore=");
				sb.Append(docs.scoreDocs[i].score);
				sb.Append("\n");
			}
			return sb.ToString();
		}
	}
}