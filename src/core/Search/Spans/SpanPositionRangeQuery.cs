using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Search.Spans
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


	using ToStringUtils = Lucene.Net.Util.ToStringUtils;


	/// <summary>
	/// Checks to see if the <seealso cref="#getMatch()"/> lies between a start and end position
	/// </summary>
	/// <seealso cref= Lucene.Net.Search.Spans.SpanFirstQuery for a derivation that is optimized for the case where start position is 0 </seealso>
	public class SpanPositionRangeQuery : SpanPositionCheckQuery
	{
	  protected internal int Start_Renamed = 0;
	  protected internal int End_Renamed;

	  public SpanPositionRangeQuery(SpanQuery match, int start, int end) : base(match)
	  {
		this.Start_Renamed = start;
		this.End_Renamed = end;
	  }


	  protected internal override AcceptStatus AcceptPosition(Spans spans)
	  {
		Debug.Assert(spans.Start() != spans.End());
		if (spans.Start() >= End_Renamed)
		{
		  return AcceptStatus.NO_AND_ADVANCE;
		}
		else if (spans.Start() >= Start_Renamed && spans.End() <= End_Renamed)
		{
		  return AcceptStatus.YES;
		}
		else
		{
		  return AcceptStatus.NO;
		}
	  }


	  /// <returns> The minimum position permitted in a match </returns>
	  public virtual int Start
	  {
		  get
		  {
			return Start_Renamed;
		  }
	  }

	  /// <returns> the maximum end position permitted in a match. </returns>
	  public virtual int End
	  {
		  get
		  {
			return End_Renamed;
		  }
	  }

	  public override string ToString(string field)
	  {
		StringBuilder buffer = new StringBuilder();
		buffer.Append("spanPosRange(");
		buffer.Append(Match_Renamed.ToString(field));
		buffer.Append(", ").Append(Start_Renamed).Append(", ");
		buffer.Append(End_Renamed);
		buffer.Append(")");
		buffer.Append(ToStringUtils.Boost(Boost));
		return buffer.ToString();
	  }

	  public override SpanPositionRangeQuery Clone()
	  {
		SpanPositionRangeQuery result = new SpanPositionRangeQuery((SpanQuery) Match_Renamed.Clone(), Start_Renamed, End_Renamed);
		result.Boost = Boost;
		return result;
	  }

	  public override bool Equals(object o)
	  {
		if (this == o)
		{
			return true;
		}
		if (!(o is SpanPositionRangeQuery))
		{
			return false;
		}

		SpanPositionRangeQuery other = (SpanPositionRangeQuery)o;
		return this.End_Renamed == other.End_Renamed && this.Start_Renamed == other.Start_Renamed && this.Match_Renamed.Equals(other.Match_Renamed) && this.Boost == other.Boost;
	  }

	  public override int HashCode()
	  {
		int h = Match_Renamed.HashCode();
		h ^= (h << 8) | ((int)((uint)h >> 25)); // reversible
		h ^= float.floatToRawIntBits(Boost) ^ End_Renamed ^ Start_Renamed;
		return h;
	  }

	}
}