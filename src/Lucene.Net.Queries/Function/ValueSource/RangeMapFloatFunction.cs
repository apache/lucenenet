using System.Collections;

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
	using FloatDocValues = org.apache.lucene.queries.function.docvalues.FloatDocValues;
	using IndexSearcher = org.apache.lucene.search.IndexSearcher;


	/// <summary>
	/// <code>RangeMapFloatFunction</code> implements a map function over
	/// another <seealso cref="ValueSource"/> whose values fall within min and max inclusive to target.
	/// <br>
	/// Normally Used as an argument to a <seealso cref="org.apache.lucene.queries.function.FunctionQuery"/>
	/// 
	/// 
	/// </summary>
	public class RangeMapFloatFunction : ValueSource
	{
	  protected internal readonly ValueSource source;
	  protected internal readonly float min;
	  protected internal readonly float max;
	  protected internal readonly ValueSource target;
	  protected internal readonly ValueSource defaultVal;

	  public RangeMapFloatFunction(ValueSource source, float min, float max, float target, float? def) : this(source, min, max, new ConstValueSource(target), def == null ? null : new ConstValueSource(def.Value))
	  {
	  }

	  public RangeMapFloatFunction(ValueSource source, float min, float max, ValueSource target, ValueSource def)
	  {
		this.source = source;
		this.min = min;
		this.max = max;
		this.target = target;
		this.defaultVal = def;
	  }

	  public override string description()
	  {
		return "map(" + source.description() + "," + min + "," + max + "," + target.description() + ")";
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues vals = source.getValues(context, readerContext);
		FunctionValues vals = source.getValues(context, readerContext);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues targets = target.getValues(context, readerContext);
		FunctionValues targets = target.getValues(context, readerContext);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.queries.function.FunctionValues defaults = (this.defaultVal == null) ? null : defaultVal.getValues(context, readerContext);
		FunctionValues defaults = (this.defaultVal == null) ? null : defaultVal.getValues(context, readerContext);
		return new FloatDocValuesAnonymousInnerClassHelper(this, this, vals, targets, defaults);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly RangeMapFloatFunction outerInstance;

		  private FunctionValues vals;
		  private FunctionValues targets;
		  private FunctionValues defaults;

		  public FloatDocValuesAnonymousInnerClassHelper(RangeMapFloatFunction outerInstance, org.apache.lucene.queries.function.valuesource.RangeMapFloatFunction this, FunctionValues vals, FunctionValues targets, FunctionValues defaults) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.vals = vals;
			  this.targets = targets;
			  this.defaults = defaults;
		  }

		  public override float floatVal(int doc)
		  {
			float val = vals.floatVal(doc);
			return (val >= outerInstance.min && val <= outerInstance.max) ? targets.floatVal(doc) : (outerInstance.defaultVal == null ? val : defaults.floatVal(doc));
		  }
		  public override string ToString(int doc)
		  {
			return "map(" + vals.ToString(doc) + ",min=" + outerInstance.min + ",max=" + outerInstance.max + ",target=" + targets.ToString(doc) + ")";
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
		int h = source.GetHashCode();
		h ^= (h << 10) | ((int)((uint)h >> 23));
		h += float.floatToIntBits(min);
		h ^= (h << 14) | ((int)((uint)h >> 19));
		h += float.floatToIntBits(max);
		h += target.GetHashCode();
		if (defaultVal != null)
		{
		  h += defaultVal.GetHashCode();
		}
		return h;
	  }

	  public override bool Equals(object o)
	  {
		if (typeof(RangeMapFloatFunction) != o.GetType())
		{
			return false;
		}
		RangeMapFloatFunction other = (RangeMapFloatFunction)o;
		return this.min == other.min && this.max == other.max && this.target.Equals(other.target) && this.source.Equals(other.source) && (this.defaultVal == other.defaultVal || (this.defaultVal != null && this.defaultVal.Equals(other.defaultVal)));
	  }
	}

}