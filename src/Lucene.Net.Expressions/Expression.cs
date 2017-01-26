using Lucene.Net.Queries.Function;
using Lucene.Net.Search;

namespace Lucene.Net.Expressions
{
	/// <summary>Base class that computes the value of an expression for a document.</summary>
	/// <remarks>
	/// Base class that computes the value of an expression for a document.
	/// <p>
	/// Example usage:
	/// <pre class="prettyprint">
	/// // compile an expression:
	/// Expression expr = JavascriptCompiler.compile("sqrt(_score) + ln(popularity)");
	/// // SimpleBindings just maps variables to SortField instances
	/// SimpleBindings bindings = new SimpleBindings();
	/// bindings.add(new SortField("_score", SortField.Type.SCORE));
	/// bindings.add(new SortField("popularity", SortField.Type.INT));
	/// // create a sort field and sort by it (reverse order)
	/// Sort sort = new Sort(expr.getSortField(bindings, true));
	/// Query query = new TermQuery(new Term("body", "contents"));
	/// searcher.search(query, null, 10, sort);
	/// </pre>
	/// </remarks>
	/// <seealso cref="Lucene.Net.Expressions.JS.JavascriptCompiler.Compile(string)">Lucene.Net.Expressions.JS.JavascriptCompiler.Compile(string)</seealso>
	/// <lucene.experimental></lucene.experimental>
	public abstract class Expression
	{
		/// <summary>The original source text</summary>
		public readonly string sourceText; // LUCENENET TODO: Make property

        /// <summary>Named variables referred to by this expression</summary>
        public readonly string[] variables; // LUCENENET TODO: Make property

        /// <summary>
        /// Creates a new
        /// <code>Expression</code>
        /// .
        /// </summary>
        /// <param name="sourceText">
        /// Source text for the expression: e.g.
        /// <code>ln(popularity)</code>
        /// </param>
        /// <param name="variables">
        /// Names of external variables referred to by the expression
        /// </param>
        protected Expression(string sourceText, string[] variables)
		{
			// javadocs
			this.sourceText = sourceText;
			this.variables = variables;
		}

		/// <summary>Evaluates the expression for the given document.</summary>
		/// <remarks>Evaluates the expression for the given document.</remarks>
		/// <param name="document"><code>docId</code> of the document to compute a value for</param>
		/// <param name="functionValues">
		/// 
		/// <see cref="Lucene.Net.Queries.Function.FunctionValues">Lucene.Net.Queries.Function.FunctionValues
		/// 	</see>
		/// for each element of
		/// <see cref="variables">variables</see>
		/// .
		/// </param>
		/// <returns>The computed value of the expression for the given document.</returns>
		public abstract double Evaluate(int document, FunctionValues[] functionValues);

		/// <summary>Get a value source which can compute the value of this expression in the context of the given bindings.
		/// 	</summary>
		/// <remarks>Get a value source which can compute the value of this expression in the context of the given bindings.
		/// 	</remarks>
		/// <param name="bindings">Bindings to use for external values in this expression</param>
		/// <returns>A value source which will evaluate this expression when used</returns>
		public virtual ValueSource GetValueSource(Bindings bindings)
		{
			return new ExpressionValueSource(bindings, this);
		}

		/// <summary>Get a sort field which can be used to rank documents by this expression.
		/// 	</summary>
		/// <remarks>Get a sort field which can be used to rank documents by this expression.
		/// 	</remarks>
		public virtual SortField GetSortField(Bindings bindings, bool reverse)
		{
			return GetValueSource(bindings).GetSortField(reverse);
		}

		/// <summary>
		/// Get a
		/// <see cref="Lucene.Net.Search.Rescorer">Lucene.Net.Search.Rescorer</see>
		/// , to rescore first-pass hits
		/// using this expression.
		/// </summary>
		public virtual Rescorer GetRescorer(Bindings bindings)
		{
			return new ExpressionRescorer(this, bindings);
		}
	}
}
