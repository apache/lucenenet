// Lucene version compatibility level 4.8.1
#if FEATURE_COLLATION
using Icu;
using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

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
    /// This factory can be created in two ways: 
    /// <list type="bullet">
    ///  <item><description>Based upon a system collator associated with a <see cref="System.Globalization.CultureInfo"/>.</description></item>
    ///  <item><description>Based upon a tailored ruleset.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Using a System collator:
    /// <list type="bullet">
    ///  <item><description>language: ISO-639 language code (mandatory)</description></item>
    ///  <item><description>country: ISO-3166 country code (optional)</description></item>
    ///  <item><description>variant: vendor or browser-specific code (optional)</description></item>
    ///  <item><description>strength: 'primary','secondary','tertiary', or 'identical' (optional)</description></item>
    ///  <item><description>decomposition: 'no','canonical', or 'full' (optional)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Using a Tailored ruleset:
    /// <list type="bullet">
    ///  <item><description>custom: UTF-8 text file containing rules supported by RuleBasedCollator (mandatory)</description></item>
    ///  <item><description>strength: 'primary','secondary','tertiary', or 'identical' (optional)</description></item>
    ///  <item><description>decomposition: 'no','canonical', or 'full' (optional)</description></item>
    /// </list>
    /// 
    /// <code>
    /// &lt;fieldType name="text_clltnky" class="solr.TextField" positionIncrementGap="100"&gt;
    ///   &lt;analyzer&gt;
    ///     &lt;tokenizer class="solr.KeywordTokenizerFactory"/&gt;
    ///     &lt;filter class="solr.CollationKeyFilterFactory" language="ja" country="JP"/&gt;
    ///   &lt;/analyzer&gt;
    /// &lt;/fieldType&gt;</code>
    /// 
    /// </para>
    /// </summary>
    /// <seealso cref="Collator"/>
    /// <seealso cref="CultureInfo"/>
    /// <seealso cref="RuleBasedCollator"/>
    /// @since solr 3.1
    /// @deprecated use <see cref="CollationKeyAnalyzer"/> instead. 
    [Obsolete("use <seealso cref=\"CollationKeyAnalyzer\"/> instead.")]
    public class CollationKeyFilterFactory : TokenFilterFactory, IMultiTermAwareComponent, IResourceLoaderAware
    {
        private Collator collator;
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
                throw new ArgumentException("Cannot specify both language and custom. " + "To tailor rules for a built-in language, see the javadocs for RuleBasedCollator. " + "Then save the entire customized ruleset to a file, and use with the custom parameter");
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
                // create from a system collator, based on Locale.
                this.collator = this.CreateFromLocale(this.language, this.country, this.variant);
            }
            else
            {
                // create from a custom ruleset
                this.collator = this.CreateFromRules(this.custom, loader);
            }

            // set the strength flag, otherwise it will be the default.
            if (this.strength != null)
            {
                if (this.strength.Equals("primary", StringComparison.OrdinalIgnoreCase))
                {
                    this.collator.Strength = CollationStrength.Primary;
                }
                else if (this.strength.Equals("secondary", StringComparison.OrdinalIgnoreCase))
                {
                    this.collator.Strength = CollationStrength.Secondary;
                }
                else if (this.strength.Equals("tertiary", StringComparison.OrdinalIgnoreCase))
                {
                    this.collator.Strength = CollationStrength.Tertiary;
                }
                else if (this.strength.Equals("identical", StringComparison.OrdinalIgnoreCase))
                {
                    this.collator.Strength = CollationStrength.Identical;
                }
                else
                {
                    throw new ArgumentException("Invalid strength: " + this.strength);
                }
            }

            // LUCENENET TODO: Verify Decomposition > NormalizationMode mapping between the JDK and icu-dotnet

            // set the decomposition flag, otherwise it will be the default.
            if (this.decomposition != null)
            {
                if (this.decomposition.Equals("no", StringComparison.OrdinalIgnoreCase))
                {
                    this.collator.NormalizationMode = NormalizationMode.Default; // .Decomposition = Collator.NoDecomposition;
                }
                else if (this.decomposition.Equals("canonical", StringComparison.OrdinalIgnoreCase))
                {
                    this.collator.NormalizationMode = NormalizationMode.Off; //.Decomposition = Collator.CannonicalDecomposition;
                }
                else if (this.decomposition.Equals("full", StringComparison.OrdinalIgnoreCase))
                {
                    this.collator.NormalizationMode = NormalizationMode.On; //.Decomposition = Collator.FullDecomposition;
                }
                else
                {
                    throw new ArgumentException("Invalid decomposition: " + this.decomposition);
                }
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new CollationKeyFilter(input, this.collator);
        }

        /// <summary>
        /// Create a locale from language, with optional country and variant.
        /// Then return the appropriate collator for the locale.
        /// </summary>
        private Collator CreateFromLocale(string language, string country, string variant)
        {
            CultureInfo cultureInfo;

            if (language is null)
            {
                throw new ArgumentException("Language is required");
            }

            if (language != null && country is null && variant != null)
            {
                throw new ArgumentException("To specify variant, country is required");
            }

            if (country != null && variant != null)
            {
                cultureInfo = new CultureInfo(string.Concat(language, "-", country, "-", variant));

                // LUCENENET TODO: This method won't work on .NET core - confirm the above solution works as expected.
                //cultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures).Single(x =>
                //{
                //	if (!x.TwoLetterISOLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase) &&
                //		!x.ThreeLetterISOLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase) &&
                //		!x.ThreeLetterWindowsLanguageName.Equals(language, StringComparison.OrdinalIgnoreCase))
                //	{
                //		return false;
                //	}

                //	var region = new RegionInfo(x.Name);

                //	if (!region.TwoLetterISORegionName.Equals(country, StringComparison.OrdinalIgnoreCase) &&
                //		!region.ThreeLetterISORegionName.Equals(country, StringComparison.OrdinalIgnoreCase) &&
                //		!region.ThreeLetterWindowsRegionName.Equals(country, StringComparison.OrdinalIgnoreCase)
                //                    )
                //	{
                //		return false;
                //	}

                //	return x.Name
                //		.Replace(x.TwoLetterISOLanguageName, string.Empty)
                //		.Replace(region.TwoLetterISORegionName, string.Empty)
                //		.Replace("-", string.Empty)
                //		.Equals(variant, StringComparison.OrdinalIgnoreCase);
                //});
            }
            else if (country != null)
            {
                cultureInfo = new CultureInfo(string.Concat(language, "-", country));
            }
            else
            {
                cultureInfo = new CultureInfo(language);
            }

            return Collator.Create(cultureInfo);
        }

         /// <summary>
         /// Read custom rules from a file, and create a RuleBasedCollator
         /// The file cannot support comments, as # might be in the rules!
         /// </summary>
        private Collator CreateFromRules(string fileName, IResourceLoader loader)
        {
            Stream input = null;
            try
            {
                input = loader.OpenResource(fileName);
                var rules = ToUTF8String(input);
                return new RuleBasedCollator(rules);
            }
            catch (TransliteratorParseException e)
            {
                // invalid rules
                throw new IOException("ParseException thrown while parsing rules", e);
            }
            catch (SyntaxErrorException e)
            {
                // invalid rules
                throw new IOException("ParseException thrown while parsing rules", e);
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(input);
            }
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }

        private static string ToUTF8String(Stream @in)
        {
            var builder = new StringBuilder();
            var buffer = new char[1024];
            var reader = IOUtils.GetDecodingReader(@in, Encoding.UTF8);

            var index = 0;
            while ((index = reader.Read(buffer, index, 1)) > 0)
            {
                builder.Append(buffer, 0, index);
            }

            return builder.ToString();
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
#endif