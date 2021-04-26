// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Miscellaneous;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Tartarus.Snowball;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Snowball
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
    /// Factory for <see cref="SnowballFilter"/>, with configurable language
    /// <para>
    /// Note: Use of the "Lovins" stemmer is not recommended, as it is implemented with reflection.
    /// <code>
    /// &lt;fieldType name="text_snowballstem" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.StandardTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.LowerCaseFilterFactory"/&gt;
    ///     &lt;filter class="solr.SnowballPorterFilterFactory" protected="protectedkeyword.txt" language="English"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </para>
    /// </summary>
    public class SnowballPorterFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        public const string PROTECTED_TOKENS = "protected";

        private readonly string language;
        private readonly string wordFiles;
        private Type stemClass;
        private CharArraySet protectedWords = null;

        /// <summary>
        /// Creates a new <see cref="SnowballPorterFilterFactory"/> </summary>
        public SnowballPorterFilterFactory(IDictionary<string, string> args) : base(args)
        {
            language = Get(args, "language", "English");
            wordFiles = Get(args, PROTECTED_TOKENS);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            string className = typeof(SnowballProgram).Namespace + ".Ext." + 
                language + "Stemmer, " + this.GetType().Assembly.GetName().Name;
            stemClass = Type.GetType(className);

            if (wordFiles != null)
            {
                protectedWords = GetWordSet(loader, wordFiles, false);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            SnowballProgram program;
            try
            {
                program = (SnowballProgram)Activator.CreateInstance(stemClass);
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create("Error instantiating stemmer for language " + language + "from class " + stemClass, e);
            }

            if (protectedWords != null)
            {
                input = new SetKeywordMarkerFilter(input, protectedWords);
            }
            return new SnowballFilter(input, program);
        }
    }
}