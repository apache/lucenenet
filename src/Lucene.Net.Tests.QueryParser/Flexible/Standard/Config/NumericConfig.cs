/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Document;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Config
{
	/// <summary>
	/// This class holds the configuration used to parse numeric queries and create
	/// <see cref="Org.Apache.Lucene.Search.NumericRangeQuery{T}">Org.Apache.Lucene.Search.NumericRangeQuery&lt;T&gt;
	/// 	</see>
	/// s.
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Search.NumericRangeQuery{T}">Org.Apache.Lucene.Search.NumericRangeQuery&lt;T&gt;
	/// 	</seealso>
	/// <seealso cref="Sharpen.NumberFormat">Sharpen.NumberFormat</seealso>
	public class NumericConfig
	{
		private int precisionStep;

		private NumberFormat format;

		private FieldType.NumericType type;

		/// <summary>
		/// Constructs a
		/// <see cref="NumericConfig">NumericConfig</see>
		/// object.
		/// </summary>
		/// <param name="precisionStep">the precision used to index the numeric values</param>
		/// <param name="format">
		/// the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to parse a
		/// <see cref="string">string</see>
		/// to
		/// <see cref="Sharpen.Number">Sharpen.Number</see>
		/// </param>
		/// <param name="type">the numeric type used to index the numeric values</param>
		/// <seealso cref="SetPrecisionStep(int)">SetPrecisionStep(int)</seealso>
		/// <seealso cref="SetNumberFormat(Sharpen.NumberFormat)">SetNumberFormat(Sharpen.NumberFormat)
		/// 	</seealso>
		/// <seealso cref="SetType(Org.Apache.Lucene.Document.FieldType.NumericType)">SetType(Org.Apache.Lucene.Document.FieldType.NumericType)
		/// 	</seealso>
		public NumericConfig(int precisionStep, NumberFormat format, FieldType.NumericType
			 type)
		{
			SetPrecisionStep(precisionStep);
			SetNumberFormat(format);
			SetType(type);
		}

		/// <summary>Returns the precision used to index the numeric values</summary>
		/// <returns>the precision used to index the numeric values</returns>
		/// <seealso cref="Org.Apache.Lucene.Search.NumericRangeQuery{T}.GetPrecisionStep()">Org.Apache.Lucene.Search.NumericRangeQuery&lt;T&gt;.GetPrecisionStep()
		/// 	</seealso>
		public virtual int GetPrecisionStep()
		{
			return precisionStep;
		}

		/// <summary>Sets the precision used to index the numeric values</summary>
		/// <param name="precisionStep">the precision used to index the numeric values</param>
		/// <seealso cref="Org.Apache.Lucene.Search.NumericRangeQuery{T}.GetPrecisionStep()">Org.Apache.Lucene.Search.NumericRangeQuery&lt;T&gt;.GetPrecisionStep()
		/// 	</seealso>
		public virtual void SetPrecisionStep(int precisionStep)
		{
			this.precisionStep = precisionStep;
		}

		/// <summary>
		/// Returns the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to parse a
		/// <see cref="string">string</see>
		/// to
		/// <see cref="Sharpen.Number">Sharpen.Number</see>
		/// </summary>
		/// <returns>
		/// the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to parse a
		/// <see cref="string">string</see>
		/// to
		/// <see cref="Sharpen.Number">Sharpen.Number</see>
		/// </returns>
		public virtual NumberFormat GetNumberFormat()
		{
			return format;
		}

		/// <summary>Returns the numeric type used to index the numeric values</summary>
		/// <returns>the numeric type used to index the numeric values</returns>
		public virtual FieldType.NumericType GetType()
		{
			return type;
		}

		/// <summary>Sets the numeric type used to index the numeric values</summary>
		/// <param name="type">the numeric type used to index the numeric values</param>
		public virtual void SetType(FieldType.NumericType type)
		{
			if (type == null)
			{
				throw new ArgumentException("type cannot be null!");
			}
			this.type = type;
		}

		/// <summary>
		/// Sets the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to parse a
		/// <see cref="string">string</see>
		/// to
		/// <see cref="Sharpen.Number">Sharpen.Number</see>
		/// </summary>
		/// <param name="format">
		/// the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to parse a
		/// <see cref="string">string</see>
		/// to
		/// <see cref="Sharpen.Number">Sharpen.Number</see>
		/// , cannot be <code>null</code>
		/// </param>
		public virtual void SetNumberFormat(NumberFormat format)
		{
			if (format == null)
			{
				throw new ArgumentException("format cannot be null!");
			}
			this.format = format;
		}

		public override bool Equals(object obj)
		{
			if (obj == this)
			{
				return true;
			}
			if (obj is Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig)
			{
				Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig other = (Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
					)obj;
				if (this.precisionStep == other.precisionStep && this.type == other.type && (this
					.format == other.format || (this.format.Equals(other.format))))
				{
					return true;
				}
			}
			return false;
		}
	}
}
