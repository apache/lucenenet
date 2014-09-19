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

using org.apache.lucene.queries.function;

namespace Lucene.Net.Queries.Function.ValueSources
{


	/// <summary>
	/// <code>ProductFloatFunction</code> returns the product of it's components.
	/// </summary>
	public class ProductFloatFunction : MultiFloatFunction
	{
	  public ProductFloatFunction(ValueSource[] sources) : base(sources)
	  {
	  }

	  protected internal override string name()
	  {
		return "product";
	  }

	  protected internal override float func(int doc, FunctionValues[] valsArr)
	  {
		float val = 1.0f;
		foreach (FunctionValues vals in valsArr)
		{
		  val *= vals.FloatVal(doc);
		}
		return val;
	  }
	}

}