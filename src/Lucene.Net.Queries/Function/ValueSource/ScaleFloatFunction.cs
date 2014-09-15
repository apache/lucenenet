using System.Collections;
using System.Collections.Generic;

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

namespace org.apache.lucene.queries.function.valuesource
{

	using AtomicReaderContext = org.apache.lucene.index.AtomicReaderContext;
	using ReaderUtil = org.apache.lucene.index.ReaderUtil;
	using FloatDocValues = org.apache.lucene.queries.function.docvalues.FloatDocValues;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;


	/// <summary>
	/// Scales values to be between min and max.
	/// <para>This implementation currently traverses all of the source values to obtain
	/// their min and max.
	/// </para>
	/// <para>This implementation currently cannot distinguish when documents have been
	/// deleted or documents that have no value, and 0.0 values will be used for
	/// these cases.  This means that if values are normally all greater than 0.0, one can
	/// still end up with 0.0 as the min value to map from.  In these cases, an
	/// appropriate map() function could be used as a workaround to change 0.0
	/// to a value in the real range.
	/// </para>
	/// </summary>
	public class ScaleFloatFunction : ValueSource
	{
	  protected internal readonly ValueSource source;
	  protected internal readonly float min;
	  protected internal readonly float max;

	  public ScaleFloatFunction(ValueSource source, float min, float max)
	  {
		this.source = source;
		this.min = min;
		this.max = max;
	  }

	  public override string description()
	  {
		return "scale(" + source.description() + "," + min + "," + max + ")";
	  }

	  private class ScaleInfo
	  {
		internal float minVal;
		internal float maxVal;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private ScaleInfo createScaleInfo(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  private ScaleInfo createScaleInfo(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<org.apache.lucene.index.AtomicReaderContext> leaves = org.apache.lucene.index.ReaderUtil.getTopLevelContext(readerContext).leaves();
		IList<AtomicReaderContext> leaves = ReaderUtil.getTopLevelContext(readerContext).leaves();

		float minVal = float.PositiveInfinity;
		float maxVal = float.NegativeInfinity;

		foreach (AtomicReaderContext leaf in leaves)
		{
		  int maxDoc = leaf.reader().maxDoc();
		  FunctionValues vals = source.getValues(context, leaf);
		  for (int i = 0; i < maxDoc; i++)
		  {

		  float val = vals.floatVal(i);
		  if ((float.floatToRawIntBits(val) & (0xff << 23)) == 0xff << 23)
		  {
			// if the exponent in the float is all ones, then this is +Inf, -Inf or NaN
			// which don't make sense to factor into the scale function
			continue;
		  }
		  if (val < minVal)
		  {
			minVal = val;
		  }
		  if (val > maxVal)
		  {
			maxVal = val;
		  }
		  }
		}

		if (minVal == float.PositiveInfinity)
		{
		// must have been an empty index
		  minVal = maxVal = 0;
		}

		ScaleInfo scaleInfo = new ScaleInfo();
		scaleInfo.minVal = minVal;
		scaleInfo.maxVal = maxVal;
		context[ScaleFloatFunction.this] = scaleInfo;
		return scaleInfo;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {

		ScaleInfo scaleInfo = (ScaleInfo)context[ScaleFloatFunction.this];
		if (scaleInfo == null)
		{
		  scaleInfo = createScaleInfo(context, readerContext);
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float scale = (scaleInfo.maxVal-scaleInfo.minVal==0) ? 0 : (max-min)/(scaleInfo.maxVal-scaleInfo.minVal);
		float scale = (scaleInfo.maxVal - scaleInfo.minVal == 0) ? 0 : (max - min) / (scaleInfo.maxVal - scaleInfo.minVal);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float minSource = scaleInfo.minVal;
		float minSource = scaleInfo.minVal;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final float maxSource = scaleInfo.maxVal;
		float maxSource = scaleInfo.maxVal;

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues vals = source.getValues(context, readerContext);
		FunctionValues vals = source.getValues(context, readerContext);

		return new FloatDocValuesAnonymousInnerClassHelper(this, this, scale, minSource, maxSource, vals);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly ScaleFloatFunction outerInstance;

		  private float scale;
		  private float minSource;
		  private float maxSource;
		  private FunctionValues vals;

		  public FloatDocValuesAnonymousInnerClassHelper(ScaleFloatFunction outerInstance, org.apache.lucene.queries.function.valuesource.ScaleFloatFunction this, float scale, float minSource, float maxSource, FunctionValues vals) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.scale = scale;
			  this.minSource = minSource;
			  this.maxSource = maxSource;
			  this.vals = vals;
		  }

		  public override float floatVal(int doc)
		  {
			return (vals.floatVal(doc) - minSource) * scale + outerInstance.min;
		  }
		  public override string ToString(int doc)
		  {
			return "scale(" + vals.ToString(doc) + ",toMin=" + outerInstance.min + ",toMax=" + outerInstance.max + ",fromMin=" + minSource + ",fromMax=" + maxSource + ")";
		  }
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		source.createWeight(context, searcher);
	  }

	  public override int GetHashCode()
	  {
		int h = float.floatToIntBits(min);
		h = h * 29;
		h += float.floatToIntBits(max);
		h = h * 29;
		h += source.GetHashCode();
		return h;
	  }

	  public override bool Equals(object o)
	  {
		if (typeof(ScaleFloatFunction) != o.GetType())
		{
			return false;
		}
		ScaleFloatFunction other = (ScaleFloatFunction)o;
		return this.min == other.min && this.max == other.max && this.source.Equals(other.source);
	  }
	}

}