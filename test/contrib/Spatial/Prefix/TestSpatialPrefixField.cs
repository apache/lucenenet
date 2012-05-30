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

using System;
using System.Collections.Generic;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Contrib.Spatial.Test.Prefix
{
	/// <summary>
	/// This is just a quick idea for *simple* tests
	/// </summary>
	public class TestSpatialPrefixField : LuceneTestCase
	{
		[Test]
		public void testRawTokens()
		{
			// Ignoring geometry for now, and focus on what tokens need to match

			var docA = new List<string> { "AAAAAA*", "AAAAAB+" };

			var docB = new List<string> { "A*", "BB*" };

			// Assumptions:
			checkQuery("AAAAA", "docA", "docB");
			checkQuery("AAAAA*", "docA", "docB"); // for now * and + are essentially identical
			checkQuery("AAAAA+", "docA", "docB"); // down the road, there may be a difference between 'covers' and an edge

			checkQuery("AA*", "docB", "docA"); // Bigger input query

			checkQuery("AAAAAAAAAAAA*", "docA", "docB"); // small

			checkQuery("BC"); // nothing
			checkQuery("XX"); // nothing

			// match only B
			checkQuery("B", "docB");
			checkQuery("BBBB", "docB");
			checkQuery("B*", "docB");
			checkQuery("BBBB*", "docB");
		}

		void checkQuery(String query, params String[] expect)
		{
			// TODO, check that the query returns the docs in order
		}

	}
}
