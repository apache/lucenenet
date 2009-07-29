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
	public class QueryUtils
	{
		[Serializable]
		private class AnonymousClassQuery : Query
		{
			public override System.String ToString(System.String field)
			{
				return "My Whacky Query";
			}
			override public System.Object Clone()
			{
				return null;
			}
		}

		private class AnonymousClassHitCollector : HitCollector
		{
			public AnonymousClassHitCollector(int[] order, int[] opidx, int skip_op, Lucene.Net.Search.Scorer scorer, int[] sdoc, float maxDiff, Lucene.Net.Search.Query q, Lucene.Net.Search.IndexSearcher s)
			{
				InitBlock(order, opidx, skip_op, scorer, sdoc, maxDiff, q, s);
			}
			private void  InitBlock(int[] order, int[] opidx, int skip_op, Lucene.Net.Search.Scorer scorer, int[] sdoc, float maxDiff, Lucene.Net.Search.Query q, Lucene.Net.Search.IndexSearcher s)
			{
				this.order = order;
				this.opidx = opidx;
				this.skip_op = skip_op;
				this.scorer = scorer;
				this.sdoc = sdoc;
				this.maxDiff = maxDiff;
				this.q = q;
				this.s = s;
			}
			private int[] order;
			private int[] opidx;
			private int skip_op;
			private Lucene.Net.Search.Scorer scorer;
			private int[] sdoc;
			private float maxDiff;
			private Lucene.Net.Search.Query q;
			private Lucene.Net.Search.IndexSearcher s;
			public override void  Collect(int doc, float score)
			{
				try
				{
					int op = order[(opidx[0]++) % order.Length];
					//System.out.println(op==skip_op ? "skip("+(sdoc[0]+1)+")":"next()");
					bool more = op == skip_op?scorer.SkipTo(sdoc[0] + 1):scorer.Next();
					sdoc[0] = scorer.Doc();
					float scorerScore = scorer.Score();
					float scorerScore2 = scorer.Score();
					float scoreDiff = System.Math.Abs(score - scorerScore);
					float scorerDiff = System.Math.Abs(scorerScore2 - scorerScore);
					if (!more || doc != sdoc[0] || scoreDiff > maxDiff || scorerDiff > maxDiff)
					{
						System.Text.StringBuilder sbord = new System.Text.StringBuilder();
						for (int i = 0; i < order.Length; i++)
							sbord.Append(order[i] == skip_op?" skip()":" next()");
						throw new System.SystemException("ERROR matching docs:" + "\n\t" + (doc != sdoc[0]?"--> ":"") + "doc=" + sdoc[0] + "\n\t" + (!more?"--> ":"") + "tscorer.more=" + more + "\n\t" + (scoreDiff > maxDiff?"--> ":"") + "scorerScore=" + scorerScore + " scoreDiff=" + scoreDiff + " maxDiff=" + maxDiff + "\n\t" + (scorerDiff > maxDiff?"--> ":"") + "scorerScore2=" + scorerScore2 + " scorerDiff=" + scorerDiff + "\n\thitCollector.doc=" + doc + " score=" + score + "\n\t Scorer=" + scorer + "\n\t Query=" + q + "  " + q.GetType().FullName + "\n\t Searcher=" + s + "\n\t Order=" + sbord + "\n\t Op=" + (op == skip_op?" skip()":" next()"));
					}
				}
				catch (System.IO.IOException e)
				{
					throw new System.Exception("", e);
				}
			}
		}
		
		private class AnonymousClassHitCollector1 : HitCollector
		{
			public AnonymousClassHitCollector1(int[] lastDoc, Lucene.Net.Search.Query q, Lucene.Net.Search.IndexSearcher s, float maxDiff)
			{
				InitBlock(lastDoc, q, s, maxDiff);
			}
			private void  InitBlock(int[] lastDoc, Lucene.Net.Search.Query q, Lucene.Net.Search.IndexSearcher s, float maxDiff)
			{
				this.lastDoc = lastDoc;
				this.q = q;
				this.s = s;
				this.maxDiff = maxDiff;
			}
			private int[] lastDoc;
			private Lucene.Net.Search.Query q;
			private Lucene.Net.Search.IndexSearcher s;
			private float maxDiff;
			public override void  Collect(int doc, float score)
			{
				//System.out.println("doc="+doc);
				try
				{
					for (int i = lastDoc[0] + 1; i <= doc; i++)
					{
						Weight w = q.Weight(s);
						Scorer scorer = w.Scorer(s.GetIndexReader());
						Assert.IsTrue(scorer.SkipTo(i), "query collected " + doc + " but skipTo(" + i + ") says no more docs!");
						Assert.AreEqual(doc, scorer.Doc(), "query collected " + doc + " but skipTo(" + i + ") got to " + scorer.Doc());
						float skipToScore = scorer.Score();
						Assert.AreEqual(skipToScore, scorer.Score(), maxDiff, "unstable skipTo(" + i + ") score!");
						Assert.AreEqual(score, skipToScore, maxDiff, "query assigned doc " + doc + " a score of <" + score + "> but skipTo(" + i + ") has <" + skipToScore + ">!");
					}
					lastDoc[0] = doc;
				}
				catch (System.IO.IOException e)
				{
					throw new System.Exception("", e);
				}
			}
		}
		
		/// <summary>Check the types of things query objects should be able to do. </summary>
		public static void  Check(Query q)
		{
			CheckHashEquals(q);
		}
		
		/// <summary>check very basic hashCode and equals </summary>
		public static void  CheckHashEquals(Query q)
		{
			Query q2 = (Query) q.Clone();
			CheckEqual(q, q2);
			
			Query q3 = (Query) q.Clone();
			q3.SetBoost(7.21792348f);
			CheckUnequal(q, q3);
			
			// test that a class check is done so that no exception is thrown
			// in the implementation of equals()
			Query whacky = new AnonymousClassQuery();
			whacky.SetBoost(q.GetBoost());
			CheckUnequal(q, whacky);
		}
		
		public static void  CheckEqual(Query q1, Query q2)
		{
			Assert.AreEqual(q1.ToString(), q2.ToString());
			Assert.AreEqual(q1.GetHashCode(), q2.GetHashCode());
		}
		
		public static void  CheckUnequal(Query q1, Query q2)
		{
			Assert.IsTrue(q1.ToString() != q2.ToString());
			Assert.IsTrue(q2.ToString() != q1.ToString());
			
			// possible this test can fail on a hash collision... if that
			// happens, please change test to use a different example.
			Assert.IsTrue(q1.GetHashCode() != q2.GetHashCode());
		}
		
		/// <summary>deep check that explanations of a query 'score' correctly </summary>
		public static void  CheckExplanations(Query q, Searcher s)
		{
			CheckHits.CheckExplanations(q, null, s, true);
		}
		
		/// <summary> various query sanity checks on a searcher, including explanation checks.</summary>
		/// <seealso cref="checkExplanations">
		/// </seealso>
		/// <seealso cref="checkSkipTo">
		/// </seealso>
		/// <seealso cref="Check(Query)">
		/// </seealso>
		public static void  Check(Query q1, Searcher s)
		{
			try
			{
				Check(q1);
				if (s != null)
				{
					if (s is IndexSearcher)
					{
						IndexSearcher is_Renamed = (IndexSearcher) s;
						CheckFirstSkipTo(q1, is_Renamed);
						CheckSkipTo(q1, is_Renamed);
					}
					CheckExplanations(q1, s);
					CheckSerialization(q1, s);
				}
			}
			catch (System.IO.IOException e)
			{
				throw new System.Exception("", e);
			}
		}
		
		/// <summary>check that the query weight is serializable. </summary>
		/// <throws>  IOException if serialization check fail.  </throws>
		private static void  CheckSerialization(Query q, Searcher s)
		{
			Weight w = q.Weight(s);
			try
			{
				System.IO.MemoryStream bos = new System.IO.MemoryStream();
				System.IO.BinaryWriter oos = new System.IO.BinaryWriter(bos);
				System.Runtime.Serialization.Formatters.Binary.BinaryFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
				formatter.Serialize(oos.BaseStream, w);
				oos.Close();
				System.IO.BinaryReader ois = new System.IO.BinaryReader(new System.IO.MemoryStream(bos.ToArray()));
				formatter.Deserialize(ois.BaseStream);
				ois.Close();
				
				//skip rquals() test for now - most weights don't overide equals() and we won't add this just for the tests.
				//TestCase.assertEquals("writeObject(w) != w.  ("+w+")",w2,w);   
			}
			catch (System.Exception e)
			{
				System.IO.IOException e2 = new System.IO.IOException("Serialization failed for " + w, e);
				throw e2;
			}
		}
		
		
		/// <summary>alternate scorer skipTo(),skipTo(),next(),next(),skipTo(),skipTo(), etc
		/// and ensure a hitcollector receives same docs and scores
		/// </summary>
		public static void  CheckSkipTo(Query q, IndexSearcher s)
		{
			//System.out.println("Checking "+q);
			
			if (BooleanQuery.GetAllowDocsOutOfOrder())
				return ; // in this case order of skipTo() might differ from that of next().
			
			int skip_op = 0;
			int next_op = 1;
			int[][] orders = new int[][]{new int[]{next_op}, new int[]{skip_op}, new int[]{skip_op, next_op}, new int[]{next_op, skip_op}, new int[]{skip_op, skip_op, next_op, next_op}, new int[]{next_op, next_op, skip_op, skip_op}, new int[]{skip_op, skip_op, skip_op, next_op, next_op}};
			for (int k = 0; k < orders.Length; k++)
			{
				int[] order = orders[k];
				//System.out.print("Order:");for (int i = 0; i < order.length; i++) System.out.print(order[i]==skip_op ? " skip()":" next()"); System.out.println();
				int[] opidx = new int[]{0};
				
				Weight w = q.Weight(s);
				Scorer scorer = w.Scorer(s.GetIndexReader());
				
				// FUTURE: ensure scorer.doc()==-1
				
				int[] sdoc = new int[]{- 1};
				float maxDiff = 1e-5f;
				s.Search(q, new AnonymousClassHitCollector(order, opidx, skip_op, scorer, sdoc, maxDiff, q, s));
				
				// make sure next call to scorer is false.
				int op = order[(opidx[0]++) % order.Length];
				//System.out.println(op==skip_op ? "last: skip()":"last: next()");
				bool more = op == skip_op?scorer.SkipTo(sdoc[0] + 1):scorer.Next();
				Assert.IsFalse(more);
			}
		}
		
		// check that first skip on just created scorers always goes to the right doc
		private static void  CheckFirstSkipTo(Query q, IndexSearcher s)
		{
			//System.out.println("checkFirstSkipTo: "+q);
			float maxDiff = 1e-5f;
			int[] lastDoc = new int[]{- 1};
			s.Search(q, new AnonymousClassHitCollector1(lastDoc, q, s, maxDiff));
			Weight w = q.Weight(s);
			Scorer scorer = w.Scorer(s.GetIndexReader());
			bool more = scorer.SkipTo(lastDoc[0] + 1);
			if (more)
				Assert.IsFalse(more, "query's last doc was " + lastDoc[0] + " but skipTo(" + (lastDoc[0] + 1) + ") got to " + scorer.Doc());
		}
	}
}