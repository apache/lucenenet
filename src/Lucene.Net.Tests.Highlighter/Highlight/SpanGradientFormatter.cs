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
	/// Formats text with different color intensity depending on the score of the
	/// term using the span tag.
	/// </summary>
	/// <remarks>
	/// Formats text with different color intensity depending on the score of the
	/// term using the span tag.  GradientFormatter uses a bgcolor argument to the font tag which
	/// doesn't work in Mozilla, thus this class.
	/// </remarks>
	/// <seealso cref="GradientFormatter">GradientFormatter</seealso>
	public class SpanGradientFormatter : GradientFormatter
	{
		public SpanGradientFormatter(float maxScore, string minForegroundColor, string maxForegroundColor
			, string minBackgroundColor, string maxBackgroundColor) : base(maxScore, minForegroundColor
			, maxForegroundColor, minBackgroundColor, maxBackgroundColor)
		{
		}

		public override string HighlightTerm(string originalText, TokenGroup tokenGroup)
		{
			if (tokenGroup.GetTotalScore() == 0)
			{
				return originalText;
			}
			float score = tokenGroup.GetTotalScore();
			if (score == 0)
			{
				return originalText;
			}
			// try to size sb correctly
			StringBuilder sb = new StringBuilder(originalText.Length + EXTRA);
			sb.Append("<span style=\"");
			if (highlightForeground)
			{
				sb.Append("color: ");
				sb.Append(GetForegroundColorString(score));
				sb.Append("; ");
			}
			if (highlightBackground)
			{
				sb.Append("background: ");
				sb.Append(GetBackgroundColorString(score));
				sb.Append("; ");
			}
			sb.Append("\">");
			sb.Append(originalText);
			sb.Append("</span>");
			return sb.ToString();
		}

		private static readonly string TEMPLATE = "<span style=\"background: #EEEEEE; color: #000000;\">...</span>";

		private static readonly int EXTRA = TEMPLATE.Length;
		// guess how much extra text we'll add to the text we're highlighting to try to avoid a  StringBuilder resize
	}
}
