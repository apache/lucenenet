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

namespace Lucene.Net.Contrib.Spatial.Test
{
	/// <summary>
	/// Make sure we are reading the tests as expected
	/// </summary>
	public class TestTestFramework : LuceneTestCase
	{
  //      public void testQueries()
  //        {
  //  String name = StrategyTestCase.QTEST_Cities_IsWithin_BBox;

  //  InputStream @in = getClass().getClassLoader().getResourceAsStream(name);
  //  SpatialContext ctx = SimpleSpatialContext.GEO_KM;
  //  Iterator<SpatialTestQuery> iter = SpatialTestQuery.getTestQueries(
  //      new SpatialArgsParser(), ctx, name, in );
  //  List<SpatialTestQuery> tests = new ArrayList<SpatialTestQuery>();
  //  while( iter.hasNext() ) {
  //    tests.add( iter.next() );
  //  }
  //  Assert.assertEquals( 3, tests.size() );

  //  SpatialTestQuery sf = tests.get(0);
  // // assert
  //  Assert.assertEquals( 1, sf.ids.size() );
  //  Assert.assertTrue( sf.ids.get(0).equals( "G5391959" ) );
  //  Assert.assertTrue( sf.args.getShape() instanceof Rectangle);
  //  Assert.assertEquals( SpatialOperation.IsWithin, sf.args.getOperation() );
  //}
	}
}
