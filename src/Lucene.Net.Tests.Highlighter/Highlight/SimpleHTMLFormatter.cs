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
	/// <see cref="Formatter">Formatter</see>
	/// implementation to highlight terms with a pre and
	/// post tag.
	/// </summary>
	public class SimpleHTMLFormatter : Formatter
	{
		private static readonly string DEFAULT_PRE_TAG = "<B>";

		private static readonly string DEFAULT_POST_TAG = "</B>";

		private string preTag;

		private string postTag;

		public SimpleHTMLFormatter(string preTag, string postTag)
		{
			this.preTag = preTag;
			this.postTag = postTag;
		}

		/// <summary>Default constructor uses HTML: &lt;B&gt; tags to markup terms.</summary>
		/// <remarks>Default constructor uses HTML: &lt;B&gt; tags to markup terms.</remarks>
		public SimpleHTMLFormatter() : this(DEFAULT_PRE_TAG, DEFAULT_POST_TAG)
		{
		}

		public virtual string HighlightTerm(string originalText, TokenGroup tokenGroup)
		{
			if (tokenGroup.GetTotalScore() <= 0)
			{
				return originalText;
			}
			// Allocate StringBuilder with the right number of characters from the
			// beginning, to avoid char[] allocations in the middle of appends.
			StringBuilder returnBuffer = new StringBuilder(preTag.Length + originalText.Length
				 + postTag.Length);
			returnBuffer.Append(preTag);
			returnBuffer.Append(originalText);
			returnBuffer.Append(postTag);
			return returnBuffer.ToString();
		}
	}
}
