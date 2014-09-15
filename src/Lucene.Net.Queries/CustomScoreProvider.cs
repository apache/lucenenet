namespace org.apache.lucene.queries
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

	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using IndexReader = org.apache.lucene.index.IndexReader; // for javadocs
	using FunctionQuery = org.apache.lucene.queries.function.FunctionQuery;
	using Explanation = org.apache.lucene.search.Explanation;
	using FieldCache = org.apache.lucene.search.FieldCache; // for javadocs

	/// <summary>
	/// An instance of this subclass should be returned by
	/// <seealso cref="CustomScoreQuery#getCustomScoreProvider"/>, if you want
	/// to modify the custom score calculation of a <seealso cref="CustomScoreQuery"/>.
	/// <para>Since Lucene 2.9, queries operate on each segment of an index separately,
	/// so the protected <seealso cref="#context"/> field can be used to resolve doc IDs,
	/// as the supplied <code>doc</code> ID is per-segment and without knowledge
	/// of the IndexReader you cannot access the document or <seealso cref="FieldCache"/>.
	/// 
	/// @lucene.experimental
	/// @since 2.9.2
	/// </para>
	/// </summary>
	public class CustomScoreProvider
	{

	  protected internal readonly AtomicReaderContext context;

	  /// <summary>
	  /// Creates a new instance of the provider class for the given <seealso cref="IndexReader"/>.
	  /// </summary>
	  public CustomScoreProvider(AtomicReaderContext context)
	  {
		this.context = context;
	  }

	  /// <summary>
	  /// Compute a custom score by the subQuery score and a number of 
	  /// <seealso cref="org.apache.lucene.queries.function.FunctionQuery"/> scores.
	  /// <para> 
	  /// Subclasses can override this method to modify the custom score.  
	  /// </para>
	  /// <para>
	  /// If your custom scoring is different than the default herein you 
	  /// should override at least one of the two customScore() methods.
	  /// If the number of <seealso cref="FunctionQuery function queries"/> is always &lt; 2 it is 
	  /// sufficient to override the other 
	  /// <seealso cref="#customScore(int, float, float) customScore()"/> 
	  /// method, which is simpler. 
	  /// </para>
	  /// <para>
	  /// The default computation herein is a multiplication of given scores:
	  /// <pre>
	  ///     ModifiedScore = valSrcScore * valSrcScores[0] * valSrcScores[1] * ...
	  /// </pre>
	  /// 
	  /// </para>
	  /// </summary>
	  /// <param name="doc"> id of scored doc. </param>
	  /// <param name="subQueryScore"> score of that doc by the subQuery. </param>
	  /// <param name="valSrcScores"> scores of that doc by the <seealso cref="FunctionQuery"/>. </param>
	  /// <returns> custom score. </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public float customScore(int doc, float subQueryScore, float valSrcScores[]) throws java.io.IOException
	  public virtual float customScore(int doc, float subQueryScore, float[] valSrcScores)
	  {
		if (valSrcScores.Length == 1)
		{
		  return customScore(doc, subQueryScore, valSrcScores[0]);
		}
		if (valSrcScores.Length == 0)
		{
		  return customScore(doc, subQueryScore, 1);
		}
		float score = subQueryScore;
		foreach (float valSrcScore in valSrcScores)
		{
		  score *= valSrcScore;
		}
		return score;
	  }

	  /// <summary>
	  /// Compute a custom score by the subQuery score and the <seealso cref="FunctionQuery"/> score.
	  /// <para> 
	  /// Subclasses can override this method to modify the custom score.
	  /// </para>
	  /// <para>
	  /// If your custom scoring is different than the default herein you 
	  /// should override at least one of the two customScore() methods.
	  /// If the number of <seealso cref="FunctionQuery function queries"/> is always &lt; 2 it is 
	  /// sufficient to override this customScore() method, which is simpler. 
	  /// </para>
	  /// <para>
	  /// The default computation herein is a multiplication of the two scores:
	  /// <pre>
	  ///     ModifiedScore = subQueryScore * valSrcScore
	  /// </pre>
	  /// 
	  /// </para>
	  /// </summary>
	  /// <param name="doc"> id of scored doc. </param>
	  /// <param name="subQueryScore"> score of that doc by the subQuery. </param>
	  /// <param name="valSrcScore"> score of that doc by the <seealso cref="FunctionQuery"/>. </param>
	  /// <returns> custom score. </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public float customScore(int doc, float subQueryScore, float valSrcScore) throws java.io.IOException
	  public virtual float customScore(int doc, float subQueryScore, float valSrcScore)
	  {
		return subQueryScore * valSrcScore;
	  }

	  /// <summary>
	  /// Explain the custom score.
	  /// Whenever overriding <seealso cref="#customScore(int, float, float[])"/>, 
	  /// this method should also be overridden to provide the correct explanation
	  /// for the part of the custom scoring.
	  /// </summary>
	  /// <param name="doc"> doc being explained. </param>
	  /// <param name="subQueryExpl"> explanation for the sub-query part. </param>
	  /// <param name="valSrcExpls"> explanation for the value source part. </param>
	  /// <returns> an explanation for the custom score </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public org.apache.lucene.search.Explanation customExplain(int doc, org.apache.lucene.search.Explanation subQueryExpl, org.apache.lucene.search.Explanation valSrcExpls[]) throws java.io.IOException
	  public virtual Explanation customExplain(int doc, Explanation subQueryExpl, Explanation[] valSrcExpls)
	  {
		if (valSrcExpls.Length == 1)
		{
		  return customExplain(doc, subQueryExpl, valSrcExpls[0]);
		}
		if (valSrcExpls.Length == 0)
		{
		  return subQueryExpl;
		}
		float valSrcScore = 1;
		foreach (Explanation valSrcExpl in valSrcExpls)
		{
		  valSrcScore *= valSrcExpl.Value;
		}
		Explanation exp = new Explanation(valSrcScore * subQueryExpl.Value, "custom score: product of:");
		exp.addDetail(subQueryExpl);
		foreach (Explanation valSrcExpl in valSrcExpls)
		{
		  exp.addDetail(valSrcExpl);
		}
		return exp;
	  }

	  /// <summary>
	  /// Explain the custom score.
	  /// Whenever overriding <seealso cref="#customScore(int, float, float)"/>, 
	  /// this method should also be overridden to provide the correct explanation
	  /// for the part of the custom scoring.
	  /// </summary>
	  /// <param name="doc"> doc being explained. </param>
	  /// <param name="subQueryExpl"> explanation for the sub-query part. </param>
	  /// <param name="valSrcExpl"> explanation for the value source part. </param>
	  /// <returns> an explanation for the custom score </returns>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public org.apache.lucene.search.Explanation customExplain(int doc, org.apache.lucene.search.Explanation subQueryExpl, org.apache.lucene.search.Explanation valSrcExpl) throws java.io.IOException
	  public virtual Explanation customExplain(int doc, Explanation subQueryExpl, Explanation valSrcExpl)
	  {
		float valSrcScore = 1;
		if (valSrcExpl != null)
		{
		  valSrcScore *= valSrcExpl.Value;
		}
		Explanation exp = new Explanation(valSrcScore * subQueryExpl.Value, "custom score: product of:");
		exp.addDetail(subQueryExpl);
		exp.addDetail(valSrcExpl);
		return exp;
	  }

	}

}