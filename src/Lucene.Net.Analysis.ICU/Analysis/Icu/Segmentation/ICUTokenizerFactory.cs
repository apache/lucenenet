// Lucene version compatibility level 8.6.1
using ICU4N;
using ICU4N.Globalization;
using ICU4N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Icu.Segmentation
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
    /// Factory for <see cref="ICUTokenizer"/>.
    /// Words are broken across script boundaries, then segmented according to
    /// the <see cref="BreakIterator"/> and typing provided by the <see cref="DefaultICUTokenizerConfig"/>.
    /// </summary>
    /// <remarks>
    /// To use the default set of per-script rules:
    /// <code>
    /// &lt;fieldType name="text_icu" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.ICUTokenizerFactory"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// <para/>
    /// You can customize this tokenizer's behavior by specifying per-script rule files,
    /// which are compiled by the ICU <see cref="RuleBasedBreakIterator"/>.  See the
    /// <a href="http://userguide.icu-project.org/boundaryanalysis#TOC-RBBI-Rules"
    /// >ICU RuleBasedBreakIterator syntax reference</a>.
    /// <para/>
    /// To add per-script rules, add a "rulefiles" argument, which should contain a
    /// comma-separated list of <c>code:rulefile</c> pairs in the following format:
    /// <a href="http://unicode.org/iso15924/iso15924-codes.html"
    /// >four-letter ISO 15924 script code</a>, followed by a colon, then a resource
    /// path.  E.g. to specify rules for Latin (script code "Latn") and Cyrillic
    /// (script code "Cyrl"):
    /// <code>
    /// &lt;fieldType name="text_icu_custom" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.ICUTokenizerFactory" cjkAsWords="true"
    ///                rulefiles="Latn:my.Latin.rules.rbbi,Cyrl:my.Cyrillic.rules.rbbi"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;
    /// </code>
    /// </remarks>
    [ExceptionToClassNameConvention]
    public class ICUTokenizerFactory : TokenizerFactory, IResourceLoaderAware
    {
        // SPI Name
        //public const string NAME = "icu";

        internal const string RULEFILES = "rulefiles";
        private readonly IDictionary<int, string> tailored;
        private ICUTokenizerConfig config;
        private readonly bool cjkAsWords;
        private readonly bool myanmarAsWords;

        /// <summary>Creates a new <see cref="ICUTokenizerFactory"/>.</summary>
        public ICUTokenizerFactory(IDictionary<string, string> args)
            : base(args)
        {
            tailored = new Dictionary<int, string>();
            string rulefilesArg = Get(args, RULEFILES);
            if (rulefilesArg != null)
            {
                IList<string> scriptAndResourcePaths = SplitFileNames(rulefilesArg);
                foreach (string scriptAndResourcePath in scriptAndResourcePaths)
                {
                    int colonPos = scriptAndResourcePath.IndexOf(':');
                    string scriptCode = scriptAndResourcePath.Substring(0, colonPos - 0).Trim();
                    string resourcePath = scriptAndResourcePath.Substring(colonPos + 1).Trim();
                    tailored[UChar.GetPropertyValueEnum(UProperty.Script, scriptCode)] = resourcePath;
                }
            }
            cjkAsWords = GetBoolean(args, "cjkAsWords", true);
            myanmarAsWords = GetBoolean(args, "myanmarAsWords", true);
            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(tailored != null, "init must be called first!");
            if (tailored.Count == 0)
            {
                config = new DefaultICUTokenizerConfig(cjkAsWords, myanmarAsWords);
            }
            else
            {
                config = new DefaultICUTokenizerConfigAnonymousClass(cjkAsWords, myanmarAsWords, tailored, loader);
            }
        }

        private sealed class DefaultICUTokenizerConfigAnonymousClass : DefaultICUTokenizerConfig
        {
            private readonly BreakIterator[] breakers;
            public DefaultICUTokenizerConfigAnonymousClass(bool cjkAsWords, bool myanmarAsWords, IDictionary<int, string> tailored, IResourceLoader loader)
                : base(cjkAsWords, myanmarAsWords)
            {
                breakers = new BreakIterator[1 + UChar.GetIntPropertyMaxValue(UProperty.Script)];
                foreach (var entry in tailored)
                {
                    int code = entry.Key;
                    string resourcePath = entry.Value;
                    breakers[code] = ParseRules(resourcePath, loader);
                }
            }

            public override RuleBasedBreakIterator GetBreakIterator(int script)
            {
                if (breakers[script] != null)
                {
                    return (RuleBasedBreakIterator)breakers[script].Clone();
                }
                else
                {
                    return base.GetBreakIterator(script);
                }
            }

            private static BreakIterator ParseRules(string filename, IResourceLoader loader) // LUCENENET: CA1822: Mark members as static
            {
                StringBuilder rules = new StringBuilder();
                Stream rulesStream = loader.OpenResource(filename);
                using (TextReader reader = IOUtils.GetDecodingReader(rulesStream, Encoding.UTF8))
                {
                    string line = null;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.StartsWith("#", StringComparison.Ordinal))
                        {
                            rules.Append(line);
                        }
                        rules.Append('\n');
                    }
                }
                return new RuleBasedBreakIterator(rules.ToString());
            }
        }

        public override Tokenizer Create(AttributeSource.AttributeFactory factory, TextReader input)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(config != null, "inform must be called first!");
            return new ICUTokenizer(factory, input, config);
        }
    }
}
