/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Text;
using Org.Apache.Lucene.Analysis;
using Org.Apache.Lucene.Analysis.Tokenattributes;
using Org.Apache.Lucene.Search.Highlight;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>
	/// Class used to markup highlighted terms found in the best sections of a
	/// text, using configurable
	/// <see cref="Fragmenter">Fragmenter</see>
	/// ,
	/// <see cref="Scorer">Scorer</see>
	/// ,
	/// <see cref="Formatter">Formatter</see>
	/// ,
	/// <see cref="Encoder">Encoder</see>
	/// and tokenizers.
	/// </summary>
	public class Highlighter
	{
		public const int DEFAULT_MAX_CHARS_TO_ANALYZE = 50 * 1024;

		private int maxDocCharsToAnalyze = DEFAULT_MAX_CHARS_TO_ANALYZE;

		private Formatter formatter;

		private Encoder encoder;

		private Fragmenter textFragmenter = new SimpleFragmenter();

		private Scorer fragmentScorer = null;

		public Highlighter(Scorer fragmentScorer) : this(new SimpleHTMLFormatter(), fragmentScorer
			)
		{
		}

		public Highlighter(Formatter formatter, Scorer fragmentScorer) : this(formatter, 
			new DefaultEncoder(), fragmentScorer)
		{
		}

		public Highlighter(Formatter formatter, Encoder encoder, Scorer fragmentScorer)
		{
			this.formatter = formatter;
			this.encoder = encoder;
			this.fragmentScorer = fragmentScorer;
		}

		/// <summary>Highlights chosen terms in a text, extracting the most relevant section.
		/// 	</summary>
		/// <remarks>
		/// Highlights chosen terms in a text, extracting the most relevant section.
		/// This is a convenience method that calls
		/// <see cref="GetBestFragment(Org.Apache.Lucene.Analysis.TokenStream, string)">GetBestFragment(Org.Apache.Lucene.Analysis.TokenStream, string)
		/// 	</see>
		/// </remarks>
		/// <param name="analyzer">
		/// the analyzer that will be used to split <code>text</code>
		/// into chunks
		/// </param>
		/// <param name="text">text to highlight terms in</param>
		/// <param name="fieldName">Name of field used to influence analyzer's tokenization policy
		/// 	</param>
		/// <returns>highlighted text fragment or null if no terms found</returns>
		/// <exception cref="InvalidTokenOffsetsException">thrown if any token's endOffset exceeds the provided text's length
		/// 	</exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Org.Apache.Lucene.Search.Highlight.InvalidTokenOffsetsException"
		/// 	></exception>
		public string GetBestFragment(Analyzer analyzer, string fieldName, string text)
		{
			TokenStream tokenStream = analyzer.TokenStream(fieldName, text);
			return GetBestFragment(tokenStream, text);
		}

		/// <summary>Highlights chosen terms in a text, extracting the most relevant section.
		/// 	</summary>
		/// <remarks>
		/// Highlights chosen terms in a text, extracting the most relevant section.
		/// The document text is analysed in chunks to record hit statistics
		/// across the document. After accumulating stats, the fragment with the highest score
		/// is returned
		/// </remarks>
		/// <param name="tokenStream">
		/// a stream of tokens identified in the text parameter, including offset information.
		/// This is typically produced by an analyzer re-parsing a document's
		/// text. Some work may be done on retrieving TokenStreams more efficiently
		/// by adding support for storing original text position data in the Lucene
		/// index but this support is not currently available (as of Lucene 1.4 rc2).
		/// </param>
		/// <param name="text">text to highlight terms in</param>
		/// <returns>highlighted text fragment or null if no terms found</returns>
		/// <exception cref="InvalidTokenOffsetsException">thrown if any token's endOffset exceeds the provided text's length
		/// 	</exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Org.Apache.Lucene.Search.Highlight.InvalidTokenOffsetsException"
		/// 	></exception>
		public string GetBestFragment(TokenStream tokenStream, string text)
		{
			string[] results = GetBestFragments(tokenStream, text, 1);
			if (results.Length > 0)
			{
				return results[0];
			}
			return null;
		}

		/// <summary>Highlights chosen terms in a text, extracting the most relevant sections.
		/// 	</summary>
		/// <remarks>
		/// Highlights chosen terms in a text, extracting the most relevant sections.
		/// This is a convenience method that calls
		/// <see cref="GetBestFragments(Org.Apache.Lucene.Analysis.TokenStream, string, int)"
		/// 	>GetBestFragments(Org.Apache.Lucene.Analysis.TokenStream, string, int)</see>
		/// </remarks>
		/// <param name="analyzer">
		/// the analyzer that will be used to split <code>text</code>
		/// into chunks
		/// </param>
		/// <param name="fieldName">the name of the field being highlighted (used by analyzer)
		/// 	</param>
		/// <param name="text">text to highlight terms in</param>
		/// <param name="maxNumFragments">the maximum number of fragments.</param>
		/// <returns>highlighted text fragments (between 0 and maxNumFragments number of fragments)
		/// 	</returns>
		/// <exception cref="InvalidTokenOffsetsException">thrown if any token's endOffset exceeds the provided text's length
		/// 	</exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Org.Apache.Lucene.Search.Highlight.InvalidTokenOffsetsException"
		/// 	></exception>
		public string[] GetBestFragments(Analyzer analyzer, string fieldName, string text
			, int maxNumFragments)
		{
			TokenStream tokenStream = analyzer.TokenStream(fieldName, text);
			return GetBestFragments(tokenStream, text, maxNumFragments);
		}

		/// <summary>Highlights chosen terms in a text, extracting the most relevant sections.
		/// 	</summary>
		/// <remarks>
		/// Highlights chosen terms in a text, extracting the most relevant sections.
		/// The document text is analysed in chunks to record hit statistics
		/// across the document. After accumulating stats, the fragments with the highest scores
		/// are returned as an array of strings in order of score (contiguous fragments are merged into
		/// one in their original order to improve readability)
		/// </remarks>
		/// <param name="text">text to highlight terms in</param>
		/// <param name="maxNumFragments">the maximum number of fragments.</param>
		/// <returns>highlighted text fragments (between 0 and maxNumFragments number of fragments)
		/// 	</returns>
		/// <exception cref="InvalidTokenOffsetsException">thrown if any token's endOffset exceeds the provided text's length
		/// 	</exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Org.Apache.Lucene.Search.Highlight.InvalidTokenOffsetsException"
		/// 	></exception>
		public string[] GetBestFragments(TokenStream tokenStream, string text, int maxNumFragments
			)
		{
			maxNumFragments = Math.Max(1, maxNumFragments);
			//sanity check
			TextFragment[] frag = GetBestTextFragments(tokenStream, text, true, maxNumFragments
				);
			//Get text
			AList<string> fragTexts = new AList<string>();
			for (int i = 0; i < frag.Length; i++)
			{
				if ((frag[i] != null) && (frag[i].GetScore() > 0))
				{
					fragTexts.AddItem(frag[i].ToString());
				}
			}
			return Sharpen.Collections.ToArray(fragTexts, new string[0]);
		}

		/// <summary>Low level api to get the most relevant (formatted) sections of the document.
		/// 	</summary>
		/// <remarks>
		/// Low level api to get the most relevant (formatted) sections of the document.
		/// This method has been made public to allow visibility of score information held in TextFragment objects.
		/// Thanks to Jason Calabrese for help in redefining the interface.
		/// </remarks>
		/// <exception cref="System.IO.IOException">If there is a low-level I/O error</exception>
		/// <exception cref="InvalidTokenOffsetsException">thrown if any token's endOffset exceeds the provided text's length
		/// 	</exception>
		/// <exception cref="Org.Apache.Lucene.Search.Highlight.InvalidTokenOffsetsException"
		/// 	></exception>
		public TextFragment[] GetBestTextFragments(TokenStream tokenStream, string text, 
			bool mergeContiguousFragments, int maxNumFragments)
		{
			AList<TextFragment> docFrags = new AList<TextFragment>();
			StringBuilder newText = new StringBuilder();
			CharTermAttribute termAtt = tokenStream.AddAttribute<CharTermAttribute>();
			OffsetAttribute offsetAtt = tokenStream.AddAttribute<OffsetAttribute>();
			tokenStream.Reset();
			TextFragment currentFrag = new TextFragment(newText, newText.Length, docFrags.Count
				);
			if (fragmentScorer is QueryScorer)
			{
				((QueryScorer)fragmentScorer).SetMaxDocCharsToAnalyze(maxDocCharsToAnalyze);
			}
			TokenStream newStream = fragmentScorer.Init(tokenStream);
			if (newStream != null)
			{
				tokenStream = newStream;
			}
			fragmentScorer.StartFragment(currentFrag);
			docFrags.AddItem(currentFrag);
			FragmentQueue fragQueue = new FragmentQueue(maxNumFragments);
			try
			{
				string tokenText;
				int startOffset;
				int endOffset;
				int lastEndOffset = 0;
				textFragmenter.Start(text, tokenStream);
				TokenGroup tokenGroup = new TokenGroup(tokenStream);
				for (bool next = tokenStream.IncrementToken(); next && (offsetAtt.StartOffset() <
					 maxDocCharsToAnalyze); next = tokenStream.IncrementToken())
				{
					if ((offsetAtt.EndOffset() > text.Length) || (offsetAtt.StartOffset() > text.Length
						))
					{
						throw new InvalidTokenOffsetsException("Token " + termAtt.ToString() + " exceeds length of provided text sized "
							 + text.Length);
					}
					if ((tokenGroup.numTokens > 0) && (tokenGroup.IsDistinct()))
					{
						//the current token is distinct from previous tokens -
						// markup the cached token group info
						startOffset = tokenGroup.matchStartOffset;
						endOffset = tokenGroup.matchEndOffset;
						tokenText = Sharpen.Runtime.Substring(text, startOffset, endOffset);
						string markedUpText = formatter.HighlightTerm(encoder.EncodeText(tokenText), tokenGroup
							);
						//store any whitespace etc from between this and last group
						if (startOffset > lastEndOffset)
						{
							newText.Append(encoder.EncodeText(Sharpen.Runtime.Substring(text, lastEndOffset, 
								startOffset)));
						}
						newText.Append(markedUpText);
						lastEndOffset = Math.Max(endOffset, lastEndOffset);
						tokenGroup.Clear();
						//check if current token marks the start of a new fragment
						if (textFragmenter.IsNewFragment())
						{
							currentFrag.SetScore(fragmentScorer.GetFragmentScore());
							//record stats for a new fragment
							currentFrag.textEndPos = newText.Length;
							currentFrag = new TextFragment(newText, newText.Length, docFrags.Count);
							fragmentScorer.StartFragment(currentFrag);
							docFrags.AddItem(currentFrag);
						}
					}
					tokenGroup.AddToken(fragmentScorer.GetTokenScore());
				}
				//        if(lastEndOffset>maxDocBytesToAnalyze)
				//        {
				//          break;
				//        }
				currentFrag.SetScore(fragmentScorer.GetFragmentScore());
				if (tokenGroup.numTokens > 0)
				{
					//flush the accumulated text (same code as in above loop)
					startOffset = tokenGroup.matchStartOffset;
					endOffset = tokenGroup.matchEndOffset;
					tokenText = Sharpen.Runtime.Substring(text, startOffset, endOffset);
					string markedUpText = formatter.HighlightTerm(encoder.EncodeText(tokenText), tokenGroup
						);
					//store any whitespace etc from between this and last group
					if (startOffset > lastEndOffset)
					{
						newText.Append(encoder.EncodeText(Sharpen.Runtime.Substring(text, lastEndOffset, 
							startOffset)));
					}
					newText.Append(markedUpText);
					lastEndOffset = Math.Max(lastEndOffset, endOffset);
				}
				//Test what remains of the original text beyond the point where we stopped analyzing
				if ((lastEndOffset < text.Length) && (text.Length <= maxDocCharsToAnalyze))
				{
					//          if there is text beyond the last token considered..
					//          and that text is not too large...
					//append it to the last fragment
					newText.Append(encoder.EncodeText(Sharpen.Runtime.Substring(text, lastEndOffset))
						);
				}
				currentFrag.textEndPos = newText.Length;
				//sort the most relevant sections of the text
				for (Iterator<TextFragment> i = docFrags.Iterator(); i.HasNext(); )
				{
					currentFrag = i.Next();
					//If you are running with a version of Lucene before 11th Sept 03
					// you do not have PriorityQueue.insert() - so uncomment the code below
					//The above code caused a problem as a result of Christoph Goller's 11th Sept 03
					//fix to PriorityQueue. The correct method to use here is the new "insert" method
					// USE ABOVE CODE IF THIS DOES NOT COMPILE!
					fragQueue.InsertWithOverflow(currentFrag);
				}
				//return the most relevant fragments
				TextFragment[] frag = new TextFragment[fragQueue.Size()];
				for (int i_1 = frag.Length - 1; i_1 >= 0; i_1--)
				{
					frag[i_1] = fragQueue.Pop();
				}
				//merge any contiguous fragments to improve readability
				if (mergeContiguousFragments)
				{
					MergeContiguousFragments(frag);
					AList<TextFragment> fragTexts = new AList<TextFragment>();
					for (int i_2 = 0; i_2 < frag.Length; i_2++)
					{
						if ((frag[i_2] != null) && (frag[i_2].GetScore() > 0))
						{
							fragTexts.AddItem(frag[i_2]);
						}
					}
					frag = Sharpen.Collections.ToArray(fragTexts, new TextFragment[0]);
				}
				return frag;
			}
			finally
			{
				if (tokenStream != null)
				{
					try
					{
						tokenStream.End();
						tokenStream.Close();
					}
					catch (Exception)
					{
					}
				}
			}
		}

		/// <summary>
		/// Improves readability of a score-sorted list of TextFragments by merging any fragments
		/// that were contiguous in the original text into one larger fragment with the correct order.
		/// </summary>
		/// <remarks>
		/// Improves readability of a score-sorted list of TextFragments by merging any fragments
		/// that were contiguous in the original text into one larger fragment with the correct order.
		/// This will leave a "null" in the array entry for the lesser scored fragment.
		/// </remarks>
		/// <param name="frag">An array of document fragments in descending score</param>
		private void MergeContiguousFragments(TextFragment[] frag)
		{
			bool mergingStillBeingDone;
			if (frag.Length > 1)
			{
				do
				{
					mergingStillBeingDone = false;
					//initialise loop control flag
					//for each fragment, scan other frags looking for contiguous blocks
					for (int i = 0; i < frag.Length; i++)
					{
						if (frag[i] == null)
						{
							continue;
						}
						//merge any contiguous blocks
						for (int x = 0; x < frag.Length; x++)
						{
							if (frag[x] == null)
							{
								continue;
							}
							if (frag[i] == null)
							{
								break;
							}
							TextFragment frag1 = null;
							TextFragment frag2 = null;
							int frag1Num = 0;
							int frag2Num = 0;
							int bestScoringFragNum;
							int worstScoringFragNum;
							//if blocks are contiguous....
							if (frag[i].Follows(frag[x]))
							{
								frag1 = frag[x];
								frag1Num = x;
								frag2 = frag[i];
								frag2Num = i;
							}
							else
							{
								if (frag[x].Follows(frag[i]))
								{
									frag1 = frag[i];
									frag1Num = i;
									frag2 = frag[x];
									frag2Num = x;
								}
							}
							//merging required..
							if (frag1 != null)
							{
								if (frag1.GetScore() > frag2.GetScore())
								{
									bestScoringFragNum = frag1Num;
									worstScoringFragNum = frag2Num;
								}
								else
								{
									bestScoringFragNum = frag2Num;
									worstScoringFragNum = frag1Num;
								}
								frag1.Merge(frag2);
								frag[worstScoringFragNum] = null;
								mergingStillBeingDone = true;
								frag[bestScoringFragNum] = frag1;
							}
						}
					}
				}
				while (mergingStillBeingDone);
			}
		}

		/// <summary>
		/// Highlights terms in the  text , extracting the most relevant sections
		/// and concatenating the chosen fragments with a separator (typically "...").
		/// </summary>
		/// <remarks>
		/// Highlights terms in the  text , extracting the most relevant sections
		/// and concatenating the chosen fragments with a separator (typically "...").
		/// The document text is analysed in chunks to record hit statistics
		/// across the document. After accumulating stats, the fragments with the highest scores
		/// are returned in order as "separator" delimited strings.
		/// </remarks>
		/// <param name="text">text to highlight terms in</param>
		/// <param name="maxNumFragments">the maximum number of fragments.</param>
		/// <param name="separator">the separator used to intersperse the document fragments (typically "...")
		/// 	</param>
		/// <returns>highlighted text</returns>
		/// <exception cref="InvalidTokenOffsetsException">thrown if any token's endOffset exceeds the provided text's length
		/// 	</exception>
		/// <exception cref="System.IO.IOException"></exception>
		/// <exception cref="Org.Apache.Lucene.Search.Highlight.InvalidTokenOffsetsException"
		/// 	></exception>
		public string GetBestFragments(TokenStream tokenStream, string text, int maxNumFragments
			, string separator)
		{
			string[] sections = GetBestFragments(tokenStream, text, maxNumFragments);
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < sections.Length; i++)
			{
				if (i > 0)
				{
					result.Append(separator);
				}
				result.Append(sections[i]);
			}
			return result.ToString();
		}

		public virtual int GetMaxDocCharsToAnalyze()
		{
			return maxDocCharsToAnalyze;
		}

		public virtual void SetMaxDocCharsToAnalyze(int maxDocCharsToAnalyze)
		{
			this.maxDocCharsToAnalyze = maxDocCharsToAnalyze;
		}

		public virtual Fragmenter GetTextFragmenter()
		{
			return textFragmenter;
		}

		public virtual void SetTextFragmenter(Fragmenter fragmenter)
		{
			textFragmenter = fragmenter;
		}

		/// <returns>Object used to score each text fragment</returns>
		public virtual Scorer GetFragmentScorer()
		{
			return fragmentScorer;
		}

		public virtual void SetFragmentScorer(Scorer scorer)
		{
			fragmentScorer = scorer;
		}

		public virtual Encoder GetEncoder()
		{
			return encoder;
		}

		public virtual void SetEncoder(Encoder encoder)
		{
			this.encoder = encoder;
		}
	}

	internal class FragmentQueue : PriorityQueue<TextFragment>
	{
		public FragmentQueue(int size) : base(size)
		{
		}

		protected sealed override bool LessThan(TextFragment fragA, TextFragment fragB)
		{
			if (fragA.GetScore() == fragB.GetScore())
			{
				return fragA.fragNum > fragB.fragNum;
			}
			else
			{
				return fragA.GetScore() < fragB.GetScore();
			}
		}
	}
}
