/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
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
	/// <see cref="Org.Apache.Lucene.Search.NumericRangeQuery{T}">Org.Apache.Lucene.Search.NumericRangeQuery&lt;T&gt;
	/// 	</see>
	/// . The table below specifies the required
	/// attributes and the defaults if optional attributes are omitted. For more
	/// detail on what each of the attributes actually do, consult the documentation
	/// for
	/// <see cref="Org.Apache.Lucene.Search.NumericRangeQuery{T}">Org.Apache.Lucene.Search.NumericRangeQuery&lt;T&gt;
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
	/// A
	/// <see cref="Org.Apache.Lucene.Queryparser.Xml.ParserException">Org.Apache.Lucene.Queryparser.Xml.ParserException
	/// 	</see>
	/// will be thrown if an error occurs parsing the
	/// supplied <tt>lowerTerm</tt> or <tt>upperTerm</tt> into the numeric type
	/// specified by <tt>type</tt>.
	/// </summary>
	public class NumericRangeQueryBuilder : QueryBuilder
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Xml.ParserException"></exception>
		public virtual Query GetQuery(Element e)
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
				Query filter;
				if (Sharpen.Runtime.EqualsIgnoreCase(type, "int"))
				{
					filter = NumericRangeQuery.NewIntRange(field, precisionStep, Sharpen.Extensions.ValueOf
						(lowerTerm), Sharpen.Extensions.ValueOf(upperTerm), lowerInclusive, upperInclusive
						);
				}
				else
				{
					if (Sharpen.Runtime.EqualsIgnoreCase(type, "long"))
					{
						filter = NumericRangeQuery.NewLongRange(field, precisionStep, Sharpen.Extensions.ValueOf
							(lowerTerm), Sharpen.Extensions.ValueOf(upperTerm), lowerInclusive, upperInclusive
							);
					}
					else
					{
						if (Sharpen.Runtime.EqualsIgnoreCase(type, "double"))
						{
							filter = NumericRangeQuery.NewDoubleRange(field, precisionStep, double.ValueOf(lowerTerm
								), double.ValueOf(upperTerm), lowerInclusive, upperInclusive);
						}
						else
						{
							if (Sharpen.Runtime.EqualsIgnoreCase(type, "float"))
							{
								filter = NumericRangeQuery.NewFloatRange(field, precisionStep, float.ValueOf(lowerTerm
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
				throw new ParserException("Could not parse lowerTerm or upperTerm into a number", 
					nfe);
			}
		}
	}
}
