using System.Diagnostics;
using System.Collections.Generic;

namespace org.apache.lucene.document
{

	/// <summary>
	/// Copyright 2004 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>

	using Analyzer = org.apache.lucene.analysis.Analyzer;
	using TokenStream = org.apache.lucene.analysis.TokenStream;
	using FieldInfo = org.apache.lucene.index.FieldInfo;
	using IndexReader = org.apache.lucene.index.IndexReader;
	using IndexableField = org.apache.lucene.index.IndexableField;
	using IndexableFieldType = org.apache.lucene.index.IndexableFieldType;
	using BytesRef = org.apache.lucene.util.BytesRef;

	/// <summary>
	/// Defers actually loading a field's value until you ask
	///  for it.  You must not use the returned Field instances
	///  after the provided reader has been closed. </summary>
	/// <seealso cref= #getField </seealso>
	public class LazyDocument
	{
	  private readonly IndexReader reader;
	  private readonly int docID;

	  // null until first field is loaded
	  private Document doc;

	  private IDictionary<int?, IList<LazyField>> fields = new Dictionary<int?, IList<LazyField>>();
	  private HashSet<string> fieldNames = new HashSet<string>();

	  public LazyDocument(IndexReader reader, int docID)
	  {
		this.reader = reader;
		this.docID = docID;
	  }

	  /// <summary>
	  /// Creates an IndexableField whose value will be lazy loaded if and 
	  /// when it is used. 
	  /// <para>
	  /// <b>NOTE:</b> This method must be called once for each value of the field 
	  /// name specified in sequence that the values exist.  This method may not be 
	  /// used to generate multiple, lazy, IndexableField instances refering to 
	  /// the same underlying IndexableField instance.
	  /// </para>
	  /// <para>
	  /// The lazy loading of field values from all instances of IndexableField 
	  /// objects returned by this method are all backed by a single Document 
	  /// per LazyDocument instance.
	  /// </para>
	  /// </summary>
	  public virtual IndexableField getField(FieldInfo fieldInfo)
	  {

		fieldNames.Add(fieldInfo.name);
		IList<LazyField> values = fields[fieldInfo.number];
		if (null == values)
		{
		  values = new List<>();
		  fields[fieldInfo.number] = values;
		}

		LazyField value = new LazyField(this, fieldInfo.name, fieldInfo.number);
		values.Add(value);

		lock (this)
		{
		  // edge case: if someone asks this LazyDoc for more LazyFields
		  // after other LazyFields from the same LazyDoc have been
		  // actuallized, we need to force the doc to be re-fetched
		  // so the new LazyFields are also populated.
		  doc = null;
		}
		return value;
	  }

	  /// <summary>
	  /// non-private for test only access
	  /// @lucene.internal 
	  /// </summary>
	  internal virtual Document Document
	  {
		  get
		  {
			  lock (this)
			  {
				if (doc == null)
				{
				  try
				  {
					doc = reader.document(docID, fieldNames);
				  }
				  catch (IOException ioe)
				  {
					throw new IllegalStateException("unable to load document", ioe);
				  }
				}
				return doc;
			  }
		  }
	  }

	  // :TODO: synchronize to prevent redundent copying? (sync per field name?)
	  private void fetchRealValues(string name, int fieldNum)
	  {
		Document d = Document;

		IList<LazyField> lazyValues = fields[fieldNum];
		IndexableField[] realValues = d.getFields(name);

		Debug.Assert(realValues.Length <= lazyValues.Count, "More lazy values then real values for field: " + name);

		for (int i = 0; i < lazyValues.Count; i++)
		{
		  LazyField f = lazyValues[i];
		  if (null != f)
		  {
			f.realValue = realValues[i];
		  }
		}
	  }


	  /// <summary>
	  /// @lucene.internal 
	  /// </summary>
	  public class LazyField : IndexableField
	  {
		  private readonly LazyDocument outerInstance;

		internal string name_Renamed;
		internal int fieldNum;
		internal volatile IndexableField realValue = null;

		internal LazyField(LazyDocument outerInstance, string name, int fieldNum)
		{
			this.outerInstance = outerInstance;
		  this.name_Renamed = name;
		  this.fieldNum = fieldNum;
		}

		/// <summary>
		/// non-private for test only access
		/// @lucene.internal 
		/// </summary>
		public virtual bool hasBeenLoaded()
		{
		  return null != realValue;
		}

		internal virtual IndexableField RealValue
		{
			get
			{
			  if (null == realValue)
			  {
				outerInstance.fetchRealValues(name_Renamed, fieldNum);
			  }
			  Debug.Assert(hasBeenLoaded(), "field value was not lazy loaded");
			  Debug.Assert(realValue.name().Equals(name()), "realvalue name != name: " + realValue.name() + " != " + name());
    
			  return realValue;
			}
		}

		public override string name()
		{
		  return name_Renamed;
		}

		public override float boost()
		{
		  return 1.0f;
		}

		public override BytesRef binaryValue()
		{
		  return RealValue.binaryValue();
		}

		public override string stringValue()
		{
		  return RealValue.stringValue();
		}

		public override Reader readerValue()
		{
		  return RealValue.readerValue();
		}

		public override Number numericValue()
		{
		  return RealValue.numericValue();
		}

		public override IndexableFieldType fieldType()
		{
		  return RealValue.fieldType();
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public org.apache.lucene.analysis.TokenStream tokenStream(org.apache.lucene.analysis.Analyzer analyzer) throws java.io.IOException
		public override TokenStream tokenStream(Analyzer analyzer)
		{
		  return RealValue.tokenStream(analyzer);
		}
	  }
	}

}