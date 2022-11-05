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
    /// Extension to <see cref="Analyzer"/> suitable for <see cref="Analyzer"/>s which wrap
    /// other <see cref="Analyzer"/>s.
    /// <para/>
    /// <see cref="GetWrappedAnalyzer(string)"/> allows the <see cref="Analyzer"/>
    /// to wrap multiple <see cref="Analyzer"/>s which are selected on a per field basis.
    /// <para/>
    /// <see cref="WrapComponents(string, TokenStreamComponents)"/> allows the
    /// <see cref="TokenStreamComponents"/> of the wrapped <see cref="Analyzer"/> to then be wrapped
    /// (such as adding a new <see cref="TokenFilter"/> to form new <see cref="TokenStreamComponents"/>).
    /// </summary>
    public abstract class AnalyzerWrapper : Analyzer
    {
        /// <summary>
        /// Creates a new <see cref="AnalyzerWrapper"/>.  Since the <see cref="ReuseStrategy"/> of
        /// the wrapped <see cref="Analyzer"/>s are unknown, <see cref="Analyzer.PER_FIELD_REUSE_STRATEGY"/> is assumed.
        /// </summary>
        [Obsolete("Use AnalyzerWrapper(Analyzer.ReuseStrategy) and specify a valid Analyzer.ReuseStrategy, probably retrieved from the wrapped analyzer using Analyzer.Strategy.")]
        protected AnalyzerWrapper()
            : this(PER_FIELD_REUSE_STRATEGY)
        {
        }

        /// <summary>
        /// Creates a new <see cref="AnalyzerWrapper"/> with the given reuse strategy.
        /// <para/>If you want to wrap a single delegate <see cref="Analyzer"/> you can probably
        /// reuse its strategy when instantiating this subclass:
        /// <c>base(innerAnalyzer.Strategy)</c>.
        /// <para/>If you choose different analyzers per field, use
        /// <see cref="Analyzer.PER_FIELD_REUSE_STRATEGY"/>.
        /// </summary>
        /// <seealso cref="Analyzer.Strategy"/>
        protected AnalyzerWrapper(ReuseStrategy reuseStrategy)
            : base(reuseStrategy)
        {
        }

        /// <summary>
        /// Retrieves the wrapped <see cref="Analyzer"/> appropriate for analyzing the field with
        /// the given name
        /// </summary>
        /// <param name="fieldName"> Name of the field which is to be analyzed </param>
        /// <returns> <see cref="Analyzer"/> for the field with the given name.  Assumed to be non-null </returns>
        protected abstract Analyzer GetWrappedAnalyzer(string fieldName);

        /// <summary>
        /// Wraps / alters the given <see cref="TokenStreamComponents"/>, taken from the wrapped
        /// <see cref="Analyzer"/>, to form new components. It is through this method that new
        /// <see cref="TokenFilter"/>s can be added by <see cref="AnalyzerWrapper"/>s. By default, the given
        /// components are returned.
        /// </summary>
        /// <param name="fieldName">
        ///          Name of the field which is to be analyzed </param>
        /// <param name="components">
        ///          <see cref="TokenStreamComponents"/> taken from the wrapped <see cref="Analyzer"/> </param>
        /// <returns> Wrapped / altered <see cref="TokenStreamComponents"/>. </returns>
        protected virtual TokenStreamComponents WrapComponents(string fieldName, TokenStreamComponents components)
        {
            return components;
        }

        /// <summary>
        /// Wraps / alters the given <see cref="TextReader"/>. Through this method <see cref="AnalyzerWrapper"/>s can
        /// implement <see cref="InitReader(string, TextReader)"/>. By default, the given reader
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