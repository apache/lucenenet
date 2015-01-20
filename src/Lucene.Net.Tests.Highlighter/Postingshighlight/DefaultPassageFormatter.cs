/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Org.Apache.Lucene.Search.Postingshighlight;
using Sharpen;

namespace Org.Apache.Lucene.Search.Postingshighlight
{
	/// <summary>Creates a formatted snippet from the top passages.</summary>
	/// <remarks>
	/// Creates a formatted snippet from the top passages.
	/// <p>
	/// The default implementation marks the query terms as bold, and places
	/// ellipses between unconnected passages.
	/// </remarks>
	public class DefaultPassageFormatter : PassageFormatter
	{
		/// <summary>text that will appear before highlighted terms</summary>
		protected internal readonly string preTag;

		/// <summary>text that will appear after highlighted terms</summary>
		protected internal readonly string postTag;

		/// <summary>text that will appear between two unconnected passages</summary>
		protected internal readonly string ellipsis;

		/// <summary>true if we should escape for html</summary>
		protected internal readonly bool escape;

		/// <summary>Creates a new DefaultPassageFormatter with the default tags.</summary>
		/// <remarks>Creates a new DefaultPassageFormatter with the default tags.</remarks>
		public DefaultPassageFormatter() : this("<b>", "</b>", "... ", false)
		{
		}

		/// <summary>Creates a new DefaultPassageFormatter with custom tags.</summary>
		/// <remarks>Creates a new DefaultPassageFormatter with custom tags.</remarks>
		/// <param name="preTag">text which should appear before a highlighted term.</param>
		/// <param name="postTag">text which should appear after a highlighted term.</param>
		/// <param name="ellipsis">text which should be used to connect two unconnected passages.
		/// 	</param>
		/// <param name="escape">true if text should be html-escaped</param>
		public DefaultPassageFormatter(string preTag, string postTag, string ellipsis, bool
			 escape)
		{
			if (preTag == null || postTag == null || ellipsis == null)
			{
				throw new ArgumentNullException();
			}
			this.preTag = preTag;
			this.postTag = postTag;
			this.ellipsis = ellipsis;
			this.escape = escape;
		}

		public override object Format(Passage[] passages, string content)
		{
			StringBuilder sb = new StringBuilder();
			int pos = 0;
			foreach (Passage passage in passages)
			{
				// don't add ellipsis if its the first one, or if its connected.
				if (passage.startOffset > pos && pos > 0)
				{
					sb.Append(ellipsis);
				}
				pos = passage.startOffset;
				for (int i = 0; i < passage.numMatches; i++)
				{
					int start = passage.matchStarts[i];
					int end = passage.matchEnds[i];
					// its possible to have overlapping terms
					if (start > pos)
					{
						Append(sb, content, pos, start);
					}
					if (end > pos)
					{
						sb.Append(preTag);
						Append(sb, content, Math.Max(pos, start), end);
						sb.Append(postTag);
						pos = end;
					}
				}
				// its possible a "term" from the analyzer could span a sentence boundary.
				Append(sb, content, pos, Math.Max(pos, passage.endOffset));
				pos = passage.endOffset;
			}
			return sb.ToString();
		}

		/// <summary>Appends original text to the response.</summary>
		/// <remarks>Appends original text to the response.</remarks>
		/// <param name="dest">resulting text, possibly transformed or encoded</param>
		/// <param name="content">original text content</param>
		/// <param name="start">index of the first character in content</param>
		/// <param name="end">index of the character following the last character in content</param>
		protected internal virtual void Append(StringBuilder dest, string content, int start
			, int end)
		{
			if (escape)
			{
				// note: these are the rules from owasp.org
				for (int i = start; i < end; i++)
				{
					char ch = content[i];
					switch (ch)
					{
						case '&':
						{
							dest.Append("&amp;");
							break;
						}

						case '<':
						{
							dest.Append("&lt;");
							break;
						}

						case '>':
						{
							dest.Append("&gt;");
							break;
						}

						case '"':
						{
							dest.Append("&quot;");
							break;
						}

						case '\'':
						{
							dest.Append("&#x27;");
							break;
						}

						case '/':
						{
							dest.Append("&#x2F;");
							break;
						}

						default:
						{
							if (ch >= unchecked((int)(0x30)) && ch <= unchecked((int)(0x39)) || ch >= unchecked(
								(int)(0x41)) && ch <= unchecked((int)(0x5A)) || ch >= unchecked((int)(0x61)) && 
								ch <= unchecked((int)(0x7A)))
							{
								dest.Append(ch);
							}
							else
							{
								if (ch < unchecked((int)(0xff)))
								{
									dest.Append("&#");
									dest.Append((int)ch);
									dest.Append(";");
								}
								else
								{
									dest.Append(ch);
								}
							}
							break;
						}
					}
				}
			}
			else
			{
				dest.AppendRange(content, start, end);
			}
		}
	}
}
