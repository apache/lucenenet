using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
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


	/*using ClassModel = com.carrotsearch.randomizedtesting.ClassModel;
	using MethodModel = com.carrotsearch.randomizedtesting.ClassModel.MethodModel;
	using TestMethodProvider = com.carrotsearch.randomizedtesting.TestMethodProvider;*/

	/// <summary>
	/// Backwards compatible test* method provider (public, non-static).
	/// </summary>
	public sealed class LuceneJUnit3MethodProvider : TestMethodProvider
	{
	  public override ICollection<Method> GetTestMethods(Type suiteClass, ClassModel classModel)
	  {
		IDictionary<Method, ClassModel.MethodModel> methods = classModel.Methods;
		List<Method> result = new List<Method>();
		foreach (ClassModel.MethodModel mm in methods.Values)
		{
		  // Skip any methods that have overrieds/ shadows.
		  if (mm.Down != null)
		  {
			  continue;
		  }

		  Method m = mm.element;
		  if (m.Name.StartsWith("test") && Modifier.isPublic(m.Modifiers) && !Modifier.isStatic(m.Modifiers) && m.ParameterTypes.length == 0)
		  {
			result.Add(m);
		  }
		}
		return result;
	  }
	}

}