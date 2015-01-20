/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Postingshighlight;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;
using Sharpen;

namespace Lucene.Net.Search.Postingshighlight
{
	/// <summary>
	/// Simple highlighter that does not analyze fields nor use
	/// term vectors.
	/// </summary>
	/// <remarks>
	/// Simple highlighter that does not analyze fields nor use
	/// term vectors. Instead it requires
	/// <see cref="Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
	/// 	">Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
	/// 	</see>
	/// .
	/// <p>
	/// PostingsHighlighter treats the single original document as the whole corpus, and then scores individual
	/// passages as if they were documents in this corpus. It uses a
	/// <see cref="Sharpen.BreakIterator">Sharpen.BreakIterator</see>
	/// to find
	/// passages in the text; by default it breaks using
	/// <see cref="Sharpen.BreakIterator.GetSentenceInstance(System.Globalization.CultureInfo)
	/// 	">
	/// 
	/// getSentenceInstance(Locale.ROOT)
	/// </see>
	/// . It then iterates in parallel (merge sorting by offset) through
	/// the positions of all terms from the query, coalescing those hits that occur in a single passage
	/// into a
	/// <see cref="Passage">Passage</see>
	/// , and then scores each Passage using a separate
	/// <see cref="PassageScorer">PassageScorer</see>
	/// .
	/// Passages are finally formatted into highlighted snippets with a
	/// <see cref="PassageFormatter">PassageFormatter</see>
	/// .
	/// <p>
	/// You can customize the behavior by subclassing this highlighter, some important hooks:
	/// <ul>
	/// <li>
	/// <see cref="GetBreakIterator(string)">GetBreakIterator(string)</see>
	/// : Customize how the text is divided into passages.
	/// <li>
	/// <see cref="GetScorer(string)">GetScorer(string)</see>
	/// : Customize how passages are ranked.
	/// <li>
	/// <see cref="GetFormatter(string)">GetFormatter(string)</see>
	/// : Customize how snippets are formatted.
	/// <li>
	/// <see cref="GetIndexAnalyzer(string)">GetIndexAnalyzer(string)</see>
	/// : Enable highlighting of MultiTermQuerys such as
	/// <code>WildcardQuery</code>
	/// .
	/// </ul>
	/// <p>
	/// <b>WARNING</b>: The code is very new and probably still has some exciting bugs!
	/// <p>
	/// Example usage:
	/// <pre class="prettyprint">
	/// // configure field with offsets at index time
	/// FieldType offsetsType = new FieldType(TextField.TYPE_STORED);
	/// offsetsType.setIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);
	/// Field body = new Field("body", "foobar", offsetsType);
	/// // retrieve highlights at query time
	/// PostingsHighlighter highlighter = new PostingsHighlighter();
	/// Query query = new TermQuery(new Term("body", "highlighting"));
	/// TopDocs topDocs = searcher.search(query, n);
	/// String highlights[] = highlighter.highlight("body", query, searcher, topDocs);
	/// </pre>
	/// <p>
	/// This is thread-safe, and can be used across different readers.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class PostingsHighlighter
	{
		/// <summary>for rewriting: we don't want slow processing from MTQs</summary>
		private static readonly IndexReader EMPTY_INDEXREADER = new MultiReader();

		/// <summary>Default maximum content size to process.</summary>
		/// <remarks>
		/// Default maximum content size to process. Typically snippets
		/// closer to the beginning of the document better summarize its content
		/// </remarks>
		public const int DEFAULT_MAX_LENGTH = 10000;

		private readonly int maxLength;

		/// <summary>
		/// Set the first time
		/// <see cref="GetFormatter(string)">GetFormatter(string)</see>
		/// is called,
		/// and then reused.
		/// </summary>
		private PassageFormatter defaultFormatter;

		/// <summary>
		/// Set the first time
		/// <see cref="GetScorer(string)">GetScorer(string)</see>
		/// is called,
		/// and then reused.
		/// </summary>
		private PassageScorer defaultScorer;

		/// <summary>
		/// Creates a new highlighter with
		/// <see cref="DEFAULT_MAX_LENGTH">DEFAULT_MAX_LENGTH</see>
		/// .
		/// </summary>
		public PostingsHighlighter() : this(DEFAULT_MAX_LENGTH)
		{
		}

		/// <summary>Creates a new highlighter, specifying maximum content length.</summary>
		/// <remarks>Creates a new highlighter, specifying maximum content length.</remarks>
		/// <param name="maxLength">maximum content size to process.</param>
		/// <exception cref="System.ArgumentException">if <code>maxLength</code> is negative or <code>Integer.MAX_VALUE</code>
		/// 	</exception>
		public PostingsHighlighter(int maxLength)
		{
			// TODO: maybe allow re-analysis for tiny fields? currently we require offsets,
			// but if the analyzer is really fast and the field is tiny, this might really be
			// unnecessary.
			if (maxLength < 0 || maxLength == int.MaxValue)
			{
				// two reasons: no overflow problems in BreakIterator.preceding(offset+1),
				// our sentinel in the offsets queue uses this value to terminate.
				throw new ArgumentException("maxLength must be < Integer.MAX_VALUE");
			}
			this.maxLength = maxLength;
		}

		/// <summary>
		/// Returns the
		/// <see cref="Sharpen.BreakIterator">Sharpen.BreakIterator</see>
		/// to use for
		/// dividing text into passages.  This returns
		/// <see cref="Sharpen.BreakIterator.GetSentenceInstance(System.Globalization.CultureInfo)
		/// 	">Sharpen.BreakIterator.GetSentenceInstance(System.Globalization.CultureInfo)</see>
		/// by default;
		/// subclasses can override to customize.
		/// </summary>
		protected internal virtual BreakIterator GetBreakIterator(string field)
		{
			return BreakIterator.GetSentenceInstance(CultureInfo.ROOT);
		}

		/// <summary>
		/// Returns the
		/// <see cref="PassageFormatter">PassageFormatter</see>
		/// to use for
		/// formatting passages into highlighted snippets.  This
		/// returns a new
		/// <code>PassageFormatter</code>
		/// by default;
		/// subclasses can override to customize.
		/// </summary>
		protected internal virtual PassageFormatter GetFormatter(string field)
		{
			if (defaultFormatter == null)
			{
				defaultFormatter = new DefaultPassageFormatter();
			}
			return defaultFormatter;
		}

		/// <summary>
		/// Returns the
		/// <see cref="PassageScorer">PassageScorer</see>
		/// to use for
		/// ranking passages.  This
		/// returns a new
		/// <code>PassageScorer</code>
		/// by default;
		/// subclasses can override to customize.
		/// </summary>
		protected internal virtual PassageScorer GetScorer(string field)
		{
			if (defaultScorer == null)
			{
				defaultScorer = new PassageScorer();
			}
			return defaultScorer;
		}

		/// <summary>Highlights the top passages from a single field.</summary>
		/// <remarks>Highlights the top passages from a single field.</remarks>
		/// <param name="field">
		/// field name to highlight.
		/// Must have a stored string value and also be indexed with offsets.
		/// </param>
		/// <param name="query">query to highlight.</param>
		/// <param name="searcher">searcher that was previously used to execute the query.</param>
		/// <param name="topDocs">TopDocs containing the summary result documents to highlight.
		/// 	</param>
		/// <returns>
		/// Array of formatted snippets corresponding to the documents in <code>topDocs</code>.
		/// If no highlights were found for a document, the
		/// first sentence for the field will be returned.
		/// </returns>
		/// <exception cref="System.IO.IOException">if an I/O error occurred during processing
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if <code>field</code> was indexed without
		/// <see cref="Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	">Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	</see>
		/// </exception>
		public virtual string[] Highlight(string field, Query query, IndexSearcher searcher
			, TopDocs topDocs)
		{
			return Highlight(field, query, searcher, topDocs, 1);
		}

		/// <summary>Highlights the top-N passages from a single field.</summary>
		/// <remarks>Highlights the top-N passages from a single field.</remarks>
		/// <param name="field">
		/// field name to highlight.
		/// Must have a stored string value and also be indexed with offsets.
		/// </param>
		/// <param name="query">query to highlight.</param>
		/// <param name="searcher">searcher that was previously used to execute the query.</param>
		/// <param name="topDocs">TopDocs containing the summary result documents to highlight.
		/// 	</param>
		/// <param name="maxPassages">
		/// The maximum number of top-N ranked passages used to
		/// form the highlighted snippets.
		/// </param>
		/// <returns>
		/// Array of formatted snippets corresponding to the documents in <code>topDocs</code>.
		/// If no highlights were found for a document, the
		/// first
		/// <code>maxPassages</code>
		/// sentences from the
		/// field will be returned.
		/// </returns>
		/// <exception cref="System.IO.IOException">if an I/O error occurred during processing
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if <code>field</code> was indexed without
		/// <see cref="Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	">Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	</see>
		/// </exception>
		public virtual string[] Highlight(string field, Query query, IndexSearcher searcher
			, TopDocs topDocs, int maxPassages)
		{
			IDictionary<string, string[]> res = HighlightFields(new string[] { field }, query
				, searcher, topDocs, new int[] { maxPassages });
			return res.Get(field);
		}

		/// <summary>Highlights the top passages from multiple fields.</summary>
		/// <remarks>
		/// Highlights the top passages from multiple fields.
		/// <p>
		/// Conceptually, this behaves as a more efficient form of:
		/// <pre class="prettyprint">
		/// Map m = new HashMap();
		/// for (String field : fields) {
		/// m.put(field, highlight(field, query, searcher, topDocs));
		/// }
		/// return m;
		/// </pre>
		/// </remarks>
		/// <param name="fields">
		/// field names to highlight.
		/// Must have a stored string value and also be indexed with offsets.
		/// </param>
		/// <param name="query">query to highlight.</param>
		/// <param name="searcher">searcher that was previously used to execute the query.</param>
		/// <param name="topDocs">TopDocs containing the summary result documents to highlight.
		/// 	</param>
		/// <returns>
		/// Map keyed on field name, containing the array of formatted snippets
		/// corresponding to the documents in <code>topDocs</code>.
		/// If no highlights were found for a document, the
		/// first sentence from the field will be returned.
		/// </returns>
		/// <exception cref="System.IO.IOException">if an I/O error occurred during processing
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if <code>field</code> was indexed without
		/// <see cref="Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	">Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	</see>
		/// </exception>
		public virtual IDictionary<string, string[]> HighlightFields(string[] fields, Query
			 query, IndexSearcher searcher, TopDocs topDocs)
		{
			int[] maxPassages = new int[fields.Length];
			Arrays.Fill(maxPassages, 1);
			return HighlightFields(fields, query, searcher, topDocs, maxPassages);
		}

		/// <summary>Highlights the top-N passages from multiple fields.</summary>
		/// <remarks>
		/// Highlights the top-N passages from multiple fields.
		/// <p>
		/// Conceptually, this behaves as a more efficient form of:
		/// <pre class="prettyprint">
		/// Map m = new HashMap();
		/// for (String field : fields) {
		/// m.put(field, highlight(field, query, searcher, topDocs, maxPassages));
		/// }
		/// return m;
		/// </pre>
		/// </remarks>
		/// <param name="fields">
		/// field names to highlight.
		/// Must have a stored string value and also be indexed with offsets.
		/// </param>
		/// <param name="query">query to highlight.</param>
		/// <param name="searcher">searcher that was previously used to execute the query.</param>
		/// <param name="topDocs">TopDocs containing the summary result documents to highlight.
		/// 	</param>
		/// <param name="maxPassages">
		/// The maximum number of top-N ranked passages per-field used to
		/// form the highlighted snippets.
		/// </param>
		/// <returns>
		/// Map keyed on field name, containing the array of formatted snippets
		/// corresponding to the documents in <code>topDocs</code>.
		/// If no highlights were found for a document, the
		/// first
		/// <code>maxPassages</code>
		/// sentences from the
		/// field will be returned.
		/// </returns>
		/// <exception cref="System.IO.IOException">if an I/O error occurred during processing
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if <code>field</code> was indexed without
		/// <see cref="Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	">Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	</see>
		/// </exception>
		public virtual IDictionary<string, string[]> HighlightFields(string[] fields, Query
			 query, IndexSearcher searcher, TopDocs topDocs, int[] maxPassages)
		{
			ScoreDoc[] scoreDocs = topDocs.scoreDocs;
			int[] docids = new int[scoreDocs.Length];
			for (int i = 0; i < docids.Length; i++)
			{
				docids[i] = scoreDocs[i].doc;
			}
			return HighlightFields(fields, query, searcher, docids, maxPassages);
		}

		/// <summary>
		/// Highlights the top-N passages from multiple fields,
		/// for the provided int[] docids.
		/// </summary>
		/// <remarks>
		/// Highlights the top-N passages from multiple fields,
		/// for the provided int[] docids.
		/// </remarks>
		/// <param name="fieldsIn">
		/// field names to highlight.
		/// Must have a stored string value and also be indexed with offsets.
		/// </param>
		/// <param name="query">query to highlight.</param>
		/// <param name="searcher">searcher that was previously used to execute the query.</param>
		/// <param name="docidsIn">containing the document IDs to highlight.</param>
		/// <param name="maxPassagesIn">
		/// The maximum number of top-N ranked passages per-field used to
		/// form the highlighted snippets.
		/// </param>
		/// <returns>
		/// Map keyed on field name, containing the array of formatted snippets
		/// corresponding to the documents in <code>docidsIn</code>.
		/// If no highlights were found for a document, the
		/// first
		/// <code>maxPassages</code>
		/// from the field will
		/// be returned.
		/// </returns>
		/// <exception cref="System.IO.IOException">if an I/O error occurred during processing
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if <code>field</code> was indexed without
		/// <see cref="Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	">Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	</see>
		/// </exception>
		public virtual IDictionary<string, string[]> HighlightFields(string[] fieldsIn, Query
			 query, IndexSearcher searcher, int[] docidsIn, int[] maxPassagesIn)
		{
			IDictionary<string, string[]> snippets = new Dictionary<string, string[]>();
			foreach (KeyValuePair<string, object[]> ent in HighlightFieldsAsObjects(fieldsIn, 
				query, searcher, docidsIn, maxPassagesIn).EntrySet())
			{
				object[] snippetObjects = ent.Value;
				string[] snippetStrings = new string[snippetObjects.Length];
				snippets.Put(ent.Key, snippetStrings);
				for (int i = 0; i < snippetObjects.Length; i++)
				{
					object snippet = snippetObjects[i];
					if (snippet != null)
					{
						snippetStrings[i] = snippet.ToString();
					}
				}
			}
			return snippets;
		}

		/// <summary>
		/// Expert: highlights the top-N passages from multiple fields,
		/// for the provided int[] docids, to custom Object as
		/// returned by the
		/// <see cref="PassageFormatter">PassageFormatter</see>
		/// .  Use
		/// this API to render to something other than String.
		/// </summary>
		/// <param name="fieldsIn">
		/// field names to highlight.
		/// Must have a stored string value and also be indexed with offsets.
		/// </param>
		/// <param name="query">query to highlight.</param>
		/// <param name="searcher">searcher that was previously used to execute the query.</param>
		/// <param name="docidsIn">containing the document IDs to highlight.</param>
		/// <param name="maxPassagesIn">
		/// The maximum number of top-N ranked passages per-field used to
		/// form the highlighted snippets.
		/// </param>
		/// <returns>
		/// Map keyed on field name, containing the array of formatted snippets
		/// corresponding to the documents in <code>docidsIn</code>.
		/// If no highlights were found for a document, the
		/// first
		/// <code>maxPassages</code>
		/// from the field will
		/// be returned.
		/// </returns>
		/// <exception cref="System.IO.IOException">if an I/O error occurred during processing
		/// 	</exception>
		/// <exception cref="System.ArgumentException">
		/// if <code>field</code> was indexed without
		/// <see cref="Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	">Lucene.Net.Index.FieldInfo.IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS
		/// 	</see>
		/// </exception>
		protected internal virtual IDictionary<string, object[]> HighlightFieldsAsObjects
			(string[] fieldsIn, Query query, IndexSearcher searcher, int[] docidsIn, int[] maxPassagesIn
			)
		{
			if (fieldsIn.Length < 1)
			{
				throw new ArgumentException("fieldsIn must not be empty");
			}
			if (fieldsIn.Length != maxPassagesIn.Length)
			{
				throw new ArgumentException("invalid number of maxPassagesIn");
			}
			IndexReader reader = searcher.GetIndexReader();
			Query rewritten = Rewrite(query);
			ICollection<Term> queryTerms = new TreeSet<Term>();
			rewritten.ExtractTerms(queryTerms);
			IndexReaderContext readerContext = reader.GetContext();
			IList<AtomicReaderContext> leaves = readerContext.Leaves();
			// Make our own copies because we sort in-place:
			int[] docids = new int[docidsIn.Length];
			System.Array.Copy(docidsIn, 0, docids, 0, docidsIn.Length);
			string[] fields = new string[fieldsIn.Length];
			System.Array.Copy(fieldsIn, 0, fields, 0, fieldsIn.Length);
			int[] maxPassages = new int[maxPassagesIn.Length];
			System.Array.Copy(maxPassagesIn, 0, maxPassages, 0, maxPassagesIn.Length);
			// sort for sequential io
			Arrays.Sort(docids);
			new _InPlaceMergeSorter_365(fields, maxPassages).Sort(0, fields.Length);
			// pull stored data:
			string[][] contents = LoadFieldValues(searcher, fields, docids, maxLength);
			IDictionary<string, object[]> highlights = new Dictionary<string, object[]>();
			for (int i = 0; i < fields.Length; i++)
			{
				string field = fields[i];
				int numPassages = maxPassages[i];
				Term floor = new Term(field, string.Empty);
				Term ceiling = new Term(field, UnicodeUtil.BIG_TERM);
				ICollection<Term> fieldTerms = queryTerms.SubSet(floor, ceiling);
				// TODO: should we have some reasonable defaults for term pruning? (e.g. stopwords)
				// Strip off the redundant field:
				BytesRef[] terms = new BytesRef[fieldTerms.Count];
				int termUpto = 0;
				foreach (Term term in fieldTerms)
				{
					terms[termUpto++] = term.Bytes();
				}
				IDictionary<int, object> fieldHighlights = HighlightField(field, contents[i], GetBreakIterator
					(field), terms, docids, leaves, numPassages, query);
				object[] result = new object[docids.Length];
				for (int j = 0; j < docidsIn.Length; j++)
				{
					result[j] = fieldHighlights.Get(docidsIn[j]);
				}
				highlights.Put(field, result);
			}
			return highlights;
		}

		private sealed class _InPlaceMergeSorter_365 : InPlaceMergeSorter
		{
			public _InPlaceMergeSorter_365(string[] fields, int[] maxPassages)
			{
				this.fields = fields;
				this.maxPassages = maxPassages;
			}

			protected override void Swap(int i, int j)
			{
				string tmp = fields[i];
				fields[i] = fields[j];
				fields[j] = tmp;
				int tmp2 = maxPassages[i];
				maxPassages[i] = maxPassages[j];
				maxPassages[j] = tmp2;
			}

			protected override int Compare(int i, int j)
			{
				return Sharpen.Runtime.CompareOrdinal(fields[i], fields[j]);
			}

			private readonly string[] fields;

			private readonly int[] maxPassages;
		}

		/// <summary>
		/// Loads the String values for each field X docID to be
		/// highlighted.
		/// </summary>
		/// <remarks>
		/// Loads the String values for each field X docID to be
		/// highlighted.  By default this loads from stored
		/// fields, but a subclass can change the source.  This
		/// method should allocate the String[fields.length][docids.length]
		/// and fill all values.  The returned Strings must be
		/// identical to what was indexed.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual string[][] LoadFieldValues(IndexSearcher searcher, string
			[] fields, int[] docids, int maxLength)
		{
			string[][] contents = new string[fields.Length][];
			//HM:revisit. 2nd array len was removed
			char[] valueSeparators = new char[fields.Length];
			for (int i = 0; i < fields.Length; i++)
			{
				valueSeparators[i] = GetMultiValuedSeparator(fields[i]);
			}
			PostingsHighlighter.LimitedStoredFieldVisitor visitor = new PostingsHighlighter.LimitedStoredFieldVisitor
				(fields, valueSeparators, maxLength);
			for (int i_1 = 0; i_1 < docids.Length; i_1++)
			{
				searcher.Doc(docids[i_1], visitor);
				for (int j = 0; j < fields.Length; j++)
				{
					contents[j][i_1] = visitor.GetValue(j).ToString();
				}
				visitor.Reset();
			}
			return contents;
		}

		/// <summary>Returns the logical separator between values for multi-valued fields.</summary>
		/// <remarks>
		/// Returns the logical separator between values for multi-valued fields.
		/// The default value is a space character, which means passages can span across values,
		/// but a subclass can override, for example with
		/// <code>U+2029 PARAGRAPH SEPARATOR (PS)</code>
		/// if each value holds a discrete passage for highlighting.
		/// </remarks>
		protected internal virtual char GetMultiValuedSeparator(string field)
		{
			return ' ';
		}

		/// <summary>
		/// Returns the analyzer originally used to index the content for
		/// <code>field</code>
		/// .
		/// <p>
		/// This is used to highlight some MultiTermQueries.
		/// </summary>
		/// <returns>Analyzer or null (the default, meaning no special multi-term processing)
		/// 	</returns>
		protected internal virtual Analyzer GetIndexAnalyzer(string field)
		{
			return null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private IDictionary<int, object> HighlightField(string field, string[] contents, 
			BreakIterator bi, BytesRef[] terms, int[] docids, IList<AtomicReaderContext> leaves
			, int maxPassages, Query query)
		{
			IDictionary<int, object> highlights = new Dictionary<int, object>();
			PassageFormatter fieldFormatter = GetFormatter(field);
			if (fieldFormatter == null)
			{
				throw new ArgumentNullException("PassageFormatter cannot be null");
			}
			// check if we should do any multiterm processing
			Analyzer analyzer = GetIndexAnalyzer(field);
			CharacterRunAutomaton[] automata = new CharacterRunAutomaton[0];
			if (analyzer != null)
			{
				automata = MultiTermHighlighting.ExtractAutomata(query, field);
			}
			// resize 'terms', where the last term is the multiterm matcher
			if (automata.Length > 0)
			{
				BytesRef[] newTerms = new BytesRef[terms.Length + 1];
				System.Array.Copy(terms, 0, newTerms, 0, terms.Length);
				terms = newTerms;
			}
			// we are processing in increasing docid order, so we only need to reinitialize stuff on segment changes
			// otherwise, we will just advance() existing enums to the new document in the same segment.
			DocsAndPositionsEnum[] postings = null;
			TermsEnum termsEnum = null;
			int lastLeaf = -1;
			for (int i = 0; i < docids.Length; i++)
			{
				string content = contents[i];
				if (content.Length == 0)
				{
					continue;
				}
				// nothing to do
				bi.SetText(content);
				int doc = docids[i];
				int leaf = ReaderUtil.SubIndex(doc, leaves);
				AtomicReaderContext subContext = leaves[leaf];
				AtomicReader r = ((AtomicReader)subContext.Reader());
				// increasing order
				// if the segment has changed, we must initialize new enums.
				if (leaf >= lastLeaf != lastLeaf)
				{
					Terms t = r.Terms(field);
					if (t != null)
					{
						termsEnum = t.Iterator(null);
						postings = new DocsAndPositionsEnum[terms.Length];
					}
				}
				if (termsEnum == null)
				{
					continue;
				}
				// no terms for this field, nothing to do
				// if there are multi-term matches, we have to initialize the "fake" enum for each document
				if (automata.Length > 0)
				{
					DocsAndPositionsEnum dp = MultiTermHighlighting.GetDocsEnum(analyzer.TokenStream(
						field, content), automata);
					dp.Advance(doc - subContext.docBase);
					postings[terms.Length - 1] = dp;
				}
				// last term is the multiterm matcher
				Passage[] passages = HighlightDoc(field, terms, content.Length, bi, doc - subContext
					.docBase, termsEnum, postings, maxPassages);
				if (passages.Length == 0)
				{
					// no passages were returned, so ask for a default summary
					passages = GetEmptyHighlight(field, bi, maxPassages);
				}
				if (passages.Length > 0)
				{
					highlights.Put(doc, fieldFormatter.Format(passages, content));
				}
				lastLeaf = leaf;
			}
			return highlights;
		}

		// algorithm: treat sentence snippets as miniature documents
		// we can intersect these with the postings lists via BreakIterator.preceding(offset),s
		// score each sentence as norm(sentenceStartOffset) * sum(weight * tf(freq))
		/// <exception cref="System.IO.IOException"></exception>
		private Passage[] HighlightDoc(string field, BytesRef[] terms, int contentLength, 
			BreakIterator bi, int doc, TermsEnum termsEnum, DocsAndPositionsEnum[] postings, 
			int n)
		{
			PassageScorer scorer = GetScorer(field);
			if (scorer == null)
			{
				throw new ArgumentNullException("PassageScorer cannot be null");
			}
			PriorityQueue<PostingsHighlighter.OffsetsEnum> pq = new PriorityQueue<PostingsHighlighter.OffsetsEnum
				>();
			float[] weights = new float[terms.Length];
			// initialize postings
			for (int i = 0; i < terms.Length; i++)
			{
				DocsAndPositionsEnum de = postings[i];
				int pDoc;
				if (de == EMPTY)
				{
					continue;
				}
				else
				{
					if (de == null)
					{
						postings[i] = EMPTY;
						// initially
						if (!termsEnum.SeekExact(terms[i]))
						{
							continue;
						}
						// term not found
						de = postings[i] = termsEnum.DocsAndPositions(null, null, DocsAndPositionsEnum.FLAG_OFFSETS
							);
						if (de == null)
						{
							// no positions available
							throw new ArgumentException("field '" + field + "' was indexed without offsets, cannot highlight"
								);
						}
						pDoc = de.Advance(doc);
					}
					else
					{
						pDoc = de.DocID();
						if (pDoc < doc)
						{
							pDoc = de.Advance(doc);
						}
					}
				}
				if (doc == pDoc)
				{
					weights[i] = scorer.Weight(contentLength, de.Freq());
					de.NextPosition();
					pq.AddItem(new PostingsHighlighter.OffsetsEnum(de, i));
				}
			}
			pq.AddItem(new PostingsHighlighter.OffsetsEnum(EMPTY, int.MaxValue));
			// a sentinel for termination
			PriorityQueue<Passage> passageQueue = new PriorityQueue<Passage>(n, new _IComparer_577
				());
			Passage current = new Passage();
			PostingsHighlighter.OffsetsEnum off;
			while ((off = pq.Poll()) != null)
			{
				DocsAndPositionsEnum dp = off.dp;
				int start = dp.StartOffset();
				if (start == -1)
				{
					throw new ArgumentException("field '" + field + "' was indexed without offsets, cannot highlight"
						);
				}
				int end = dp.EndOffset();
				// LUCENE-5166: this hit would span the content limit... however more valid 
				// hits may exist (they are sorted by start). so we pretend like we never 
				// saw this term, it won't cause a passage to be added to passageQueue or anything.
				if (EMPTY.StartOffset() == int.MaxValue < contentLength && end > contentLength)
				{
					continue;
				}
				if (start >= current.endOffset)
				{
					if (current.startOffset >= 0)
					{
						// finalize current
						current.score *= scorer.Norm(current.startOffset);
						// new sentence: first add 'current' to queue 
						if (passageQueue.Count == n && current.score < passageQueue.Peek().score)
						{
							current.Reset();
						}
						else
						{
							// can't compete, just reset it
							passageQueue.Offer(current);
							if (passageQueue.Count > n)
							{
								current = passageQueue.Poll();
								current.Reset();
							}
							else
							{
								current = new Passage();
							}
						}
					}
					// if we exceed limit, we are done
					if (start >= contentLength)
					{
						Passage[] passages = new Passage[passageQueue.Count];
						Sharpen.Collections.ToArray(passageQueue, passages);
						foreach (Passage p in passages)
						{
							p.Sort();
						}
						// sort in ascending order
						Arrays.Sort(passages, new _IComparer_631());
						return passages;
					}
					// advance breakiterator
					BreakIterator.DONE < 0.startOffset = Math.Max(bi.Preceding(start + 1), 0);
					current.endOffset = Math.Min(bi.Next(), contentLength);
				}
				int tf = 0;
				while (true)
				{
					tf++;
					BytesRef term = terms[off.id];
					if (term == null)
					{
						// multitermquery match, pull from payload
						term = off.dp.GetPayload();
					}
					term != null.AddMatch(start, end, term);
					if (off.pos == dp.Freq())
					{
						break;
					}
					else
					{
						// removed from pq
						off.pos++;
						dp.NextPosition();
						start = dp.StartOffset();
						end = dp.EndOffset();
					}
					if (start >= current.endOffset || end > contentLength)
					{
						pq.Offer(off);
						break;
					}
				}
				current.score += weights[off.id] * scorer.Tf(tf, current.endOffset - current.startOffset
					);
			}
			// Dead code but compiler disagrees:
			//HM:revisit
			//assert false;
			return null;
		}

		private sealed class _IComparer_577 : IComparer<Passage>
		{
			public _IComparer_577()
			{
			}

			public int Compare(Passage left, Passage right)
			{
				if (left.score < right.score)
				{
					return -1;
				}
				else
				{
					if (left.score > right.score)
					{
						return 1;
					}
					else
					{
						return left.startOffset - right.startOffset;
					}
				}
			}
		}

		private sealed class _IComparer_631 : IComparer<Passage>
		{
			public _IComparer_631()
			{
			}

			public int Compare(Passage left, Passage right)
			{
				return left.startOffset - right.startOffset;
			}
		}

		/// <summary>
		/// Called to summarize a document when no hits were
		/// found.
		/// </summary>
		/// <remarks>
		/// Called to summarize a document when no hits were
		/// found.  By default this just returns the first
		/// <code>maxPassages</code>
		/// sentences; subclasses can override
		/// to customize.
		/// </remarks>
		protected internal virtual Passage[] GetEmptyHighlight(string fieldName, BreakIterator
			 bi, int maxPassages)
		{
			// BreakIterator should be un-next'd:
			IList<Passage> passages = new AList<Passage>();
			int pos = bi.Current();
			while (pos == 0.Count < maxPassages)
			{
				int next = bi.Next();
				if (next == BreakIterator.DONE)
				{
					break;
				}
				Passage passage = new Passage();
				passage.score = float.NaN;
				passage.startOffset = pos;
				passage.endOffset = next;
				passages.AddItem(passage);
				pos = next;
			}
			return Sharpen.Collections.ToArray(passages, new Passage[passages.Count]);
		}

		private class OffsetsEnum : Comparable<PostingsHighlighter.OffsetsEnum>
		{
			internal DocsAndPositionsEnum dp;

			internal int pos;

			internal int id;

			/// <exception cref="System.IO.IOException"></exception>
			internal OffsetsEnum(DocsAndPositionsEnum dp, int id)
			{
				this.dp = dp;
				this.id = id;
				this.pos = 1;
			}

			public virtual int CompareTo(PostingsHighlighter.OffsetsEnum other)
			{
				try
				{
					int off = dp.StartOffset();
					int otherOff = other.dp.StartOffset();
					if (off == otherOff)
					{
						return id - other.id;
					}
					else
					{
						return int.Compare(off, otherOff);
					}
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}
		}

		private sealed class _DocsAndPositionsEnum_728 : DocsAndPositionsEnum
		{
			public _DocsAndPositionsEnum_728()
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextPosition()
			{
				return 0;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int StartOffset()
			{
				return int.MaxValue;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int EndOffset()
			{
				return int.MaxValue;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override BytesRef GetPayload()
			{
				return null;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Freq()
			{
				return 0;
			}

			public override int DocID()
			{
				return DocIdSetIterator.NO_MORE_DOCS;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int NextDoc()
			{
				return DocIdSetIterator.NO_MORE_DOCS;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int Advance(int target)
			{
				return DocIdSetIterator.NO_MORE_DOCS;
			}

			public override long Cost()
			{
				return 0;
			}
		}

		private static readonly DocsAndPositionsEnum EMPTY = new _DocsAndPositionsEnum_728
			();

		/// <summary>
		/// we rewrite against an empty indexreader: as we don't want things like
		/// rangeQueries that don't summarize the document
		/// </summary>
		/// <exception cref="System.IO.IOException"></exception>
		private static Query Rewrite(Query original)
		{
			Query query = original;
			for (Query rewrittenQuery = query.Rewrite(EMPTY_INDEXREADER); rewrittenQuery != query
				; rewrittenQuery = query.Rewrite(EMPTY_INDEXREADER))
			{
				query = rewrittenQuery;
			}
			return query;
		}

		private class LimitedStoredFieldVisitor : StoredFieldVisitor
		{
			private readonly string fields;

			private readonly char valueSeparators;

			private readonly int maxLength;

			private readonly StringBuilder builders;

			private int currentField = -1;

			public LimitedStoredFieldVisitor(string[] fields, char[] valueSeparators, int maxLength
				)
			{
				//HM:revisit
				//assert fields.length == valueSeparators.length;
				this.fields = fields;
				this.valueSeparators = valueSeparators;
				this.maxLength = maxLength;
				builders = new StringBuilder[fields.Length];
				for (int i = 0; i < builders.Length; i++)
				{
					builders[i] = new StringBuilder();
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void StringField(FieldInfo fieldInfo, string value)
			{
				StringBuilder builder = currentField >= 0[currentField];
				if (builder.Length > 0 && builder.Length < maxLength)
				{
					builder.Append(valueSeparators[currentField]);
				}
				if (builder.Length + value.Length > maxLength)
				{
					builder.AppendRange(value, 0, maxLength - builder.Length);
				}
				else
				{
					builder.Append(value);
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override StoredFieldVisitor.Status NeedsField(FieldInfo fieldInfo)
			{
				currentField = System.Array.BinarySearch(fields, fieldInfo.name);
				if (currentField < 0)
				{
					return StoredFieldVisitor.Status.NO;
				}
				else
				{
					if (builders[currentField].Length > maxLength)
					{
						return fields.Length == 1 ? StoredFieldVisitor.Status.STOP : StoredFieldVisitor.Status
							.NO;
					}
				}
				return StoredFieldVisitor.Status.YES;
			}

			internal virtual string GetValue(int i)
			{
				return builders[i].ToString();
			}

			internal virtual void Reset()
			{
				currentField = -1;
				for (int i = 0; i < fields.Length; i++)
				{
					builders[i].Length = 0;
				}
			}
		}
	}
}
