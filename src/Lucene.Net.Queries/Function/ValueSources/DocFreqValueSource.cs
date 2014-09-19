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
using System.Collections;
using org.apache.lucene.queries.function;
using org.apache.lucene.queries.function.docvalues;

namespace Lucene.Net.Queries.Function.ValueSources
{
    internal class ConstIntDocValues : IntDocValues
	{
	  internal readonly int ival;
	  internal readonly float fval;
	  internal readonly double dval;
	  internal readonly long lval;
	  internal readonly string sval;
	  internal readonly ValueSource parent;

	  internal ConstIntDocValues(int val, ValueSource parent) : base(parent)
	  {
		ival = val;
		fval = val;
		dval = val;
		lval = val;
		sval = Convert.ToString(val);
		this.parent = parent;
	  }

	  public override float floatVal(int doc)
	  {
		return fval;
	  }
	  public override int intVal(int doc)
	  {
		return ival;
	  }
	  public override long longVal(int doc)
	  {
		return lval;
	  }
	  public override double doubleVal(int doc)
	  {
		return dval;
	  }
	  public override string strVal(int doc)
	  {
		return sval;
	  }
	  public override string ToString(int doc)
	  {
		return parent.description() + '=' + sval;
	  }
	}

	internal class ConstDoubleDocValues : DoubleDocValues
	{
	  internal readonly int ival;
	  internal readonly float fval;
	  internal readonly double dval;
	  internal readonly long lval;
	  internal readonly string sval;
	  internal readonly ValueSource parent;

	  internal ConstDoubleDocValues(double val, ValueSource parent) : base(parent)
	  {
		ival = (int)val;
		fval = (float)val;
		dval = val;
		lval = (long)val;
		sval = Convert.ToString(val);
		this.parent = parent;
	  }

	  public override float floatVal(int doc)
	  {
		return fval;
	  }
	  public override int intVal(int doc)
	  {
		return ival;
	  }
	  public override long longVal(int doc)
	  {
		return lval;
	  }
	  public override double doubleVal(int doc)
	  {
		return dval;
	  }
	  public override string strVal(int doc)
	  {
		return sval;
	  }
	  public override string ToString(int doc)
	  {
		return parent.description() + '=' + sval;
	  }
	}


	/// <summary>
	/// <code>DocFreqValueSource</code> returns the number of documents containing the term.
	/// @lucene.internal
	/// </summary>
	public class DocFreqValueSource : ValueSource
	{
	  protected internal readonly string field;
	  protected internal readonly string indexedField;
	  protected internal readonly string val;
	  protected internal readonly BytesRef indexedBytes;

	  public DocFreqValueSource(string field, string val, string indexedField, BytesRef indexedBytes)
	  {
		this.field = field;
		this.val = val;
		this.indexedField = indexedField;
		this.indexedBytes = indexedBytes;
	  }

	  public virtual string name()
	  {
		return "docfreq";
	  }

	  public override string description()
	  {
		return name() + '(' + field + ',' + val + ')';
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
		IndexSearcher searcher = (IndexSearcher)context["searcher"];
		int docfreq = searcher.IndexReader.docFreq(new Term(indexedField, indexedBytes));
		return new ConstIntDocValues(docfreq, this);
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public void createWeight(java.util.Map context, org.apache.lucene.search.IndexSearcher searcher) throws java.io.IOException
	  public override void createWeight(IDictionary context, IndexSearcher searcher)
	  {
		context["searcher"] = searcher;
	  }

	  public override int GetHashCode()
	  {
		return this.GetType().GetHashCode() + indexedField.GetHashCode() * 29 + indexedBytes.GetHashCode();
	  }

	  public override bool Equals(object o)
	  {
		if (this.GetType() != o.GetType())
		{
			return false;
		}
		DocFreqValueSource other = (DocFreqValueSource)o;
		return this.indexedField.Equals(other.indexedField) && this.indexedBytes.Equals(other.indexedBytes);
	  }
	}


}