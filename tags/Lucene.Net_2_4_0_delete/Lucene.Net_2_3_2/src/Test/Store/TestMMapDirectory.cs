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

using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Store
{
	
	[TestFixture]
	public class TestMMapDirectory : LuceneTestCase
	{
		
		// Simply verify that if there is a method in FSDirectory
		// that returns IndexInput or a subclass, that
		// MMapDirectory overrides it.
		[Test]
		public virtual void  TestIndexInputMethods()
		{
			System.Type FSDirectory = System.Type.GetType("Lucene.Net.Store.FSDirectory,Lucene.Net");
			System.Type IndexInput = System.Type.GetType("Lucene.Net.Store.IndexInput,Lucene.Net");
			System.Type MMapDirectory = System.Type.GetType("Lucene.Net.Store.MMapDirectory,Lucene.Net");
			
			//System.Type FSDirectory = System.Type.GetType("Lucene.Net.Store.FSDirectory");
			//System.Type IndexInput = System.Type.GetType("Lucene.Net.Store.IndexInput");
			//System.Type MMapDirectory = System.Type.GetType("Lucene.Net.Store.MMapDirectory");
			
			System.Reflection.MethodInfo[] methods = FSDirectory.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Static);
			for (int i = 0; i < methods.Length; i++)
			{
				System.Reflection.MethodInfo method = methods[i];
				if (IndexInput.IsAssignableFrom(method.ReturnType))
				{
					// There is a method that returns IndexInput or a
					// subclass of IndexInput
					try
					{
						System.Reflection.ParameterInfo[] parameters = method.GetParameters();
						System.Type[] types = new System.Type[parameters.Length];
						for (int j = 0; j < types.Length; j++)
						{
							types[j] = parameters[j].ParameterType;
						}
						System.Reflection.MethodInfo m = MMapDirectory.GetMethod(method.Name, types);

						if (m.DeclaringType != MMapDirectory)
						{
							Assert.Fail("FSDirectory has method " + method + " but MMapDirectory does not override");
						}
					}
					catch (System.MethodAccessException)
					{
						// Should not happen
						Assert.Fail("unexpected NoSuchMethodException");
					}
				}
			}
		}
	}
}