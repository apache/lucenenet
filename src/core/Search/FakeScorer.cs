using System.Collections.Generic;

namespace Lucene.Net.Search
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

	/// <summary>
	/// Used by <seealso cref="BulkScorer"/>s that need to pass a {@link
	///  Scorer} to <seealso cref="Collector#setScorer"/>. 
	/// </summary>
	public sealed class FakeScorer : Scorer
	{
	  internal float Score_Renamed;
	  internal int Doc = -1;
	  internal int Freq_Renamed = 1;

	  public FakeScorer() : base(null)
	  {
	  }

	  public override int Advance(int target)
	  {
		throw new System.NotSupportedException("FakeScorer doesn't support advance(int)");
	  }

	  public override int DocID()
	  {
		return Doc;
	  }

	  public override int Freq()
	  {
		return Freq_Renamed;
	  }

	  public override int NextDoc()
	  {
		throw new System.NotSupportedException("FakeScorer doesn't support nextDoc()");
	  }

	  public override float Score()
	  {
		return Score_Renamed;
	  }

	  public override long Cost()
	  {
		return 1;
	  }

	  public override Weight Weight
	  {
		  get
		  {
			throw new System.NotSupportedException();
		  }
	  }

	  public override ICollection<ChildScorer> Children
	  {
		  get
		  {
			throw new System.NotSupportedException();
		  }
	  }
	}

}