/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Xml;
using Org.Apache.Lucene.Queryparser.Xml.Builders;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Org.W3c.Dom;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml.Builders
{
	/// <summary>
	/// Creates a
	/// <see cref="Org.Apache.Lucene.Search.NumericRangeFilter{T}">Org.Apache.Lucene.Search.NumericRangeFilter&lt;T&gt;
	/// 	</see>
	/// . The table below specifies the required
	/// attributes and the defaults if optional attributes are omitted. For more
	/// detail on what each of the attributes actually do, consult the documentation
	/// for
	/// <see cref="Org.Apache.Lucene.Search.NumericRangeFilter{T}">Org.Apache.Lucene.Search.NumericRangeFilter&lt;T&gt;
	/// 	</see>
	/// :
	/// <table>
	/// <tr>
	/// <th>Attribute name</th>
	/// <th>Values</th>
	/// <th>Required</th>
	/// <th>Default</th>
	/// </tr>
	/// <tr>
	/// <td>fieldName</td>
	/// <td>String</td>
	/// <td>Yes</td>
	/// <td>N/A</td>
	/// </tr>
	/// <tr>
	/// <td>lowerTerm</td>
	/// <td>Specified by <tt>type</tt></td>
	/// <td>Yes</td>
	/// <td>N/A</td>
	/// </tr>
	/// <tr>
	/// <td>upperTerm</td>
	/// <td>Specified by <tt>type</tt></td>
	/// <td>Yes</td>
	/// <td>N/A</td>
	/// </tr>
	/// <tr>
	/// <td>type</td>
	/// <td>int, long, float, double</td>
	/// <td>No</td>
	/// <td>int</td>
	/// </tr>
	/// <tr>
	/// <td>includeLower</td>
	/// <td>true, false</td>
	/// <td>No</td>
	/// <td>true</td>
	/// </tr>
	/// <tr>
	/// <td>includeUpper</td>
	/// <td>true, false</td>
	/// <td>No</td>
	/// <td>true</td>
	/// </tr>
	/// <tr>
	/// <td>precisionStep</td>
	/// <td>Integer</td>
	/// <td>No</td>
	/// <td>4</td>
	/// </tr>
	/// </table>
	/// <p/>
	/// If an error occurs parsing the supplied <tt>lowerTerm</tt> or
	/// <tt>upperTerm</tt> into the numeric type specified by <tt>type</tt>, then the
	/// error will be silently ignored and the resulting filter will not match any
	/// documents.
	/// </summary>
	public class NumericRangeFilterBuilder : FilterBuilder
	{
		private static readonly NumericRangeFilterBuilder.NoMatchFilter NO_MATCH_FILTER = 
			new NumericRangeFilterBuilder.NoMatchFilter();

		private bool strictMode = false;

		/// <summary>
		/// Specifies how this
		/// <see cref="NumericRangeFilterBuilder">NumericRangeFilterBuilder</see>
		/// will handle errors.
		/// <p/>
		/// If this is set to true,
		/// <see cref="GetFilter(Org.W3c.Dom.Element)">GetFilter(Org.W3c.Dom.Element)</see>
		/// will throw a
		/// <see cref="Org.Apache.Lucene.Queryparser.Xml.ParserException">Org.Apache.Lucene.Queryparser.Xml.ParserException
		/// 	</see>
		/// if it is unable to parse the lowerTerm or upperTerm
		/// into the appropriate numeric type. If this is set to false, then this
		/// exception will be silently ignored and the resulting filter will not match
		/// any documents.
		/// <p/>
		/// Defaults to false.
		/// </summary>
		public virtual void SetStrictMode(bool strictMode)
		{
			this.strictMode = strictMode;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Filter GetFilter(Element e)
		{
			string field = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			string lowerTerm = DOMUtils.GetAttributeOrFail(e, "lowerTerm");
			string upperTerm = DOMUtils.GetAttributeOrFail(e, "upperTerm");
			bool lowerInclusive = DOMUtils.GetAttribute(e, "includeLower", true);
			bool upperInclusive = DOMUtils.GetAttribute(e, "includeUpper", true);
			int precisionStep = DOMUtils.GetAttribute(e, "precisionStep", NumericUtils.PRECISION_STEP_DEFAULT
				);
			string type = DOMUtils.GetAttribute(e, "type", "int");
			try
			{
				Filter filter;
				if (Sharpen.Runtime.EqualsIgnoreCase(type, "int"))
				{
					filter = NumericRangeFilter.NewIntRange(field, precisionStep, Sharpen.Extensions.ValueOf
						(lowerTerm), Sharpen.Extensions.ValueOf(upperTerm), lowerInclusive, upperInclusive
						);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(type, "long"))
					{
						filter = NumericRangeFilter.NewLongRange(field, precisionStep, Sharpen.Extensions.ValueOf
							(lowerTerm), Sharpen.Extensions.ValueOf(upperTerm), lowerInclusive, upperInclusive
							);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(type, "double"))
						{
							filter = NumericRangeFilter.NewDoubleRange(field, precisionStep, double.ValueOf(lowerTerm
								), double.ValueOf(upperTerm), lowerInclusive, upperInclusive);
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(type, "float"))
							{
								filter = NumericRangeFilter.NewFloatRange(field, precisionStep, float.ValueOf(lowerTerm
									), float.ValueOf(upperTerm), lowerInclusive, upperInclusive);
							}
							else
							{
								throw new ParserException("type attribute must be one of: [long, int, double, float]"
									);
							}
						}
					}
				}
				return filter;
			}
			catch (FormatException nfe)
			{
				if (strictMode)
				{
					throw new ParserException("Could not parse lowerTerm or upperTerm into a number", 
						nfe);
				}
				return NO_MATCH_FILTER;
			}
		}

		internal class NoMatchFilter : Filter
		{
			/// <exception cref="System.IO.IOException"></exception>
			public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs
				)
			{
				return null;
			}
		}
	}
}
