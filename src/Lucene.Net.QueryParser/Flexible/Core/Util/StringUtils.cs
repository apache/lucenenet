/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Util;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Util
{
	/// <summary>String manipulation routines</summary>
	public sealed class StringUtils
	{
		public static string ToString(object obj)
		{
			if (obj != null)
			{
				return obj.ToString();
			}
			else
			{
				return null;
			}
		}
	}
}
