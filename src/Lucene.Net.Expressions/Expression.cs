using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;

namespace Lucene.Net.Expressions
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>Base class that computes the value of an expression for a document.</summary>
    /// <remarks>
    /// Base class that computes the value of an expression for a document.
    /// <para/>
    /// Example usage:
    /// <code>
    /// // compile an expression:
    /// Expression expr = JavascriptCompiler.Compile("sqrt(_score) + ln(popularity)");
    /// // SimpleBindings just maps variables to SortField instances
    /// SimpleBindings bindings = new SimpleBindings();
    /// bindings.Add(new SortField("_score", SortFieldType.SCORE));
    /// bindings.Add(new SortField("popularity", SortFieldType.INT));
    /// // create a sort field and sort by it (reverse order)
    /// Sort sort = new Sort(expr.GetSortField(bindings, true));
    /// Query query = new TermQuery(new Term("body", "contents"));
    /// searcher.Search(query, null, 10, sort);
    /// </code>
    /// @lucene.experimental
    /// </remarks>
    /// <seealso cref="Lucene.Net.Expressions.JS.JavascriptCompiler.Compile(string)"/>
    [SuppressMessage("Design", "CA1012:Abstract types should not have constructors", Justification = "Required for Reflection")]
    public abstract class Expression
    {
        /// <summary>The original source text</summary>
        public string SourceText { get; private set; }

        /// <summary>Named variables referred to by this expression</summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public string[] Variables { get; private set; }

        /// <summary>
        /// Creates a new <see cref="Expression"/>.
        /// </summary>
        /// <param name="sourceText">
        /// Source text for the expression: e.g.
        /// <c>ln(popularity)</c>
        /// </param>
        /// <param name="variables">
        /// Names of external variables referred to by the expression
        /// </param>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("CodeQuality", "S3442:\"abstract\" classes should not have \"public\" constructors", Justification = "Public is required for Relection")]
        public Expression(string sourceText, string[] variables) // LUCENENET NOTE: This must be public for the Reflection code to work right.
        {
            // javadocs
            this.SourceText = sourceText;
            this.Variables = variables;
        }

        /// <summary>Evaluates the expression for the given document.</summary>
        /// <remarks>Evaluates the expression for the given document.</remarks>
        /// <param name="document"><c>docId</c> of the document to compute a value for</param>
        /// <param name="functionValues">
        /// <see cref="Lucene.Net.Queries.Function.FunctionValues"/>
        /// for each element of <see cref="Variables">variables</see>.
        /// </param>
        /// <returns>The computed value of the expression for the given document.</returns>
        public abstract double Evaluate(int document, FunctionValues[] functionValues);

        /// <summary>Get a value source which can compute the value of this expression in the context of the given bindings.</summary>
        /// <remarks>Get a value source which can compute the value of this expression in the context of the given bindings.</remarks>
        /// <param name="bindings">Bindings to use for external values in this expression</param>
        /// <returns>A value source which will evaluate this expression when used</returns>
        public virtual ValueSource GetValueSource(Bindings bindings)
        {
            return new ExpressionValueSource(bindings, this);
        }

        /// <summary>Get a sort field which can be used to rank documents by this expression.</summary>
        /// <remarks>Get a sort field which can be used to rank documents by this expression.</remarks>
        public virtual SortField GetSortField(Bindings bindings, bool reverse)
        {
            return GetValueSource(bindings).GetSortField(reverse);
        }

        /// <summary>
        /// Get a <see cref="Lucene.Net.Search.Rescorer"/>, to rescore first-pass hits
        /// using this expression.
        /// </summary>
        public virtual Rescorer GetRescorer(Bindings bindings)
        {
            return new ExpressionRescorer(this, bindings);
        }
    }
}
