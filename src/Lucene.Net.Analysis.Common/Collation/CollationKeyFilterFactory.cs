// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Lucene.Net.Collation
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
    /// Factory for <see cref="CollationKeyFilter"/>.
    /// <para>
    /// This factory is created based upon a system collator associated with a
    /// <see cref="CultureInfo"/>:
    /// <list type="bullet">
    ///  <item><description>language: ISO-639 language code (mandatory)</description></item>
    ///  <item><description>country: ISO-3166 country code (optional)</description></item>
    ///  <item><description>variant: vendor or browser-specific code (optional)</description></item>
    ///  <item><description>strength: 'primary','secondary','tertiary', or 'identical' (optional)</description></item>
    ///  <item><description>decomposition: 'no','canonical', or 'full' (optional)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <code>
    /// &lt;fieldType name="text_clltnky" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.CollationKeyFilterFactory" language="ja" country="JP"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// </para>
    /// <para>
    /// <strong>LUCENENET NOTE:</strong> Unlike Lucene's <c>java.text.RuleBasedCollator</c>, the .NET
    /// <see cref="CompareInfo"/> does not support tailored (custom) collation rules. The <c>custom</c>
    /// parameter that exists in Lucene is therefore not supported; specifying it throws
    /// <see cref="NotSupportedException"/>. Use the <c>ICUCollationKeyFilterFactory</c> in the
    /// Lucene.Net.ICU package if you need custom rules.
    /// </para>
    /// </summary>
    /// <seealso cref="CompareInfo"/>
    /// <seealso cref="CultureInfo"/>
    /// @since solr 3.1
    /// @deprecated use <see cref="CollationKeyAnalyzer"/> instead.
    [Obsolete("use CollationKeyAnalyzer instead.")]
    public class CollationKeyFilterFactory : TokenFilterFactory, IMultiTermAwareComponent, IResourceLoaderAware
    {
        private CompareInfo collator;
        private CompareOptions options;
        private readonly string custom;
        private readonly string language;
        private readonly string country;
        private readonly string variant;
        private readonly string strength;
        private readonly string decomposition;

        public CollationKeyFilterFactory(IDictionary<string, string> args) : base(args)
        {
            this.custom = this.RemoveFromDictionary(args, "custom");
            this.language = this.RemoveFromDictionary(args, "language");
            this.country = this.RemoveFromDictionary(args, "country");
            this.variant = this.RemoveFromDictionary(args, "variant");
            this.strength = this.RemoveFromDictionary(args, "strength");
            this.decomposition = this.RemoveFromDictionary(args, "decomposition");

            if (this.custom is null && this.language is null)
            {
                throw new ArgumentException("Either custom or language is required.");
            }

            if (this.custom != null && (this.language != null || this.country != null || this.variant != null))
            {
                throw new ArgumentException("Cannot specify both language and custom. "
                    + "To tailor rules for a built-in language, the platform collator must be replaced with the "
                    + "ICUCollationKeyFilterFactory in the Lucene.Net.ICU package, which supports custom rulesets.");
            }

            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (this.language != null)
            {
                // create from a system collator, based on culture.
                this.collator = this.CreateFromLocale(this.language, this.country, this.variant);
            }
            else
            {
                // LUCENENET: The .NET CompareInfo does not support tailored (custom) collation
                // rules the way java.text.RuleBasedCollator does. Direct users to the ICU package.
                throw new NotSupportedException("Custom collation rules are not supported by the platform collator. "
                    + "Use the ICUCollationKeyFilterFactory in the Lucene.Net.ICU package, which supports custom rulesets.");
            }

            this.options = CompareOptions.None;

            // set the strength flag, otherwise it will be the default.
            // LUCENENET: System.Globalization.CompareInfo has no Strength concept; the collation
            // strength is approximated with CompareOptions. IDENTICAL is the strongest level the
            // platform collator exposes (it has no level beyond TERTIARY/IDENTICAL).
            if (this.strength != null)
            {
                if (this.strength.Equals("primary", StringComparison.OrdinalIgnoreCase))
                {
                    this.options |= CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
                }
                else if (this.strength.Equals("secondary", StringComparison.OrdinalIgnoreCase))
                {
                    this.options |= CompareOptions.IgnoreCase;
                }
                else if (this.strength.Equals("tertiary", StringComparison.OrdinalIgnoreCase))
                {
                    // default: case- and accent-sensitive (CompareOptions.None)
                }
                else if (this.strength.Equals("identical", StringComparison.OrdinalIgnoreCase))
                {
                    // default: the platform collator has no level beyond tertiary/identical
                }
                else
                {
                    throw new ArgumentException("Invalid strength: " + this.strength);
                }
            }

            // set the decomposition flag, otherwise it will be the default.
            // LUCENENET: The platform collator always normalizes canonically-equivalent text, so
            // 'no' and 'canonical' both rely on the default behavior. 'full' additionally folds
            // compatibility differences such as full-/half-width and kana type.
            if (this.decomposition != null)
            {
                if (this.decomposition.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    // default
                }
                else if (this.decomposition.Equals("canonical", StringComparison.OrdinalIgnoreCase))
                {
                    // default
                }
                else if (this.decomposition.Equals("full", StringComparison.OrdinalIgnoreCase))
                {
                    this.options |= CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType;
                }
                else
                {
                    throw new ArgumentException("Invalid decomposition: " + this.decomposition);
                }
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new CollationKeyFilter(input, this.collator, this.options);
        }

        /// <summary>
        /// Create a culture from language, with optional country and variant.
        /// Then return the appropriate collator for the culture.
        /// </summary>
        private CompareInfo CreateFromLocale(string language, string country, string variant)
        {
            if (language is null)
            {
                throw new ArgumentException("Language is required");
            }

            if (country is null && variant != null)
            {
                throw new ArgumentException("To specify variant, country is required");
            }

            string name;
            if (country != null && variant != null)
            {
                name = string.Concat(language, "-", country, "-", variant);
            }
            else if (country != null)
            {
                name = string.Concat(language, "-", country);
            }
            else
            {
                name = language;
            }

            return CompareInfo.GetCompareInfo(name);
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }

        /// <summary>
        /// Trys to gets the value of a key from a dictionary and removes the value after.
        /// This is to mimic java's Dictionary.Remove method.
        /// </summary>
        /// <returns>The value for the given key; otherwise null.</returns>
        private string RemoveFromDictionary(IDictionary<string, string> args, string key)
        {
            string value = null;
            if (args.TryGetValue(key, out value))
            {
                args.Remove(key);
            }

            return value;
        }
    }
}
