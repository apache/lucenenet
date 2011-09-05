// -----------------------------------------------------------------------
// <copyright company="Apache" file="ReusableAnalyzerBase.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// A subclass of <see cref="Analyzer"/> for the purpose of making it easier to implement
    /// <see cref="TokenStream"/> re-use.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <see cref="ReusableAnalyzerBase"/> is meant to support easy re-use of <see cref="TokenStream"/>
    ///         for the most common use-cases.  Analyzers like <c>PerFieldAnalyzerWraper</c> that behave
    ///         differently depending upon the field name may need to subclass <see cref="Analyzer"/>
    ///         directly. 
    ///     </para>
    ///     <note>Subclasses must implement <see cref="CreateComponents(string, StreamReader)"/>.</note>
    ///     <para>
    ///         For consistency, this class does not allow subclasses to extend 
    ///         <see cref="ReusableTokenStream(string, StreamReader)"/> or <see cref="TokenStream(string, StreamReader)"/>
    ///         directly. 
    ///     </para>
    /// </remarks>
    public abstract class ReusableAnalyzerBase : Analyzer
    {
        /// <summary>
        /// Finds or creates a <see cref="TokenStream"/> that is permits the <see cref="TokenStream"/>
        /// to be re-used on the same thread.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The overridden behavior of this method is to check <see cref="Analyzer.PreviousTokenStreamOrStorage"/>
        ///         to see if a <see cref="TokenStreamComponents"/> object is already stored there. If not, it creates
        ///         a new instance of <see cref="TokenStreamComponents"/> and stores it in <see cref="Analyzer.PreviousTokenStreamOrStorage"/>.
        ///         The <see cref="TokenStream" /> held inside the current the <see cref="TokenStreamComponents"/> 
        ///         instance is then returned.
        ///     </para>
        /// </remarks>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="TokenStream"/>.
        /// </returns>
        public sealed override TokenStream ReusableTokenStream(string fieldName, StreamReader reader)
        {
            var components = this.PreviousTokenStreamOrStorage as TokenStreamComponents;
            var initializedReader = this.InitializeReader(reader);
            
            if (components == null || !components.Reset(initializedReader))
            {
                components = this.CreateComponents(fieldName, initializedReader);
                this.PreviousTokenStreamOrStorage = components;
            }

            return components.TokenStream;
        }

        /// <summary>
        /// Creates a <see cref="TokenStream"/> using the specified <see cref="StreamReader"/>.
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="TokenStream"/>.
        /// </returns>
        /// <remarks>
        /// Subclasses that implement this method should always be able to handle null
        /// values for the field name for backwards compatibility.
        /// </remarks>
        public sealed override TokenStream TokenStream(string fieldName, StreamReader reader)
        {
            var initializedReader = this.InitializeReader(reader);
            var components = this.CreateComponents(fieldName, initializedReader);

            return components.TokenStream;
        }

        /// <summary>
        /// Creates a new instance of <see cref="TokenStreamComponents"/>.
        /// </summary>
        /// <param name="fieldName">Name of the file.</param>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="TokenStreamComponents"/>.
        /// </returns>
        protected abstract TokenStreamComponents CreateComponents(string fieldName, StreamReader reader);

        /// <summary>
        /// Initializes the <paramref name="reader"/>.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="StreamReader"/>.
        /// </returns>
        protected virtual StreamReader InitializeReader(StreamReader reader)
        {
            return reader;
        }

        /// <summary>
        /// The components of a <see cref="TokenStream"/>. This class
        /// provides access to the <see cref="Tokenizer"/> source and the outer end.
        /// The outer end is instance of <c>TokenFilter</c> which is also a <see cref="TokenStream"/>.
        /// </summary>
        protected internal class TokenStreamComponents
        {
            private readonly Tokenizer tokenizer;
            private readonly TokenStream tokenStream;

            /// <summary>
            /// Initializes a new instance of the <see cref="TokenStreamComponents"/> class.
            /// </summary>
            /// <param name="tokenizer">The tokenizer.</param>
            public TokenStreamComponents(Tokenizer tokenizer)
                : this(tokenizer, tokenizer)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TokenStreamComponents"/> class.
            /// </summary>
            /// <param name="tokenizer">The tokenizer.</param>
            /// <param name="tokenStream">The token stream.</param>
            public TokenStreamComponents(Tokenizer tokenizer, TokenStream tokenStream)
            {
                this.tokenizer = tokenizer;
                this.tokenStream = tokenStream;
            }

            /// <summary>
            /// Gets the token stream.
            /// </summary>
            /// <value>The token stream.</value>
            protected internal TokenStream TokenStream
            {
                get { return this.tokenStream; }
            }

            /// <summary>
            /// Resets the components with the specified <paramref name="reader"/>.
            /// </summary>
            /// <param name="reader">The reader.</param>
            /// <returns><c>true</c> if the internal components where reset, otherwise <c>false</c>.</returns>
            /// <exception cref="IOException">
            ///     Thrown when the internal <see cref="Tokenizer"/> throws an
            ///     <see cref="IOException"/>/
            /// </exception>
            protected internal bool Reset(StreamReader reader)
            {
                this.tokenizer.Reset(reader);
                return true;
            }
        }
    }
}