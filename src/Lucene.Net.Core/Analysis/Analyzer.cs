using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis
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

    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;

    /// <summary>
    /// An Analyzer builds TokenStreams, which analyze text.  It thus represents a
    /// policy for extracting index terms from text.
    /// <p>
    /// In order to define what analysis is done, subclasses must define their
    /// <seealso cref="TokenStreamComponents TokenStreamComponents"/> in <seealso cref="#createComponents(String, Reader)"/>.
    /// The components are then reused in each call to <seealso cref="#tokenStream(String, Reader)"/>.
    /// <p>
    /// Simple example:
    /// <pre class="prettyprint">
    /// Analyzer analyzer = new Analyzer() {
    ///  {@literal @Override}
    ///   protected TokenStreamComponents createComponents(String fieldName, Reader reader) {
    ///     Tokenizer source = new FooTokenizer(reader);
    ///     TokenStream filter = new FooFilter(source);
    ///     filter = new BarFilter(filter);
    ///     return new TokenStreamComponents(source, filter);
    ///   }
    /// };
    /// </pre>
    /// For more examples, see the <seealso cref="Lucene.Net.Analysis Analysis package documentation"/>.
    /// <p>
    /// For some concrete implementations bundled with Lucene, look in the analysis modules:
    /// <ul>
    ///   <li><a href="{@docRoot}/../analyzers-common/overview-summary.html">Common</a>:
    ///       Analyzers for indexing content in different languages and domains.
    ///   <li><a href="{@docRoot}/../analyzers-icu/overview-summary.html">ICU</a>:
    ///       Exposes functionality from ICU to Apache Lucene.
    ///   <li><a href="{@docRoot}/../analyzers-kuromoji/overview-summary.html">Kuromoji</a>:
    ///       Morphological analyzer for Japanese text.
    ///   <li><a href="{@docRoot}/../analyzers-morfologik/overview-summary.html">Morfologik</a>:
    ///       Dictionary-driven lemmatization for the Polish language.
    ///   <li><a href="{@docRoot}/../analyzers-phonetic/overview-summary.html">Phonetic</a>:
    ///       Analysis for indexing phonetic signatures (for sounds-alike search).
    ///   <li><a href="{@docRoot}/../analyzers-smartcn/overview-summary.html">Smart Chinese</a>:
    ///       Analyzer for Simplified Chinese, which indexes words.
    ///   <li><a href="{@docRoot}/../analyzers-stempel/overview-summary.html">Stempel</a>:
    ///       Algorithmic Stemmer for the Polish Language.
    ///   <li><a href="{@docRoot}/../analyzers-uima/overview-summary.html">UIMA</a>:
    ///       Analysis integration with Apache UIMA.
    /// </ul>
    /// </summary>
    public abstract class Analyzer : IDisposable
    {
        private readonly ReuseStrategy _reuseStrategy;

        // non final as it gets nulled if closed; pkg private for access by ReuseStrategy's final helper methods:
        internal IDisposableThreadLocal<object> StoredValue = new IDisposableThreadLocal<object>();

        /// <summary>
        /// Create a new Analyzer, reusing the same set of components per-thread
        /// across calls to <seealso cref="#tokenStream(String, Reader)"/>.
        /// </summary>
        protected Analyzer()
            : this(GLOBAL_REUSE_STRATEGY)
        {
        }

        /// <summary>
        /// Expert: create a new Analyzer with a custom <seealso cref="ReuseStrategy"/>.
        /// <p>
        /// NOTE: if you just want to reuse on a per-field basis, its easier to
        /// use a subclass of <seealso cref="AnalyzerWrapper"/> such as
        /// <a href="{@docRoot}/../analyzers-common/Lucene.Net.Analysis/miscellaneous/PerFieldAnalyzerWrapper.html">
        /// PerFieldAnalyerWrapper</a> instead.
        /// </summary>
        protected Analyzer(ReuseStrategy reuseStrategy)
        {
            this._reuseStrategy = reuseStrategy;
        }

        /// <summary>
        /// Creates a new <seealso cref="TokenStreamComponents"/> instance for this analyzer.
        /// </summary>
        /// <param name="fieldName">
        ///          the name of the fields content passed to the
        ///          <seealso cref="TokenStreamComponents"/> sink as a reader </param>
        /// <param name="reader">
        ///          the reader passed to the <seealso cref="Tokenizer"/> constructor </param>
        /// <returns> the <seealso cref="TokenStreamComponents"/> for this analyzer. </returns>
        public abstract TokenStreamComponents CreateComponents(string fieldName, TextReader reader);

        /// <summary>
        /// Returns a TokenStream suitable for <code>fieldName</code>, tokenizing
        /// the contents of <code>text</code>.
        /// <p>
        /// this method uses <seealso cref="#createComponents(String, Reader)"/> to obtain an
        /// instance of <seealso cref="TokenStreamComponents"/>. It returns the sink of the
        /// components and stores the components internally. Subsequent calls to this
        /// method will reuse the previously stored components after resetting them
        /// through <seealso cref="TokenStreamComponents#setReader(Reader)"/>.
        /// <p>
        /// <b>NOTE:</b> After calling this method, the consumer must follow the
        /// workflow described in <seealso cref="TokenStream"/> to properly consume its contents.
        /// See the <seealso cref="Lucene.Net.Analysis Analysis package documentation"/> for
        /// some examples demonstrating this.
        /// </summary>
        /// <param name="fieldName"> the name of the field the created TokenStream is used for </param>
        /// <param name="text"> the String the streams source reads from </param>
        /// <returns> TokenStream for iterating the analyzed content of <code>reader</code> </returns>
        /// <exception cref="AlreadyClosedException"> if the Analyzer is closed. </exception>
        /// <exception cref="IOException"> if an i/o error occurs (may rarely happen for strings). </exception>
        /// <seealso cref= #tokenStream(String, Reader) </seealso>
        public TokenStream TokenStream(string fieldName, TextReader reader)
        {
            TokenStreamComponents components = _reuseStrategy.GetReusableComponents(this, fieldName);
            TextReader r = InitReader(fieldName, reader);
            if (components == null)
            {
                components = CreateComponents(fieldName, r);
                _reuseStrategy.SetReusableComponents(this, fieldName, components);
            }
            else
            {
                components.Reader = r;// LUCENENET TODO new TextReaderWrapper(r);
            }
            return components.TokenStream;
        }

        public TokenStream TokenStream(string fieldName, string text)
        {
            TokenStreamComponents components = _reuseStrategy.GetReusableComponents(this, fieldName);
            ReusableStringReader strReader =
                (components == null || components.ReusableStringReader == null)
                    ? new ReusableStringReader()
                    : components.ReusableStringReader;
            strReader.Value = text;
            var r = InitReader(fieldName, strReader);
            if (components == null)
            {
                components = CreateComponents(fieldName, r);
                _reuseStrategy.SetReusableComponents(this, fieldName, components);
            }
            else
            {
                components.Reader = r;
            }
            components.ReusableStringReader = strReader;
            return components.TokenStream;
        }

        /// <summary>
        /// Override this if you want to add a CharFilter chain.
        /// <p>
        /// The default implementation returns <code>reader</code>
        /// unchanged.
        /// </summary>
        /// <param name="fieldName"> IndexableField name being indexed </param>
        /// <param name="reader"> original Reader </param>
        /// <returns> reader, optionally decorated with CharFilter(s) </returns>
        public virtual TextReader InitReader(string fieldName, TextReader reader)
        {
            return reader;
        }

        /// <summary>
        /// Invoked before indexing a IndexableField instance if
        /// terms have already been added to that field.  this allows custom
        /// analyzers to place an automatic position increment gap between
        /// IndexbleField instances using the same field name.  The default value
        /// position increment gap is 0.  With a 0 position increment gap and
        /// the typical default token position increment of 1, all terms in a field,
        /// including across IndexableField instances, are in successive positions, allowing
        /// exact PhraseQuery matches, for instance, across IndexableField instance boundaries.
        /// </summary>
        /// <param name="fieldName"> IndexableField name being indexed. </param>
        /// <returns> position increment gap, added to the next token emitted from <seealso cref="#tokenStream(String,Reader)"/>.
        ///         this value must be {@code >= 0}. </returns>
        public virtual int GetPositionIncrementGap(string fieldName)
        {
            return 0;
        }

        /// <summary>
        /// Just like <seealso cref="#getPositionIncrementGap"/>, except for
        /// Token offsets instead.  By default this returns 1.
        /// this method is only called if the field
        /// produced at least one token for indexing.
        /// </summary>
        /// <param name="fieldName"> the field just indexed </param>
        /// <returns> offset gap, added to the next token emitted from <seealso cref="#tokenStream(String,Reader)"/>.
        ///         this value must be {@code >= 0}. </returns>
        public virtual int GetOffsetGap(string fieldName)
        {
            return 1;
        }

        /// <summary>
        /// Returns the used <seealso cref="ReuseStrategy"/>.
        /// </summary>
        public ReuseStrategy Strategy
        {
            get
            {
                return _reuseStrategy;
            }
        }

        /// <summary>
        /// Frees persistent resources used by this Analyzer </summary>
        public void Dispose()
        {
            if (StoredValue != null)
            {
                StoredValue.Dispose();
                StoredValue = null;
            }
        }

        /// <summary>
        /// this class encapsulates the outer components of a token stream. It provides
        /// access to the source (<seealso cref="Tokenizer"/>) and the outer end (sink), an
        /// instance of <seealso cref="TokenFilter"/> which also serves as the
        /// <seealso cref="TokenStream"/> returned by
        /// <seealso cref="Analyzer#tokenStream(String, Reader)"/>.
        /// </summary>
        public class TokenStreamComponents
        {
            /// <summary>
            /// Original source of the tokens.
            /// </summary>
            protected internal readonly Tokenizer Source;

            /// <summary>
            /// Sink tokenstream, such as the outer tokenfilter decorating
            /// the chain. this can be the source if there are no filters.
            /// </summary>
            protected internal readonly TokenStream Sink;

            /// <summary>
            /// Internal cache only used by <seealso cref="Analyzer#tokenStream(String, String)"/>. </summary>
            internal ReusableStringReader ReusableStringReader;

            /// <summary>
            /// Creates a new <seealso cref="TokenStreamComponents"/> instance.
            /// </summary>
            /// <param name="source">
            ///          the analyzer's tokenizer </param>
            /// <param name="result">
            ///          the analyzer's resulting token stream </param>
            public TokenStreamComponents(Tokenizer source, TokenStream result)
            {
                this.Source = source;
                this.Sink = result;
            }

            /// <summary>
            /// Creates a new <seealso cref="TokenStreamComponents"/> instance.
            /// </summary>
            /// <param name="source">
            ///          the analyzer's tokenizer </param>
            public TokenStreamComponents(Tokenizer source)
            {
                this.Source = source;
                this.Sink = source;
            }

            /// <summary>
            /// Resets the encapsulated components with the given reader. If the components
            /// cannot be reset, an Exception should be thrown.
            /// </summary>
            /// <param name="reader">
            ///          a reader to reset the source component </param>
            /// <exception cref="IOException">
            ///           if the component's reset method throws an <seealso cref="IOException"/> </exception>
            protected internal virtual TextReader Reader
            {
                set
                {
                    Source.Reader = value;
                }
            }

            /// <summary>
            /// Returns the sink <seealso cref="TokenStream"/>
            /// </summary>
            /// <returns> the sink <seealso cref="TokenStream"/> </returns>
            public virtual TokenStream TokenStream
            {
                get
                {
                    return Sink;
                }
            }

            /// <summary>
            /// Returns the component's <seealso cref="Tokenizer"/>
            /// </summary>
            /// <returns> Component's <seealso cref="Tokenizer"/> </returns>
            public virtual Tokenizer Tokenizer
            {
                get
                {
                    return Source;
                }
            }
        }

        /// <summary>
        /// Strategy defining how TokenStreamComponents are reused per call to
        /// <seealso cref="Analyzer#tokenStream(String, java.io.Reader)"/>.
        /// </summary>
        public abstract class ReuseStrategy
        {
            /// <summary>
            /// Gets the reusable TokenStreamComponents for the field with the given name.
            /// </summary>
            /// <param name="analyzer"> Analyzer from which to get the reused components. Use
            ///        <seealso cref="#getStoredValue(Analyzer)"/> and <seealso cref="#setStoredValue(Analyzer, Object)"/>
            ///        to access the data on the Analyzer. </param>
            /// <param name="fieldName"> Name of the field whose reusable TokenStreamComponents
            ///        are to be retrieved </param>
            /// <returns> Reusable TokenStreamComponents for the field, or {@code null}
            ///         if there was no previous components for the field </returns>
            public abstract TokenStreamComponents GetReusableComponents(Analyzer analyzer, string fieldName);

            /// <summary>
            /// Stores the given TokenStreamComponents as the reusable components for the
            /// field with the give name.
            /// </summary>
            /// <param name="fieldName"> Name of the field whose TokenStreamComponents are being set </param>
            /// <param name="components"> TokenStreamComponents which are to be reused for the field </param>
            public abstract void SetReusableComponents(Analyzer analyzer, string fieldName, TokenStreamComponents components);

            /// <summary>
            /// Returns the currently stored value.
            /// </summary>
            /// <returns> Currently stored value or {@code null} if no value is stored </returns>
            /// <exception cref="AlreadyClosedException"> if the Analyzer is closed. </exception>
            protected internal object GetStoredValue(Analyzer analyzer)
            {
                if (analyzer.StoredValue == null)
                {
                    throw new AlreadyClosedException("this Analyzer is closed");
                }
                return analyzer.StoredValue.Get();
            }

            /// <summary>
            /// Sets the stored value.
            /// </summary>
            /// <param name="storedValue"> Value to store </param>
            /// <exception cref="AlreadyClosedException"> if the Analyzer is closed. </exception>
            protected internal void SetStoredValue(Analyzer analyzer, object storedValue)
            {
                if (analyzer.StoredValue == null)
                {
                    throw new AlreadyClosedException("this Analyzer is closed");
                }
                analyzer.StoredValue.Set(storedValue);
            }
        }

        /// <summary>
        /// A predefined <seealso cref="ReuseStrategy"/>  that reuses the same components for
        /// every field.
        /// </summary>
        public static readonly ReuseStrategy GLOBAL_REUSE_STRATEGY = new GlobalReuseStrategy();

        /// <summary>
        /// Implementation of <seealso cref="ReuseStrategy"/> that reuses the same components for
        /// every field. </summary>
        /// @deprecated this implementation class will be hidden in Lucene 5.0.
        ///   Use <seealso cref="Analyzer#GLOBAL_REUSE_STRATEGY"/> instead!
        [Obsolete("this implementation class will be hidden in Lucene 5.0.")]
        public sealed class GlobalReuseStrategy : ReuseStrategy
        /// <summary>
        /// Sole constructor. (For invocation by subclass constructors, typically implicit.) </summary>
        /// @deprecated Don't create instances of this class, use <seealso cref="Analyzer#GLOBAL_REUSE_STRATEGY"/>
        {
            public override TokenStreamComponents GetReusableComponents(Analyzer analyzer, string fieldName)
            {
                return (TokenStreamComponents)GetStoredValue(analyzer);
            }

            public override void SetReusableComponents(Analyzer analyzer, string fieldName, TokenStreamComponents components)
            {
                SetStoredValue(analyzer, components);
            }
        }

        /// <summary>
        /// A predefined <seealso cref="ReuseStrategy"/> that reuses components per-field by
        /// maintaining a Map of TokenStreamComponent per field name.
        /// </summary>
        public static readonly ReuseStrategy PER_FIELD_REUSE_STRATEGY = new PerFieldReuseStrategy();

        /// <summary>
        /// Implementation of <seealso cref="ReuseStrategy"/> that reuses components per-field by
        /// maintaining a Map of TokenStreamComponent per field name. </summary>
        /// @deprecated this implementation class will be hidden in Lucene 5.0.
        ///   Use <seealso cref="Analyzer#PER_FIELD_REUSE_STRATEGY"/> instead!
        [Obsolete("this implementation class will be hidden in Lucene 5.0.")]
        public class PerFieldReuseStrategy : ReuseStrategy
        /// <summary>
        /// Sole constructor. (For invocation by subclass constructors, typically implicit.) </summary>
        /// @deprecated Don't create instances of this class, use <seealso cref="Analyzer#PER_FIELD_REUSE_STRATEGY"/>
        {
            public PerFieldReuseStrategy()
            {
            }

            public override TokenStreamComponents GetReusableComponents(Analyzer analyzer, string fieldName)
            {
                var componentsPerField = (IDictionary<string, TokenStreamComponents>)GetStoredValue(analyzer);
                if (componentsPerField != null)
                {
                    TokenStreamComponents ret;
                    componentsPerField.TryGetValue(fieldName, out ret);
                    return ret;
                }
                return null;
            }

            public override void SetReusableComponents(Analyzer analyzer, string fieldName, TokenStreamComponents components)
            {
                var componentsPerField = (IDictionary<string, TokenStreamComponents>)GetStoredValue(analyzer);
                if (componentsPerField == null)
                {
                    componentsPerField = new Dictionary<string, TokenStreamComponents>();
                    SetStoredValue(analyzer, componentsPerField);
                }
                componentsPerField[fieldName] = components;
            }
        }
    }
}