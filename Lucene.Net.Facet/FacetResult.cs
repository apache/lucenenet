using System.Text;
using Lucene.Net.Support;

namespace Lucene.Net.Facet
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
	/// Counts or aggregates for a single dimension. </summary>
	public sealed class FacetResult
	{

	  /// <summary>
	  /// Dimension that was requested. </summary>
	  public readonly string dim;

	  /// <summary>
	  /// Path whose children were requested. </summary>
	  public readonly string[] path;

	  /// <summary>
	  /// Total value for this path (sum of all child counts, or
	  ///  sum of all child values), even those not included in
	  ///  the topN. 
	  /// </summary>
	  public readonly float value;

	  /// <summary>
	  /// How many child labels were encountered. </summary>
	  public readonly int childCount;

	  /// <summary>
	  /// Child counts. </summary>
	  public readonly LabelAndValue[] labelValues;

	  /// <summary>
	  /// Sole constructor. </summary>
	  public FacetResult(string dim, string[] path, float value, LabelAndValue[] labelValues, int childCount)
	  {
		this.dim = dim;
		this.path = path;
		this.value = value;
		this.labelValues = labelValues;
		this.childCount = childCount;
	  }

	  public override string ToString()
	  {
		StringBuilder sb = new StringBuilder();
		sb.Append("dim=");
		sb.Append(dim);
		sb.Append(" path=");
		sb.Append("[" + Arrays.ToString(path) + "]");
		sb.Append(" value=");
		sb.Append(value);
		sb.Append(" childCount=");
		sb.Append(childCount);
		sb.Append('\n');
		foreach (LabelAndValue labelValue in labelValues)
		{
		  sb.Append("  " + labelValue + "\n");
		}
		return sb.ToString();
	  }

	  public override bool Equals(object _other)
	  {
		if ((_other is FacetResult) == false)
		{
		  return false;
		}
		FacetResult other = (FacetResult) _other;
		return value.Equals(other.value) && childCount == other.childCount && Arrays.Equals(labelValues, other.labelValues);
	  }

	  public override int GetHashCode()
	  {
		int hashCode = value.GetHashCode() + 31 * childCount;
		foreach (LabelAndValue labelValue in labelValues)
		{
		  hashCode = labelValue.GetHashCode() + 31 * hashCode;
		}
		return hashCode;
	  }
	}

}