// lucene version compatibility level: 4.8.1
using ICU4N.Globalization;
using ICU4N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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
    /// Factory for <see cref="ICUCollationKeyFilter"/>.
    /// </summary>
    /// <remarks>
    /// This factory can be created in two ways: 
    /// <list type="bullet">
    ///     <item><description>Based upon a system collator associated with a Locale.</description></item>
    ///     <item><description>Based upon a tailored ruleset.</description></item>
    /// </list>
    /// <para/>
    /// Using a System collator:
    /// <list type="bullet">
    ///     <item><description>locale: RFC 3066 locale ID (mandatory)</description></item>
    ///     <item><description>strength: 'primary','secondary','tertiary', 'quaternary', or 'identical' (optional)</description></item>
    ///     <item><description>decomposition: 'no', or 'canonical' (optional)</description></item>
    /// </list>
    /// <para/>
    /// Using a Tailored ruleset:
    /// <list type="bullet">
    ///     <item><description>custom: UTF-8 text file containing rules supported by RuleBasedCollator (mandatory)</description></item>
    ///     <item><description>strength: 'primary','secondary','tertiary', 'quaternary', or 'identical' (optional)</description></item>
    ///     <item><description>decomposition: 'no' or 'canonical' (optional)</description></item>
    /// </list>
    /// <para/>
    /// Expert options:
    /// <list type="bullet">
    ///     <item><description>alternate: 'shifted' or 'non-ignorable'. Can be used to ignore punctuation/whitespace.</description></item>
    ///     <item><description>caseLevel: 'true' or 'false'. Useful with strength=primary to ignore accents but not case.</description></item>
    ///     <item><description>caseFirst: 'lower' or 'upper'. Useful to control which is sorted first when case is not ignored.</description></item>
    ///     <item><description>numeric: 'true' or 'false'. Digits are sorted according to numeric value, e.g. foobar-9 sorts before foobar-10</description></item>
    /// </list>
    /// </remarks>
    /// <seealso cref="Collator"/>
    /// <seealso cref="RuleBasedCollator"/>
    [Obsolete("Use ICUCollationKeyAnalyzer instead.")]
    [ExceptionToClassNameConvention]
    public class ICUCollationKeyFilterFactory : TokenFilterFactory, IMultiTermAwareComponent, IResourceLoaderAware
    {
        private Collator collator;
        private readonly string custom;
        private readonly string localeID;
        private readonly string strength;
        private readonly string decomposition;

        private readonly string alternate;
        private readonly string caseLevel;
        private readonly string caseFirst;
        private readonly string numeric;
        private readonly string variableTop;

        public ICUCollationKeyFilterFactory(IDictionary<string, string> args)
            : base(args)
        {
            custom = Get(args, "custom");
            localeID = Get(args, "locale");
            strength = Get(args, "strength");
            decomposition = Get(args, "decomposition");

            alternate = Get(args, "alternate");
            caseLevel = Get(args, "caseLevel");
            caseFirst = Get(args, "caseFirst");
            numeric = Get(args, "numeric");
            variableTop = Get(args, "variableTop");

            if (custom is null && localeID is null)
                throw new ArgumentException("Either custom or locale is required.");

            if (custom != null && localeID != null)
                throw new ArgumentException("Cannot specify both locale and custom. "
                    + "To tailor rules for a built-in language, see the docs for RuleBasedCollator. "
                    + "Then save the entire customized ruleset to a file, and use with the custom parameter");

            if (args.Count > 0)
            {
                throw new ArgumentException(string.Format(J2N.Text.StringFormatter.CurrentCulture, "Unknown parameters: {0}", args));
            }
        }

        public virtual void Inform(IResourceLoader loader)
        {
            if (localeID != null)
            {
                // create from a system collator, based on Locale.
                collator = CreateFromLocale(localeID);
            }
            else
            {
                // create from a custom ruleset
                collator = CreateFromRules(custom, loader);
            }

            // set the strength flag, otherwise it will be the default.
            if (strength != null)
            {
                if (strength.Equals("primary", StringComparison.OrdinalIgnoreCase))
                    collator.Strength = CollationStrength.Primary;
                else if (strength.Equals("secondary", StringComparison.OrdinalIgnoreCase))
                    collator.Strength = CollationStrength.Secondary;
                else if (strength.Equals("tertiary", StringComparison.OrdinalIgnoreCase))
                    collator.Strength = CollationStrength.Tertiary;
                else if (strength.Equals("quaternary", StringComparison.OrdinalIgnoreCase))
                    collator.Strength = CollationStrength.Quaternary;
                else if (strength.Equals("identical", StringComparison.OrdinalIgnoreCase))
                    collator.Strength = CollationStrength.Identical;
                else
                    throw new ArgumentException("Invalid strength: " + strength);
            }

            // set the decomposition flag, otherwise it will be the default.
            if (decomposition != null)
            {
                if (decomposition.Equals("no", StringComparison.OrdinalIgnoreCase))
                    collator.Decomposition = NormalizationMode.NoDecomposition;  // (Collator.NO_DECOMPOSITION);
                else if (decomposition.Equals("canonical", StringComparison.OrdinalIgnoreCase))
                    collator.Decomposition = NormalizationMode.CanonicalDecomposition;     //.setDecomposition(Collator.CANONICAL_DECOMPOSITION);
                else
                    throw new ArgumentException("Invalid decomposition: " + decomposition);
            }

            // expert options: concrete subclasses are always a RuleBasedCollator
            RuleBasedCollator rbc = (RuleBasedCollator)collator;
            if (alternate != null)
            {
                if (alternate.Equals("shifted", StringComparison.OrdinalIgnoreCase))
                {
                    rbc.IsAlternateHandlingShifted = true;
                }
                else if (alternate.Equals("non-ignorable", StringComparison.OrdinalIgnoreCase))
                {
                    rbc.IsAlternateHandlingShifted = false;
                }
                else
                {
                    throw new ArgumentException("Invalid alternate: " + alternate);
                }
            }
            if (caseLevel != null)
            {
                rbc.IsCaseLevel = bool.Parse(caseLevel);
            }
            if (caseFirst != null)
            {
                if (caseFirst.Equals("lower", StringComparison.OrdinalIgnoreCase))
                {
                    rbc.IsLowerCaseFirst = true;
                }
                else if (caseFirst.Equals("upper", StringComparison.OrdinalIgnoreCase))
                {
                    rbc.IsUpperCaseFirst = true;
                }
                else
                {
                    throw new ArgumentException("Invalid caseFirst: " + caseFirst);
                }
            }
            if (numeric != null)
            {
                rbc.IsNumericCollation = bool.Parse(numeric);
            }

            if (variableTop != null)
            {
                rbc.SetVariableTop(variableTop);
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new ICUCollationKeyFilter(input, collator);
        }

        /// <summary>
        /// Create a locale from <paramref name="localeID"/>.
        /// Then return the appropriate collator for the locale.
        /// </summary>
        /// <param name="localeID"></param>
        /// <returns>The appropriate collator for the locale.</returns>
        private Collator CreateFromLocale(string localeID)
        {
            return Collator.GetInstance(new UCultureInfo(localeID));
        }

        /// <summary>
        /// Read custom rules from a file, and create a <see cref="RuleBasedCollator"/>.
        /// The file cannot support comments, as # might be in the rules!
        /// </summary>
        private Collator CreateFromRules(string fileName, IResourceLoader loader)
        {
            Stream input = null;
            try
            {
                input = loader.OpenResource(fileName);
                string rules = ToUTF8String(input);
                return new RuleBasedCollator(rules);
            }
            catch (Exception e) when (e.IsException())
            {
                // io error or invalid rules
                throw RuntimeException.Create(e);
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(input);
            }
        }

        public virtual AbstractAnalysisFactory GetMultiTermComponent()
        {
            return this;
        }

        private static string ToUTF8String(Stream input) // LUCENENET: CA1822: Mark members as static
        {
            StringBuilder sb = new StringBuilder();
            char[] buffer = new char[1024];
            TextReader r = IOUtils.GetDecodingReader(input, Encoding.UTF8);
            int len; // LUCENENET: IDE0059: Remove unnecessary value assignment
            while ((len = r.Read(buffer, 0, buffer.Length)) > 0)
            {
                sb.Append(buffer, 0, len);
            }
            return sb.ToString();
        }
    }
}
