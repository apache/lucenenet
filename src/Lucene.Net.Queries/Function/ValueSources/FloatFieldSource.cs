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
using System.Collections;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function.DocValues;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Queries.Function.ValueSources
{
    /// <summary>
	/// Obtains float field values from <seealso cref="IFieldCache#getFloats"/> and makes those
	/// values available as other numeric types, casting as needed.
	/// </summary>
	public class FloatFieldSource : FieldCacheSource
	{

	  protected internal readonly FieldCache.FloatParser parser;

	  public FloatFieldSource(string field) : this(field, null)
	  {
	  }

	  public FloatFieldSource(string field, IFieldCache.FloatParser parser) : base(field)
	  {
		this.parser = parser;
	  }

        public override string Description
        {
            get { return "float(" + field + ')'; }
        }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues GetValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.search.FieldCache.Floats arr = cache.getFloats(readerContext.reader(), field, parser, true);
		FieldCache.Floats arr = cache.GetFloats(readerContext.AtomicReader, field, parser, true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits valid = cache.getDocsWithField(readerContext.reader(), field);
		Bits valid = cache.GetDocsWithField(readerContext.AtomicReader, field);

		return new FloatDocValuesAnonymousInnerClassHelper(this, this, arr, valid);
	  }

	  private class FloatDocValuesAnonymousInnerClassHelper : FloatDocValues
	  {
		  private readonly FloatFieldSource outerInstance;

		  private readonly FieldCache.Floats arr;
		  private readonly Bits valid;

		  public FloatDocValuesAnonymousInnerClassHelper(FloatFieldSource outerInstance, FloatFieldSource this, FieldCache.Floats arr, Bits valid) : base(this)
		  {
			  this.outerInstance = outerInstance;
			  this.arr = arr;
			  this.valid = valid;
		  }

		  public override float FloatVal(int doc)
		  {
			return arr.get(doc);
		  }

		  public override object objectVal(int doc)
		  {
			return valid.get(doc) ? arr.get(doc) : null;
		  }

		  public override bool exists(int doc)
		  {
			return arr.get(doc) != 0 || valid.get(doc);
		  }

		  public override ValueFiller ValueFiller
		  {
			  get
			  {
				return new ValueFillerAnonymousInnerClassHelper(this);
			  }
		  }

		  private class ValueFillerAnonymousInnerClassHelper : ValueFiller
		  {
			  private readonly FloatDocValuesAnonymousInnerClassHelper outerInstance;

			  public ValueFillerAnonymousInnerClassHelper(FloatDocValuesAnonymousInnerClassHelper outerInstance)
			  {
				  this.outerInstance = outerInstance;
				  mval = new MutableValueFloat();
			  }

			  private readonly MutableValueFloat mval;

			  public override MutableValue Value
			  {
				  get
				  {
					return mval;
				  }
			  }

			  public override void fillValue(int doc)
			  {
				mval.value = outerInstance.arr.get(doc);
				mval.exists = mval.value != 0 || outerInstance.valid.get(doc);
			  }
		  }

	  }

	  public override bool Equals(object o)
	  {
		if (o.GetType() != typeof(FloatFieldSource))
		{
			return false;
		}
		FloatFieldSource other = (FloatFieldSource)o;
		return base.Equals(other) && (this.parser == null ? other.parser == null : this.parser.GetType() == other.parser.GetType());
	  }

	  public override int GetHashCode()
	  {
		int h = parser == null ? typeof(float?).GetHashCode() : parser.GetType().GetHashCode();
		h += base.GetHashCode();
		return h;
	  }
	}

}