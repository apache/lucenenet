/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.IO;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Document;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Document
{
	/// <summary>
	/// Defers actually loading a field's value until you ask
	/// for it.
	/// </summary>
	/// <remarks>
	/// Defers actually loading a field's value until you ask
	/// for it.  You must not use the returned Field instances
	/// after the provided reader has been closed.
	/// </remarks>
	/// <seealso cref="GetField(Org.Apache.Lucene.Index.FieldInfo)">GetField(Org.Apache.Lucene.Index.FieldInfo)
	/// 	</seealso>
	public class LazyDocument
	{
		private readonly IndexReader reader;

		private readonly int docID;

		private Org.Apache.Lucene.Document.Document doc;

		private IDictionary<int, IList<LazyDocument.LazyField>> fields = new Dictionary<int
			, IList<LazyDocument.LazyField>>();

		private ICollection<string> fieldNames = new HashSet<string>();

		public LazyDocument(IndexReader reader, int docID)
		{
			// null until first field is loaded
			this.reader = reader;
			this.docID = docID;
		}

		/// <summary>
		/// Creates an IndexableField whose value will be lazy loaded if and
		/// when it is used.
		/// </summary>
		/// <remarks>
		/// Creates an IndexableField whose value will be lazy loaded if and
		/// when it is used.
		/// <p>
		/// <b>NOTE:</b> This method must be called once for each value of the field
		/// name specified in sequence that the values exist.  This method may not be
		/// used to generate multiple, lazy, IndexableField instances refering to
		/// the same underlying IndexableField instance.
		/// </p>
		/// <p>
		/// The lazy loading of field values from all instances of IndexableField
		/// objects returned by this method are all backed by a single Document
		/// per LazyDocument instance.
		/// </p>
		/// </remarks>
		public virtual IndexableField GetField(FieldInfo fieldInfo)
		{
			fieldNames.AddItem(fieldInfo.name);
			IList<LazyDocument.LazyField> values = fields.Get(fieldInfo.number);
			if (null == values)
			{
				values = new AList<LazyDocument.LazyField>();
				fields.Put(fieldInfo.number, values);
			}
			LazyDocument.LazyField value = new LazyDocument.LazyField(this, fieldInfo.name, fieldInfo
				.number);
			values.AddItem(value);
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

		/// <summary>non-private for test only access</summary>
		/// <lucene.internal></lucene.internal>
		internal virtual Org.Apache.Lucene.Document.Document GetDocument()
		{
			lock (this)
			{
				if (doc == null)
				{
					try
					{
						doc = reader.Document(docID, fieldNames);
					}
					catch (IOException ioe)
					{
						throw new InvalidOperationException("unable to load document", ioe);
					}
				}
				return doc;
			}
		}

		// :TODO: synchronize to prevent redundent copying? (sync per field name?)
		private void FetchRealValues(string name, int fieldNum)
		{
			Org.Apache.Lucene.Document.Document d = GetDocument();
			IList<LazyDocument.LazyField> lazyValues = fields.Get(fieldNum);
			IndexableField[] realValues = d.GetFields(name);
			//HM:revisit
			for (int i = 0; i < lazyValues.Count; i++)
			{
				LazyDocument.LazyField f = lazyValues[i];
				if (null != f)
				{
					f.realValue = realValues[i];
				}
			}
		}

		/// <lucene.internal></lucene.internal>
		public class LazyField : IndexableField
		{
			private string name;

			private int fieldNum;

			internal volatile IndexableField realValue = null;

			private LazyField(LazyDocument _enclosing, string name, int fieldNum)
			{
				this._enclosing = _enclosing;
				this.name = name;
				this.fieldNum = fieldNum;
			}

			/// <summary>non-private for test only access</summary>
			/// <lucene.internal></lucene.internal>
			public virtual bool HasBeenLoaded()
			{
				return null != this.realValue;
			}

			private IndexableField GetRealValue()
			{
				if (null == this.realValue)
				{
					this._enclosing.FetchRealValues(this.name, this.fieldNum);
				}
				//HM:assert
				return this.realValue;
			}

			public virtual string Name()
			{
				return this.name;
			}

			public virtual float Boost()
			{
				return 1.0f;
			}

			public virtual BytesRef BinaryValue()
			{
				return this.GetRealValue().BinaryValue();
			}

			public virtual string StringValue()
			{
				return this.GetRealValue().StringValue();
			}

			public virtual StreamReader ReaderValue()
			{
				return this.GetRealValue().ReaderValue();
			}

			public virtual Number NumericValue()
			{
				return this.GetRealValue().NumericValue();
			}

			public virtual IndexableFieldType FieldType()
			{
				return this.GetRealValue().FieldType();
			}

			/// <exception cref="System.IO.IOException"></exception>
			public virtual Org.Apache.Lucene.Analysis.TokenStream TokenStream(Analyzer analyzer
				)
			{
				return this.GetRealValue().TokenStream(analyzer);
			}

			private readonly LazyDocument _enclosing;
		}
	}
}
