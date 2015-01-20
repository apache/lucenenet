/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Queries.Function.Docvalues;
using Org.Apache.Lucene.Queries.Function.Valuesource;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queries.Function.Valuesource
{
	/// <summary>
	/// An implementation for retrieving
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionValues">Org.Apache.Lucene.Queries.Function.FunctionValues
	/// 	</see>
	/// instances for string based fields.
	/// </summary>
	public class BytesRefFieldSource : FieldCacheSource
	{
		public BytesRefFieldSource(string field) : base(field)
		{
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FunctionValues GetValues(IDictionary context, AtomicReaderContext
			 readerContext)
		{
			FieldInfo fieldInfo = ((AtomicReader)readerContext.Reader()).GetFieldInfos().FieldInfo
				(field);
			// To be sorted or not to be sorted, that is the question
			// TODO: do it cleaner?
			if (fieldInfo != null && fieldInfo.GetDocValuesType() == FieldInfo.DocValuesType.
				BINARY)
			{
				BinaryDocValues binaryValues = FieldCache.DEFAULT.GetTerms(((AtomicReader)readerContext
					.Reader()), field, true);
				Bits docsWithField = FieldCache.DEFAULT.GetDocsWithField(((AtomicReader)readerContext
					.Reader()), field);
				return new _FunctionValues_50(this, docsWithField, binaryValues);
			}
			else
			{
				return new _DocTermsIndexDocValues_81(this, this, readerContext, field);
			}
		}

		private sealed class _FunctionValues_50 : FunctionValues
		{
			public _FunctionValues_50(BytesRefFieldSource _enclosing, Bits docsWithField, BinaryDocValues
				 binaryValues)
			{
				this._enclosing = _enclosing;
				this.docsWithField = docsWithField;
				this.binaryValues = binaryValues;
			}

			public override bool Exists(int doc)
			{
				return docsWithField.Get(doc);
			}

			public override bool BytesVal(int doc, BytesRef target)
			{
				binaryValues.Get(doc, target);
				return target.length > 0;
			}

			public override string StrVal(int doc)
			{
				BytesRef bytes = new BytesRef();
				return this.BytesVal(doc, bytes) ? bytes.Utf8ToString() : null;
			}

			public override object ObjectVal(int doc)
			{
				return this.StrVal(doc);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + '=' + this.StrVal(doc);
			}

			private readonly BytesRefFieldSource _enclosing;

			private readonly Bits docsWithField;

			private readonly BinaryDocValues binaryValues;
		}

		private sealed class _DocTermsIndexDocValues_81 : DocTermsIndexDocValues
		{
			public _DocTermsIndexDocValues_81(BytesRefFieldSource _enclosing, ValueSource baseArg1
				, AtomicReaderContext baseArg2, string baseArg3) : base(baseArg1, baseArg2, baseArg3
				)
			{
				this._enclosing = _enclosing;
			}

			protected internal override string ToTerm(string readableValue)
			{
				return readableValue;
			}

			public override object ObjectVal(int doc)
			{
				return this.StrVal(doc);
			}

			public override string ToString(int doc)
			{
				return this._enclosing.Description() + '=' + this.StrVal(doc);
			}

			private readonly BytesRefFieldSource _enclosing;
		}
	}
}
