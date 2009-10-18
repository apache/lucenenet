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
using Term = Lucene.Net.Index.Term;

namespace Lucene.Net.Search
{
	[TestFixture]
	public class TestBooleanQuery
	{
		
		[Test]
        public virtual void  TestEquality()
		{
			BooleanQuery bq1 = new BooleanQuery();
			bq1.Add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur.SHOULD);
			bq1.Add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur.SHOULD);
			BooleanQuery nested1 = new BooleanQuery();
			nested1.Add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur.SHOULD);
			nested1.Add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur.SHOULD);
			bq1.Add(nested1, BooleanClause.Occur.SHOULD);
			
			BooleanQuery bq2 = new BooleanQuery();
			bq2.Add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur.SHOULD);
			bq2.Add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur.SHOULD);
			BooleanQuery nested2 = new BooleanQuery();
			nested2.Add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur.SHOULD);
			nested2.Add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur.SHOULD);
			bq2.Add(nested2, BooleanClause.Occur.SHOULD);
			
			Assert.AreEqual(bq1, bq2);
		}
		
		[Test]
        public virtual void  TestException()
		{
			try
			{
				BooleanQuery.SetMaxClauseCount(0);
				Assert.Fail();
			}
			catch (System.ArgumentException e)
			{
				// okay
			}
		}
	}
}