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
using System.Collections;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// A function with a single argument
	/// </summary>
	 public abstract class SingleFunction : ValueSource
	 {
	  protected internal readonly ValueSource source;

	  public SingleFunction(ValueSource source)
	  {
		this.source = source;
	  }

	  protected internal abstract string name();

	  public override string description()
	  {
		return name() + '(' + source.description() + ')';
	  }

	  public override int GetHashCode()
	  {
		return source.GetHashCode() + name().GetHashCode();
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
			return false;
		}
		SingleFunction other = (SingleFunction)o;
		return this.name().Equals(other.name()) && this.source.Equals(other.source);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		source.createWeight(context, searcher);
	  }
	 }
}