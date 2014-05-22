using System;

namespace Lucene.Net.Search
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */


	using IndexReader = Lucene.Net.Index.IndexReader;
	using Term = Lucene.Net.Index.Term;

	/// <summary>
	/// Assertion-enabled query. </summary>
	public class AssertingQuery : Query
	{

	  private readonly Random Random;
	  private readonly Query @in;

	  /// <summary>
	  /// Sole constructor. </summary>
	  public AssertingQuery(Random random, Query @in)
	  {
		this.Random = random;
		this.@in = @in;
	  }

	  /// <summary>
	  /// Wrap a query if necessary. </summary>
	  public static Query Wrap(Random random, Query query)
	  {
		return query is AssertingQuery ? query : new AssertingQuery(random, query);
	  }

	  public override Weight CreateWeight(IndexSearcher searcher)
	  {
		return AssertingWeight.Wrap(new Random(Random.nextLong()), @in.createWeight(searcher));
	  }

	  public override void ExtractTerms(Set<Term> terms)
	  {
		@in.extractTerms(terms);
	  }

	  public override string ToString(string field)
	  {
		return @in.ToString(field);
	  }

	  public override bool Equals(object obj)
	  {
		if (obj == null || !(obj is AssertingQuery))
		{
		  return false;
		}
		AssertingQuery that = (AssertingQuery) obj;
		return this.@in.Equals(that.@in);
	  }

	  public override int HashCode()
	  {
		return -@in.GetHashCode();
	  }

	  public override Query Clone()
	  {
		return Wrap(new Random(Random.nextLong()), @in.clone());
	  }

	  public override Query Rewrite(IndexReader reader)
	  {
		Query rewritten = @in.rewrite(reader);
		if (rewritten == @in)
		{
		  return this;
		}
		else
		{
		  return Wrap(new Random(Random.nextLong()), rewritten);
		}
	  }

	  public override float Boost
	  {
		  get
		  {
			return @in.Boost;
		  }
		  set
		  {
			@in.Boost = value;
		  }
	  }


	}

}