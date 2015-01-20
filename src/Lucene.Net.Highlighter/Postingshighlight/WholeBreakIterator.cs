/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Search.Postingshighlight;
using Sharpen;

namespace Lucene.Net.Search.Postingshighlight
{
	/// <summary>Just produces one single fragment for the entire text</summary>
	public sealed class WholeBreakIterator : BreakIterator
	{
		private CharacterIterator text;

		private int start;

		private int end;

		private int current;

		public override int Current()
		{
			return current;
		}

		public override int First()
		{
			return (current = start);
		}

		public override int Following(int pos)
		{
			if (pos < start || pos > end)
			{
				throw new ArgumentException("offset out of bounds");
			}
			else
			{
				if (pos == end)
				{
					// this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
					// http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=9000909
					current = end;
					return DONE;
				}
				else
				{
					return Last();
				}
			}
		}

		public override CharacterIterator GetText()
		{
			return text;
		}

		public override int Last()
		{
			return (current = end);
		}

		public override int Next()
		{
			if (current == end)
			{
				return DONE;
			}
			else
			{
				return Last();
			}
		}

		public override int Next(int n)
		{
			if (n < 0)
			{
				for (int i = 0; i < -n; i++)
				{
					Previous();
				}
			}
			else
			{
				for (int i = 0; i < n; i++)
				{
					Next();
				}
			}
			return Current();
		}

		public override int Preceding(int pos)
		{
			if (pos < start || pos > end)
			{
				throw new ArgumentException("offset out of bounds");
			}
			else
			{
				if (pos == start)
				{
					// this conflicts with the javadocs, but matches actual behavior (Oracle has a bug in something)
					// http://bugs.sun.com/bugdatabase/view_bug.do?bug_id=9000909
					current = start;
					return DONE;
				}
				else
				{
					return First();
				}
			}
		}

		public override int Previous()
		{
			if (current == start)
			{
				return DONE;
			}
			else
			{
				return First();
			}
		}

		public override void SetText(CharacterIterator newText)
		{
			start = newText.GetBeginIndex();
			end = newText.GetEndIndex();
			text = newText;
			current = start;
		}
	}
}
