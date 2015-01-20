/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Xml;
using Lucene.Net.Queryparser.Xml.Builders;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Org.W3c.Dom;
using Sharpen;

namespace Lucene.Net.Queryparser.Xml.Builders
{
	/// <summary>
	/// Builder for
	/// <see cref="Lucene.Net.Sandbox.Queries.DuplicateFilter">Lucene.Net.Sandbox.Queries.DuplicateFilter
	/// 	</see>
	/// </summary>
	public class DuplicateFilterBuilder : FilterBuilder
	{
		/// <exception cref="Lucene.Net.Queryparser.Xml.ParserException"></exception>
		public virtual Filter GetFilter(Element e)
		{
			string fieldName = DOMUtils.GetAttributeWithInheritanceOrFail(e, "fieldName");
			DuplicateFilter df = new DuplicateFilter(fieldName);
			string keepMode = DOMUtils.GetAttribute(e, "keepMode", "first");
			if (Sharpen.Runtime.EqualsIgnoreCase(keepMode, "first"))
			{
				df.SetKeepMode(DuplicateFilter.KeepMode.KM_USE_FIRST_OCCURRENCE);
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(keepMode, "last"))
				{
					df.SetKeepMode(DuplicateFilter.KeepMode.KM_USE_LAST_OCCURRENCE);
				}
				else
				{
					throw new ParserException("Illegal keepMode attribute in DuplicateFilter:" + keepMode
						);
				}
			}
			string processingMode = DOMUtils.GetAttribute(e, "processingMode", "full");
			if (Sharpen.Runtime.EqualsIgnoreCase(processingMode, "full"))
			{
				df.SetProcessingMode(DuplicateFilter.ProcessingMode.PM_FULL_VALIDATION);
			}
			else
			{
				if (Sharpen.Runtime.EqualsIgnoreCase(processingMode, "fast"))
				{
					df.SetProcessingMode(DuplicateFilter.ProcessingMode.PM_FAST_INVALIDATION);
				}
				else
				{
					throw new ParserException("Illegal processingMode attribute in DuplicateFilter:" 
						+ processingMode);
				}
			}
			return df;
		}
	}
}
