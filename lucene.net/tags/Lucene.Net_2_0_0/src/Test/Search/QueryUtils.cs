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
			Assert.IsTrue(!q1.Equals(q2));
			Assert.IsTrue(!q2.Equals(q1));
			
			// possible this test can fail on a hash collision... if that
			// happens, please change test to use a different example.
			Assert.IsTrue(q1.GetHashCode() != q2.GetHashCode());
		}
	}
}