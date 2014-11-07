namespace org.apache.lucene.analysis.@in
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

	using CharTermAttribute = org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

	/// <summary>
	/// A <seealso cref="TokenFilter"/> that applies <seealso cref="IndicNormalizer"/> to normalize text
	/// in Indian Languages.
	/// </summary>
	public sealed class IndicNormalizationFilter : TokenFilter
	{
	  private readonly CharTermAttribute termAtt = addAttribute(typeof(CharTermAttribute));
	  private readonly IndicNormalizer normalizer = new IndicNormalizer();

	  public IndicNormalizationFilter(TokenStream input) : base(input)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  termAtt.Length = normalizer.normalize(termAtt.buffer(), termAtt.length());
		  return true;
		}
		else
		{
		  return false;
		}
	  }
	}

}