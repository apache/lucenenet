using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using JCG = J2N.Collections.Generic;

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

    /// <summary>
    /// An <see cref="Analyzer"/> builds <see cref="Analysis.TokenStream"/>s, which analyze text.  It thus represents a
    /// policy for extracting index terms from text.
    /// <para/>
    /// In order to define what analysis is done, subclasses must define their
    /// <see cref="TokenStreamComponents"/> in <see cref="CreateComponents(string, TextReader)"/>.
    /// The components are then reused in each call to <see cref="GetTokenStream(string, TextReader)"/>.
    /// <para/>
    /// Simple example:
    /// <code>
    /// Analyzer analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) => 
    /// {
    ///     Tokenizer source = new FooTokenizer(reader);
    ///     TokenStream filter = new FooFilter(source);
    ///     filter = new BarFilter(filter);
    ///     return new TokenStreamComponents(source, filter);
    /// });
    /// </code>
    /// For more examples, see the <see cref="Lucene.Net.Analysis"/> namespace documentation.
    /// <para/>
    /// For some concrete implementations bundled with Lucene, look in the analysis modules:
    /// <list type="bullet">
    ///   <item><description>[Common](../analysis-common/overview.html):
    ///       Analyzers for indexing content in different languages and domains.</description></item>
    ///   <item><description>[ICU](../icu/Lucene.Net.Analysis.Icu.html):
    ///       Exposes functionality from ICU to Apache Lucene.</description></item>
    ///   <item><description>[Kuromoji](../analysis-kuromoji/Lucene.Net.Analysis.Ja.html):
    ///       Morphological analyzer for Japanese text.</description></item>
    ///   <item><description>[Morfologik](../analysis-morfologik/Lucene.Net.Analysis.Morfologik.html):
    ///       Dictionary-driven lemmatization for the Polish language.</description></item>
    ///   <item><description>[OpenNLP](../analysis-opennlp/Lucene.Net.Analysis.OpenNlp.html):
    ///       Analysis integration with Apache OpenNLP.</description></item>
    ///   <item><description>[Phonetic](../analysis-phonetic/Lucene.Net.Analysis.Phonetic.html):
    ///       Analysis for indexing phonetic signatures (for sounds-alike search).</description></item>
    ///   <item><description>[Smart Chinese](../analysis-smartcn/Lucene.Net.Analysis.Cn.Smart.html):
    ///       Analyzer for Simplified Chinese, which indexes words.</description></item>
    ///   <item><description>[Stempel](../analysis-stempel/Lucene.Net.Analysis.Stempel.html):
    ///       Algorithmic Stemmer for the Polish Language.</description></item>
    /// </list>
    /// </summary>
    public abstract class Analyzer : IDisposable
    {
        private readonly ReuseStrategy reuseStrategy;

        // non readonly as it gets nulled if closed; internal for access by ReuseStrategy's final helper methods:
        internal DisposableThreadLocal<object> storedValue = new DisposableThreadLocal<object>();

        /// <summary>
        /// Create a new <see cref="Analyzer"/>, reusing the same set of components per-thread
        /// across calls to <see cref="GetTokenStream(string, TextReader)"/>.
        /// </summary>
        protected Analyzer() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(GLOBAL_REUSE_STRATEGY)
        {
        }

        /// <summary>
        /// Expert: create a new Analyzer with a custom <see cref="ReuseStrategy"/>.
        /// <para/>
        /// NOTE: if you just want to reuse on a per-field basis, its easier to
        /// use a subclass of <see cref="AnalyzerWrapper"/> such as
        /// <c>Lucene.Net.Analysis.Common.Miscellaneous.PerFieldAnalyzerWrapper</c>
        /// instead.
        /// </summary>
        protected Analyzer(ReuseStrategy reuseStrategy) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.reuseStrategy = reuseStrategy;
        }

        /// <summary>
        /// Creates a new instance with the ability to specify the body of the <see cref="CreateComponents(string, TextReader)"/>
        /// method through the <paramref name="createComponents"/> parameter.
        /// Simple example: 
        /// <code>
        ///     var analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) => 
        ///     {
        ///         Tokenizer source = new FooTokenizer(reader);
        ///         TokenStream filter = new FooFilter(source);
        ///         filter = new BarFilter(filter);
        ///         return new TokenStreamComponents(source, filter);
        ///     });
        /// </code>
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <param name="createComponents">
        /// A delegate method that represents (is called by) the <see cref="CreateComponents(string, TextReader)"/> 
        /// method. It accepts a <see cref="string"/> fieldName and a <see cref="TextReader"/> reader and 
        /// returns the <see cref="TokenStreamComponents"/> for this analyzer.
        /// </param>
        /// <returns> A new <see cref="AnonymousAnalyzer"/> instance.</returns>
        public static Analyzer NewAnonymous(Func<string, TextReader, TokenStreamComponents> createComponents)
        {
            return NewAnonymous(createComponents, GLOBAL_REUSE_STRATEGY);
        }

        /// <summary>
        /// Creates a new instance with the ability to specify the body of the <see cref="CreateComponents(string, TextReader)"/>
        /// method through the <paramref name="createComponents"/> parameter and allows the use of a <see cref="ReuseStrategy"/>.
        /// Simple example: 
        /// <code>
        ///     var analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) => 
        ///     {
        ///         Tokenizer source = new FooTokenizer(reader);
        ///         TokenStream filter = new FooFilter(source);
        ///         filter = new BarFilter(filter);
        ///         return new TokenStreamComponents(source, filter);
        ///     }, reuseStrategy);
        /// </code>
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <param name="createComponents">
        /// An delegate method that represents (is called by) the <see cref="CreateComponents(string, TextReader)"/> 
        /// method. It accepts a <see cref="string"/> fieldName and a <see cref="TextReader"/> reader and 
        /// returns the <see cref="TokenStreamComponents"/> for this analyzer.
        /// </param>
        /// <param name="reuseStrategy">A custom <see cref="ReuseStrategy"/> instance.</param>
        /// <returns> A new <see cref="AnonymousAnalyzer"/> instance.</returns>
        public static Analyzer NewAnonymous(Func<string, TextReader, TokenStreamComponents> createComponents, ReuseStrategy reuseStrategy)
        {
            return NewAnonymous(createComponents, null, reuseStrategy);
        }

        /// <summary>
        /// Creates a new instance with the ability to specify the body of the <see cref="CreateComponents(string, TextReader)"/>
        /// method through the <paramref name="createComponents"/> parameter and the body of the <see cref="InitReader(string, TextReader)"/>
        /// method through the <paramref name="initReader"/> parameter.
        /// Simple example: 
        /// <code>
        ///     var analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) => 
        ///     {
        ///         Tokenizer source = new FooTokenizer(reader);
        ///         TokenStream filter = new FooFilter(source);
        ///         filter = new BarFilter(filter);
        ///         return new TokenStreamComponents(source, filter);
        ///     }, initReader: (fieldName, reader) => 
        ///     {
        ///         return new HTMLStripCharFilter(reader);
        ///     });
        /// </code>
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <param name="createComponents">
        /// A delegate method that represents (is called by) the <see cref="CreateComponents(string, TextReader)"/> 
        /// method. It accepts a <see cref="string"/> fieldName and a <see cref="TextReader"/> reader and 
        /// returns the <see cref="TokenStreamComponents"/> for this analyzer.
        /// </param>
        /// <param name="initReader">A delegate method that represents (is called by) the <see cref="InitReader(string, TextReader)"/>
        /// method. It accepts a <see cref="string"/> fieldName and a <see cref="TextReader"/> reader and 
        /// returns the <see cref="TextReader"/> that can be modified or wrapped by the <paramref name="initReader"/> method.</param>
        /// <returns> A new <see cref="AnonymousAnalyzer"/> instance.</returns>
        public static Analyzer NewAnonymous(Func<string, TextReader, TokenStreamComponents> createComponents, Func<string, TextReader, TextReader> initReader)
        {
            return NewAnonymous(createComponents, initReader, GLOBAL_REUSE_STRATEGY);
        }

        /// <summary>
        /// Creates a new instance with the ability to specify the body of the <see cref="CreateComponents(string, TextReader)"/>
        /// method through the <paramref name="createComponents"/> parameter, the body of the <see cref="InitReader(string, TextReader)"/>
        /// method through the <paramref name="initReader"/> parameter, and allows the use of a <see cref="ReuseStrategy"/>.
        /// Simple example: 
        /// <code>
        ///     var analyzer = Analyzer.NewAnonymous(createComponents: (fieldName, reader) => 
        ///     {
        ///         Tokenizer source = new FooTokenizer(reader);
        ///         TokenStream filter = new FooFilter(source);
        ///         filter = new BarFilter(filter);
        ///         return new TokenStreamComponents(source, filter);
        ///     }, initReader: (fieldName, reader) => 
        ///     {
        ///         return new HTMLStripCharFilter(reader);
        ///     }, reuseStrategy);
        /// </code>
        /// <para/>
        /// LUCENENET specific
        /// </summary>
        /// <param name="createComponents">
        /// A delegate method that represents (is called by) the <see cref="CreateComponents(string, TextReader)"/> 
        /// method. It accepts a <see cref="string"/> fieldName and a <see cref="TextReader"/> reader and 
        /// returns the <see cref="TokenStreamComponents"/> for this analyzer.
        /// </param>
        /// <param name="initReader">A delegate method that represents (is called by) the <see cref="InitReader(string, TextReader)"/>
        /// method. It accepts a <see cref="string"/> fieldName and a <see cref="TextReader"/> reader and 
        /// returns the <see cref="TextReader"/> that can be modified or wrapped by the <paramref name="initReader"/> method.</param>
        /// <param name="reuseStrategy">A custom <see cref="ReuseStrategy"/> instance.</param>
        /// <returns> A new <see cref="AnonymousAnalyzer"/> instance.</returns>
        public static Analyzer NewAnonymous(Func<string, TextReader, TokenStreamComponents> createComponents, Func<string, TextReader, TextReader> initReader, ReuseStrategy reuseStrategy)
        {
            return new AnonymousAnalyzer(createComponents, initReader, reuseStrategy);
        }

        /// <summary>
        /// Creates a new <see cref="TokenStreamComponents"/> instance for this analyzer.
        /// </summary>
        /// <param name="fieldName">
        ///          the name of the fields content passed to the
        ///          <see cref="TokenStreamComponents"/> sink as a reader </param>
        /// <param name="reader">
        ///          the reader passed to the <see cref="Tokenizer"/> constructor </param>
        /// <returns> the <see cref="TokenStreamComponents"/> for this analyzer. </returns>
        protected internal abstract TokenStreamComponents CreateComponents(string fieldName, TextReader reader);

        /// <summary>
        /// Returns a <see cref="TokenStream"/> suitable for <paramref name="fieldName"/>, tokenizing
        /// the contents of <c>text</c>.
        /// <para/>
        /// This method uses <see cref="CreateComponents(string, TextReader)"/> to obtain an
        /// instance of <see cref="TokenStreamComponents"/>. It returns the sink of the
        /// components and stores the components internally. Subsequent calls to this
        /// method will reuse the previously stored components after resetting them
        /// through <see cref="TokenStreamComponents.SetReader(TextReader)"/>.
        /// <para/>
        /// <b>NOTE:</b> After calling this method, the consumer must follow the
        /// workflow described in <see cref="Analysis.TokenStream"/> to properly consume its contents.
        /// See the <see cref="Lucene.Net.Analysis"/> namespace documentation for
        /// some examples demonstrating this.
        /// </summary>
        /// <param name="fieldName"> the name of the field the created <see cref="Analysis.TokenStream"/> is used for </param>
        /// <param name="reader"> the reader the streams source reads from </param>
        /// <returns> <see cref="Analysis.TokenStream"/> for iterating the analyzed content of <see cref="TextReader"/> </returns>
        /// <exception cref="ObjectDisposedException"> if the Analyzer is disposed. </exception>
        /// <exception cref="IOException"> if an i/o error occurs (may rarely happen for strings). </exception>
        /// <seealso cref="GetTokenStream(string, string)"/>
        public TokenStream GetTokenStream(string fieldName, TextReader reader)
        {
            TokenStreamComponents components = reuseStrategy.GetReusableComponents(this, fieldName);
            TextReader r = InitReader(fieldName, reader);
            if (components is null)
            {
                components = CreateComponents(fieldName, r);
                reuseStrategy.SetReusableComponents(this, fieldName, components);
            }
            else
            {
                components.SetReader(r);
            }
            return components.TokenStream;
        }

        /// <summary>
        /// Returns a <see cref="Analysis.TokenStream"/> suitable for <paramref name="fieldName"/>, tokenizing
        /// the contents of <paramref name="text"/>.
        /// <para/>
        /// This method uses <see cref="CreateComponents(string, TextReader)"/> to obtain an
        /// instance of <see cref="TokenStreamComponents"/>. It returns the sink of the
        /// components and stores the components internally. Subsequent calls to this
        /// method will reuse the previously stored components after resetting them
        /// through <see cref="TokenStreamComponents.SetReader(TextReader)"/>.
        /// <para/>
        /// <b>NOTE:</b> After calling this method, the consumer must follow the 
        /// workflow described in <see cref="Analysis.TokenStream"/> to properly consume its contents.
        /// See the <see cref="Lucene.Net.Analysis"/> namespace documentation for
        /// some examples demonstrating this.
        /// </summary>
        /// <param name="fieldName">the name of the field the created <see cref="Analysis.TokenStream"/> is used for</param>
        /// <param name="text">the <see cref="string"/> the streams source reads from </param>
        /// <returns><see cref="Analysis.TokenStream"/> for iterating the analyzed content of <c>reader</c></returns>
        /// <exception cref="ObjectDisposedException"> if the Analyzer is disposed. </exception>
        /// <exception cref="IOException"> if an i/o error occurs (may rarely happen for strings). </exception>
        /// <seealso cref="GetTokenStream(string, TextReader)"/>
        public TokenStream GetTokenStream(string fieldName, string text)
        {
            TokenStreamComponents components = reuseStrategy.GetReusableComponents(this, fieldName);
            ReusableStringReader strReader =
                (components is null || components.reusableStringReader is null)
                    ? new ReusableStringReader()
                    : components.reusableStringReader;
            strReader.SetValue(text);
            var r = InitReader(fieldName, strReader);
            if (components is null)
            {
                components = CreateComponents(fieldName, r);
                reuseStrategy.SetReusableComponents(this, fieldName, components);
            }
            else
            {
                components.SetReader(r);
            }
            components.reusableStringReader = strReader;
            return components.TokenStream;
        }

        /// <summary>
        /// Override this if you want to add a <see cref="CharFilter"/> chain.
        /// <para/>
        /// The default implementation returns <paramref name="reader"/>
        /// unchanged.
        /// </summary>
        /// <param name="fieldName"> <see cref="Index.IIndexableField"/> name being indexed </param>
        /// <param name="reader"> original <see cref="TextReader"/> </param>
        /// <returns> reader, optionally decorated with <see cref="CharFilter"/>(s) </returns>
        protected internal virtual TextReader InitReader(string fieldName, TextReader reader)
        {
            return reader;
        }

        /// <summary>
        /// Invoked before indexing a <see cref="Index.IIndexableField"/> instance if
        /// terms have already been added to that field.  This allows custom
        /// analyzers to place an automatic position increment gap between
        /// <see cref="Index.IIndexableField"/> instances using the same field name.  The default value
        /// position increment gap is 0.  With a 0 position increment gap and
        /// the typical default token position increment of 1, all terms in a field,
        /// including across <see cref="Index.IIndexableField"/> instances, are in successive positions, allowing
        /// exact <see cref="Search.PhraseQuery"/> matches, for instance, across <see cref="Index.IIndexableField"/> instance boundaries.
        /// </summary>
        /// <param name="fieldName"> <see cref="Index.IIndexableField"/> name being indexed. </param>
        /// <returns> position increment gap, added to the next token emitted from <see cref="GetTokenStream(string, TextReader)"/>.
        ///         this value must be <c>&gt;= 0</c>.</returns>
        public virtual int GetPositionIncrementGap(string fieldName)
        {
            return 0;
        }

        /// <summary>
        /// Just like <see cref="GetPositionIncrementGap"/>, except for
        /// <see cref="Token"/> offsets instead.  By default this returns 1.
        /// this method is only called if the field
        /// produced at least one token for indexing.
        /// </summary>
        /// <param name="fieldName"> the field just indexed </param>
        /// <returns> offset gap, added to the next token emitted from <see cref="GetTokenStream(string, TextReader)"/>.
        ///         this value must be <c>&gt;= 0</c>. </returns>
        public virtual int GetOffsetGap(string fieldName)
        {
            return 1;
        }

        /// <summary>
        /// Returns the used <see cref="ReuseStrategy"/>.
        /// </summary>
        public ReuseStrategy Strategy => reuseStrategy;

        /// <summary>
        /// Frees persistent resources used by this <see cref="Analyzer"/> 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Frees persistent resources used by this <see cref="Analyzer"/> 
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (storedValue != null)
                {
                    storedValue.Dispose();
                    storedValue = null;
                }
            }
        }

        // LUCENENET specific - de-nested TokenStreamComponents and ReuseStrategy
        // so they don't need to be qualified when used outside of Analyzer subclasses.

        /// <summary>
        /// A predefined <see cref="ReuseStrategy"/>  that reuses the same components for
        /// every field.
        /// </summary>
        public static readonly ReuseStrategy GLOBAL_REUSE_STRATEGY =
#pragma warning disable 612, 618
            new GlobalReuseStrategy();
#pragma warning restore 612, 618

        /// <summary>
        /// Implementation of <see cref="ReuseStrategy"/> that reuses the same components for
        /// every field. </summary>
        [Obsolete("this implementation class will be hidden in Lucene 5.0. Use Analyzer.GLOBAL_REUSE_STRATEGY instead!")]
        public sealed class GlobalReuseStrategy : ReuseStrategy
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass constructors, typically implicit.) </summary>
            [Obsolete("Don't create instances of this class, use Analyzer.GLOBAL_REUSE_STRATEGY")]
            public GlobalReuseStrategy()
            { }

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
        /// A predefined <see cref="ReuseStrategy"/> that reuses components per-field by
        /// maintaining a Map of <see cref="TokenStreamComponents"/> per field name.
        /// </summary>
        public static readonly ReuseStrategy PER_FIELD_REUSE_STRATEGY =
#pragma warning disable 612, 618
            new PerFieldReuseStrategy();
#pragma warning restore 612, 618

        /// <summary>
        /// Implementation of <see cref="ReuseStrategy"/> that reuses components per-field by
        /// maintaining a Map of <see cref="TokenStreamComponents"/> per field name.
        /// </summary>
        [Obsolete("this implementation class will be hidden in Lucene 5.0. Use Analyzer.PER_FIELD_REUSE_STRATEGY instead!")]
        public class PerFieldReuseStrategy : ReuseStrategy
        {
            /// <summary>
            /// Sole constructor. (For invocation by subclass constructors, typically implicit.)
            /// </summary>
            [Obsolete("Don't create instances of this class, use Analyzer.PER_FIELD_REUSE_STRATEGY")]
            public PerFieldReuseStrategy()
            {
            }

            public override TokenStreamComponents GetReusableComponents(Analyzer analyzer, string fieldName)
            {
                var componentsPerField = (IDictionary<string, TokenStreamComponents>)GetStoredValue(analyzer);
                if (componentsPerField != null)
                {
                    componentsPerField.TryGetValue(fieldName, out TokenStreamComponents ret);
                    return ret;
                }
                return null;
            }

            public override void SetReusableComponents(Analyzer analyzer, string fieldName, TokenStreamComponents components)
            {
                var componentsPerField = (IDictionary<string, TokenStreamComponents>)GetStoredValue(analyzer);
                if (componentsPerField is null)
                {
                    // LUCENENET-615: This needs to support nullable keys
                    componentsPerField = new JCG.Dictionary<string, TokenStreamComponents>();
                    SetStoredValue(analyzer, componentsPerField);
                }
                componentsPerField[fieldName] = components;
            }
        }

        /// <summary>
        /// LUCENENET specific helper class to mimick Java's ability to create anonymous classes.
        /// Clearly, the design of <see cref="Analyzer"/> took this feature of Java into consideration.
        /// Since it doesn't exist in .NET, we can use a delegate method to call the constructor of
        /// this concrete instance to fake it (by calling an overload of 
        /// <see cref="Analyzer.NewAnonymous(Func{string, TextReader, TokenStreamComponents})"/>).
        /// </summary>
        private class AnonymousAnalyzer : Analyzer
        {
            private readonly Func<string, TextReader, TokenStreamComponents> createComponents;
            private readonly Func<string, TextReader, TextReader> initReader;

            public AnonymousAnalyzer(Func<string, TextReader, TokenStreamComponents> createComponents, Func<string, TextReader, TextReader> initReader, ReuseStrategy reuseStrategy)
                : base(reuseStrategy)
            {
                this.createComponents = createComponents ?? throw new ArgumentNullException(nameof(createComponents));
                this.initReader = initReader;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return this.createComponents(fieldName, reader);
            }

            protected internal override TextReader InitReader(string fieldName, TextReader reader)
            {
                if (this.initReader != null)
                {
                    return this.initReader(fieldName, reader);
                }
                return base.InitReader(fieldName, reader);
            }
        }
    }

    /// <summary>
    /// This class encapsulates the outer components of a token stream. It provides
    /// access to the source (<see cref="Analysis.Tokenizer"/>) and the outer end (sink), an
    /// instance of <see cref="TokenFilter"/> which also serves as the
    /// <see cref="Analysis.TokenStream"/> returned by
    /// <see cref="Analyzer.GetTokenStream(string, TextReader)"/>.
    /// </summary>
    public class TokenStreamComponents
    {
        /// <summary>
        /// Original source of the tokens.
        /// </summary>
        protected readonly Tokenizer m_source;

        /// <summary>
        /// Sink tokenstream, such as the outer tokenfilter decorating
        /// the chain. This can be the source if there are no filters.
        /// </summary>
        protected readonly TokenStream m_sink;

        /// <summary>
        /// Internal cache only used by <see cref="Analyzer.GetTokenStream(string, string)"/>. </summary>
        internal ReusableStringReader reusableStringReader;

        /// <summary>
        /// Creates a new <see cref="TokenStreamComponents"/> instance.
        /// </summary>
        /// <param name="source">
        ///          the analyzer's tokenizer </param>
        /// <param name="result">
        ///          the analyzer's resulting token stream </param>
        public TokenStreamComponents(Tokenizer source, TokenStream result)
        {
            this.m_source = source;
            this.m_sink = result;
        }

        /// <summary>
        /// Creates a new <see cref="TokenStreamComponents"/> instance.
        /// </summary>
        /// <param name="source">
        ///          the analyzer's tokenizer </param>
        public TokenStreamComponents(Tokenizer source)
        {
            this.m_source = source;
            this.m_sink = source;
        }

        /// <summary>
        /// Resets the encapsulated components with the given reader. If the components
        /// cannot be reset, an Exception should be thrown.
        /// </summary>
        /// <param name="reader">
        ///          a reader to reset the source component </param>
        /// <exception cref="IOException">
        ///           if the component's reset method throws an <seealso cref="IOException"/> </exception>
        protected internal virtual void SetReader(TextReader reader)
        {
            m_source.SetReader(reader);
        }

        /// <summary>
        /// Returns the sink <see cref="Analysis.TokenStream"/>
        /// </summary>
        /// <returns> the sink <see cref="Analysis.TokenStream"/> </returns>
        public virtual TokenStream TokenStream => m_sink;

        /// <summary>
        /// Returns the component's <see cref="Analysis.Tokenizer"/>
        /// </summary>
        /// <returns> Component's <see cref="Analysis.Tokenizer"/> </returns>
        public virtual Tokenizer Tokenizer => m_source;
    }

    /// <summary>
    /// Strategy defining how <see cref="TokenStreamComponents"/> are reused per call to
    /// <see cref="Analyzer.GetTokenStream(string, TextReader)"/>.
    /// </summary>
    public abstract class ReuseStrategy
    {
        /// <summary>
        /// Gets the reusable <see cref="TokenStreamComponents"/> for the field with the given name.
        /// </summary>
        /// <param name="analyzer"> <see cref="Analyzer"/> from which to get the reused components. Use
        ///        <see cref="GetStoredValue(Analyzer)"/> and <see cref="SetStoredValue(Analyzer, object)"/>
        ///        to access the data on the <see cref="Analyzer"/>. </param>
        /// <param name="fieldName"> Name of the field whose reusable <see cref="TokenStreamComponents"/>
        ///        are to be retrieved </param>
        /// <returns> Reusable <see cref="TokenStreamComponents"/> for the field, or <c>null</c>
        ///         if there was no previous components for the field </returns>
        public abstract TokenStreamComponents GetReusableComponents(Analyzer analyzer, string fieldName);

        /// <summary>
        /// Stores the given <see cref="TokenStreamComponents"/> as the reusable components for the
        /// field with the give name.
        /// </summary>
        /// <param name="analyzer"> Analyzer </param>
        /// <param name="fieldName"> Name of the field whose <see cref="TokenStreamComponents"/> are being set </param>
        /// <param name="components"> <see cref="TokenStreamComponents"/> which are to be reused for the field </param>
        public abstract void SetReusableComponents(Analyzer analyzer, string fieldName, TokenStreamComponents components);

        /// <summary>
        /// Returns the currently stored value.
        /// </summary>
        /// <returns> Currently stored value or <c>null</c> if no value is stored </returns>
        /// <exception cref="ObjectDisposedException"> if the <see cref="Analyzer"/> is closed. </exception>
        protected internal static object GetStoredValue(Analyzer analyzer) // LUCENENET: CA1822: Mark members as static
        {
            if (analyzer.storedValue is null)
            {
                throw AlreadyClosedException.Create(analyzer.GetType().FullName, "this Analyzer is disposed.");
            }
            return analyzer.storedValue.Value;
        }

        /// <summary>
        /// Sets the stored value.
        /// </summary>
        /// <param name="analyzer"> Analyzer </param>
        /// <param name="storedValue"> Value to store </param>
        /// <exception cref="ObjectDisposedException"> if the <see cref="Analyzer"/> is closed. </exception>
        protected internal static void SetStoredValue(Analyzer analyzer, object storedValue) // LUCENENET: CA1822: Mark members as static
        {
            if (analyzer.storedValue is null)
            {
                throw AlreadyClosedException.Create(analyzer.GetType().FullName, "this Analyzer is disposed.");
            }
            analyzer.storedValue.Value = storedValue;
        }
    }
}