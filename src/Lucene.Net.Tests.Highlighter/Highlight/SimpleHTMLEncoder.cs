/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Text;
using Org.Apache.Lucene.Search.Highlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// Simple
	/// <see cref="Encoder">Encoder</see>
	/// implementation to escape text for HTML output
	/// </summary>
	public class SimpleHTMLEncoder : Encoder
	{
		public SimpleHTMLEncoder()
		{
		}

		public virtual string EncodeText(string originalText)
		{
			return HtmlEncode(originalText);
		}

		/// <summary>Encode string into HTML</summary>
		public static string HtmlEncode(string plainText)
		{
			if (plainText == null || plainText.Length == 0)
			{
				return string.Empty;
			}
			StringBuilder result = new StringBuilder(plainText.Length);
			for (int index = 0; index < plainText.Length; index++)
			{
				char ch = plainText[index];
				switch (ch)
				{
					case '"':
					{
						result.Append("&quot;");
						break;
					}

					case '&':
					{
						result.Append("&amp;");
						break;
					}

					case '<':
					{
						result.Append("&lt;");
						break;
					}

					case '>':
					{
						result.Append("&gt;");
						break;
					}

					case '\'':
					{
						result.Append("&#x27;");
						break;
					}

					case '/':
					{
						result.Append("&#x2F;");
						break;
					}

					default:
					{
						result.Append(ch);
						break;
					}
				}
			}
			return result.ToString();
		}
	}
}
