using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Path
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
    /// Factory for <seealso cref="PathHierarchyTokenizer"/>. 
    /// <para>
    /// This factory is typically configured for use only in the <code>index</code> 
    /// Analyzer (or only in the <code>query</code> Analyzer, but never both).
    /// </para>
    /// <para>
    /// For example, in the configuration below a query for 
    /// <code>Books/NonFic</code> will match documents indexed with values like 
    /// <code>Books/NonFic</code>, <code>Books/NonFic/Law</code>, 
    /// <code>Books/NonFic/Science/Physics</code>, etc. But it will not match 
    /// documents indexed with values like <code>Books</code>, or 
    /// <code>Books/Fic</code>...
    /// </para>
    /// 
    /// <pre class="prettyprint">
    /// &lt;fieldType name="descendent_path" class="solr.TextField"&gt;
    ///   &lt;analyzer type="index"&gt;
    ///     &lt;tokenizer class="solr.PathHierarchyTokenizerFactory" delimiter="/" /&gt;
    ///   &lt;/analyzer&gt;
    ///   &lt;analyzer type="query"&gt;
    ///     &lt;tokenizer class="solr.KeywordTokenizerFactory" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </pre>
    /// <para>
    /// In this example however we see the oposite configuration, so that a query 
    /// for <code>Books/NonFic/Science/Physics</code> would match documents 
    /// containing <code>Books/NonFic</code>, <code>Books/NonFic/Science</code>, 
    /// or <code>Books/NonFic/Science/Physics</code>, but not 
    /// <code>Books/NonFic/Science/Physics/Theory</code> or 
    /// <code>Books/NonFic/Law</code>.
    /// </para>
    /// <pre class="prettyprint">
    /// &lt;fieldType name="descendent_path" class="solr.TextField"&gt;
    ///   &lt;analyzer type="index"&gt;
    ///     &lt;tokenizer class="solr.KeywordTokenizerFactory" /&gt;
    ///   &lt;/analyzer&gt;
    ///   &lt;analyzer type="query"&gt;
    ///     &lt;tokenizer class="solr.PathHierarchyTokenizerFactory" delimiter="/" /&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </pre>
    /// </summary>
    public class PathHierarchyTokenizerFactory : TokenizerFactory
    {
        private readonly char delimiter;
        private readonly char replacement;
        private readonly bool reverse;
        private readonly int skip;

        /// <summary>
        /// Creates a new PathHierarchyTokenizerFactory </summary>
        public PathHierarchyTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            delimiter = GetChar(args, "delimiter", PathHierarchyTokenizer.DEFAULT_DELIMITER);
            replacement = GetChar(args, "replace", delimiter);
            reverse = GetBoolean(args, "reverse", false);
            skip = GetInt(args, "skip", PathHierarchyTokenizer.DEFAULT_SKIP);
            if (args.Count > 0)
            {
                throw new System.ArgumentException("Unknown parameters: " + args);
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            if (reverse)
            {
                return new ReversePathHierarchyTokenizer(factory, input, delimiter, replacement, skip);
            }
            return new PathHierarchyTokenizer(factory, input, delimiter, replacement, skip);
        }
    }
}