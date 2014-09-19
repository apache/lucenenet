using System.Collections;
using Lucene.Net.Queries.Function.DocValues;
using org.apache.lucene.queries.function;

namespace Lucene.Net.Queries.Function.ValueSources
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
	/// An implementation for retrieving <seealso cref="FunctionValues"/> instances for string based fields.
	/// </summary>
	public class BytesRefFieldSource : FieldCacheSource
	{

	  public BytesRefFieldSource(string field) : base(field)
	  {
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.queries.function.FunctionValues getValues(java.util.Map context, org.apache.lucene.index.AtomicReaderContext readerContext) throws java.io.IOException
	  public override FunctionValues getValues(IDictionary context, AtomicReaderContext readerContext)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.FieldInfo fieldInfo = readerContext.reader().getFieldInfos().fieldInfo(field);
		FieldInfo fieldInfo = readerContext.reader().FieldInfos.fieldInfo(field);
		// To be sorted or not to be sorted, that is the question
		// TODO: do it cleaner?
		if (fieldInfo != null && fieldInfo.DocValuesType == FieldInfo.DocValuesType.BINARY)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.index.BinaryDocValues binaryValues = org.apache.lucene.search.FieldCache.DEFAULT.getTerms(readerContext.reader(), field, true);
		  BinaryDocValues binaryValues = FieldCache.DEFAULT.getTerms(readerContext.reader(), field, true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.Bits docsWithField = org.apache.lucene.search.FieldCache.DEFAULT.getDocsWithField(readerContext.reader(), field);
		  Bits docsWithField = FieldCache.DEFAULT.getDocsWithField(readerContext.reader(), field);
		  return new FunctionValuesAnonymousInnerClassHelper(this, binaryValues, docsWithField);
		}
		else
		{
		  return new DocTermsIndexDocValuesAnonymousInnerClassHelper(this, this, readerContext, field);
		}
	  }

	  private class FunctionValuesAnonymousInnerClassHelper : FunctionValues
	  {
		  private readonly BytesRefFieldSource outerInstance;

		  private BinaryDocValues binaryValues;
		  private Bits docsWithField;

		  public FunctionValuesAnonymousInnerClassHelper(BytesRefFieldSource outerInstance, BinaryDocValues binaryValues, Bits docsWithField)
		  {
			  this.outerInstance = outerInstance;
			  this.binaryValues = binaryValues;
			  this.docsWithField = docsWithField;
		  }


		  public override bool exists(int doc)
		  {
			return docsWithField.get(doc);
		  }

		  public override bool bytesVal(int doc, BytesRef target)
		  {
			binaryValues.get(doc, target);
			return target.length > 0;
		  }

		  public override string strVal(int doc)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.BytesRef bytes = new org.apache.lucene.util.BytesRef();
			BytesRef bytes = new BytesRef();
			return bytesVal(doc, bytes) ? bytes.utf8ToString() : null;
		  }

		  public override object objectVal(int doc)
		  {
			return strVal(doc);
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.description() + '=' + strVal(doc);
		  }
	  }

	  private class DocTermsIndexDocValuesAnonymousInnerClassHelper : DocTermsIndexDocValues
	  {
		  private readonly BytesRefFieldSource outerInstance;

		  public DocTermsIndexDocValuesAnonymousInnerClassHelper(BytesRefFieldSource outerInstance, BytesRefFieldSource this, AtomicReaderContext readerContext, string field) : base(this, readerContext, field)
		  {
			  this.outerInstance = outerInstance;
		  }


		  protected internal override string toTerm(string readableValue)
		  {
			return readableValue;
		  }

		  public override object objectVal(int doc)
		  {
			return strVal(doc);
		  }

		  public override string ToString(int doc)
		  {
			return outerInstance.description() + '=' + strVal(doc);
		  }
	  }
	}

}