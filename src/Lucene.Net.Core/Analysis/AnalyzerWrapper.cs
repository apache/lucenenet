using System;
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

    /// <summary>
    /// Extension to <seealso cref="Analyzer"/> suitable for Analyzers which wrap
    /// other Analyzers.
    /// <p/>
    /// <seealso cref="#getWrappedAnalyzer(String)"/> allows the Analyzer
    /// to wrap multiple Analyzers which are selected on a per field basis.
    /// <p/>
    /// <seealso cref="#wrapComponents(String, Analyzer.TokenStreamComponents)"/> allows the
    /// TokenStreamComponents of the wrapped Analyzer to then be wrapped
    /// (such as adding a new <seealso cref="TokenFilter"/> to form new TokenStreamComponents.
    /// </summary>
    public abstract class AnalyzerWrapper : Analyzer
    {
        /// <summary>
        /// Creates a new AnalyzerWrapper.  Since the <seealso cref="Analyzer.ReuseStrategy"/> of
        /// the wrapped Analyzers are unknown, <seealso cref="#PER_FIELD_REUSE_STRATEGY"/> is assumed. </summary>
        /// @deprecated Use <seealso cref="#AnalyzerWrapper(Analyzer.ReuseStrategy)"/>
        /// and specify a valid <seealso cref="Analyzer.ReuseStrategy"/>, probably retrieved from the
        /// wrapped analyzer using <seealso cref="#getReuseStrategy()"/>.
        [Obsolete]
        protected internal AnalyzerWrapper()
            : this(PER_FIELD_REUSE_STRATEGY)
        {
        }

        /// <summary>
        /// Creates a new AnalyzerWrapper with the given reuse strategy.
        /// <p>If you want to wrap a single delegate Analyzer you can probably
        /// reuse its strategy when instantiating this subclass:
        /// {@code super(delegate.getReuseStrategy());}.
        /// <p>If you choose different analyzers per field, use
        /// <seealso cref="#PER_FIELD_REUSE_STRATEGY"/>. </summary>
        /// <seealso cref= #getReuseStrategy() </seealso>
        protected internal AnalyzerWrapper(ReuseStrategy reuseStrategy)
            : base(reuseStrategy)
        {
        }

        /// <summary>
        /// Retrieves the wrapped Analyzer appropriate for analyzing the field with
        /// the given name
        /// </summary>
        /// <param name="fieldName"> Name of the field which is to be analyzed </param>
        /// <returns> Analyzer for the field with the given name.  Assumed to be non-null </returns>
        protected abstract Analyzer GetWrappedAnalyzer(string fieldName);

        /// <summary>
        /// Wraps / alters the given TokenStreamComponents, taken from the wrapped
        /// Analyzer, to form new components. It is through this method that new
        /// TokenFilters can be added by AnalyzerWrappers. By default, the given
        /// components are returned.
        /// </summary>
        /// <param name="fieldName">
        ///          Name of the field which is to be analyzed </param>
        /// <param name="components">
        ///          TokenStreamComponents taken from the wrapped Analyzer </param>
        /// <returns> Wrapped / altered TokenStreamComponents. </returns>
        protected virtual TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            return components;
        }

        /// <summary>
        /// Wraps / alters the given Reader. Through this method AnalyzerWrappers can
        /// implement <seealso cref="#initReader(String, Reader)"/>. By default, the given reader
        /// is returned.
        /// </summary>
        /// <param name="fieldName">
        ///          name of the field which is to be analyzed </param>
        /// <param name="reader">
        ///          the reader to wrap </param>
        /// <returns> the wrapped reader </returns>
        protected virtual TextReader WrapReader(string fieldName, TextReader reader)
        {
            return reader;
        }

        protected internal override sealed TokenStreamComponents CreateComponents(string fieldName, TextReader aReader)
        {
            var wrappedAnalyzer = GetWrappedAnalyzer(fieldName);
            var component = wrappedAnalyzer.CreateComponents(fieldName, aReader);
            return WrapComponents(fieldName, component);
        }

        public override int GetPositionIncrementGap(string fieldName)
        {
            return GetWrappedAnalyzer(fieldName).GetPositionIncrementGap(fieldName);
        }

        public override int GetOffsetGap(string fieldName)
        {
            return GetWrappedAnalyzer(fieldName).GetOffsetGap(fieldName);
        }

        protected internal override TextReader InitReader(string fieldName, TextReader reader)
        {
            return GetWrappedAnalyzer(fieldName).InitReader(fieldName, WrapReader(fieldName, reader));
        }
    }
}