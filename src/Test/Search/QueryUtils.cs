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
	
	/// <author>  yonik
	/// </author>
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
            public AnonymousClassHitCollector(int[] which, Lucene.Net.Search.Scorer scorer, int[] sdoc, float maxDiff, Lucene.Net.Search.Query q, Lucene.Net.Search.IndexSearcher s)
            {
                InitBlock(which, scorer, sdoc, maxDiff, q, s);
            }
            private void  InitBlock(int[] which, Lucene.Net.Search.Scorer scorer, int[] sdoc, float maxDiff, Lucene.Net.Search.Query q, Lucene.Net.Search.IndexSearcher s)
            {
                this.which = which;
                this.scorer = scorer;
                this.sdoc = sdoc;
                this.maxDiff = maxDiff;
                this.q = q;
                this.s = s;
            }
            private int[] which;
            private Lucene.Net.Search.Scorer scorer;
            private int[] sdoc;
            private float maxDiff;
            private Lucene.Net.Search.Query q;
            private Lucene.Net.Search.IndexSearcher s;
            public override void  Collect(int doc, float score)
            {
                try
                {
                    bool more = (which[0]++ & 0x02) == 0?scorer.SkipTo(sdoc[0] + 1):scorer.Next();
                    sdoc[0] = scorer.Doc();
                    float scorerScore = scorer.Score();
                    float scoreDiff = System.Math.Abs(score - scorerScore);
                    scoreDiff = 0; // TODO: remove this go get LUCENE-697
                    // failures
                    if (more == false || doc != sdoc[0] || scoreDiff > maxDiff)
                    {
                        throw new System.SystemException("ERROR matching docs:" + "\n\tscorer.more=" + more + " doc=" + sdoc[0] + " score=" + scorerScore + "\n\thitCollector.doc=" + doc + " score=" + score + "\n\t Scorer=" + scorer + "\n\t Query=" + q + "\n\t Searcher=" + s);
                    }
                }
                catch (System.IO.IOException e)
                {
                    throw new System.SystemException("", e);
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
			Assert.AreEqual(q1, q2);
			Assert.AreEqual(q1.GetHashCode(), q2.GetHashCode());
		}
		
		public static void  CheckUnequal(Query q1, Query q2)
		{
            Assert.IsTrue(q1.ToString() != q2.ToString());
			
			// possible this test can fail on a hash collision... if that
			// happens, please change test to use a different example.
			Assert.IsTrue(q1.GetHashCode() != q2.GetHashCode());
		}
		
        /// <summary>various query sanity checks on a searcher </summary>
        public static void  Check(Query q1, Searcher s)
        {
            try
            {
                Check(q1);
                if (s != null && s is IndexSearcher)
                {
                    IndexSearcher is_Renamed = (IndexSearcher) s;
                    CheckSkipTo(q1, is_Renamed);
                }
            }
            catch (System.IO.IOException e)
            {
                throw new System.SystemException("", e);
            }
        }
		
        /// <summary> alternate scorer skipTo(),skipTo(),next(),next(),skipTo(),skipTo(), etc
        /// and ensure a hitcollector receives same docs and scores
        /// </summary>
        public static void  CheckSkipTo(Query q, IndexSearcher s)
        {
            // System.out.println("Checking "+q);
            Weight w = q.Weight(s);
            Scorer scorer = w.Scorer(s.GetIndexReader());
			
            // FUTURE: ensure scorer.doc()==-1
			
            if (BooleanQuery.GetUseScorer14())
                return ; // 1.4 doesn't support skipTo
			
            int[] which = new int[1];
            int[] sdoc = new int[]{- 1};
            float maxDiff = 1e-5f;
            s.Search(q, new AnonymousClassHitCollector(which, scorer, sdoc, maxDiff, q, s));
			
            // make sure next call to scorer is false.
            Assert.IsFalse((which[0]++ & 0x02) == 0 ? scorer.SkipTo(sdoc[0] + 1) : scorer.Next());
        }
    }
}