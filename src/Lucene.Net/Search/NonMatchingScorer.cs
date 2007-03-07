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

namespace Lucene.Net.Search
{
	
	/// <summary>A scorer that matches no document at all. </summary>
	class NonMatchingScorer : Scorer
	{
		public NonMatchingScorer() : base(null)
		{
		} // no similarity used
		
		public override int Doc()
		{
			throw new System.NotSupportedException();
		}
		
		public override bool Next()
		{
			return false;
		}
		
		public override float Score()
		{
			throw new System.NotSupportedException();
		}
		
		public override bool SkipTo(int target)
		{
			return false;
		}
		
		public override Explanation Explain(int doc)
		{
			Explanation e = new Explanation();
			e.SetDescription("No document matches.");
			return e;
		}
	}
}