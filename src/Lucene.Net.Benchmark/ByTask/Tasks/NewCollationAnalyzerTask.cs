using ICU4N.Text;
using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using System;
using System.Globalization;

namespace Lucene.Net.Benchmarks.ByTask.Tasks
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
    /// LUCENENET specific extension methods for the <see cref="NewCollationAnalyzerTask.Implementation"/> enumeration.
    /// </summary>
    public static class ImplementationExtensions
    {
        public static Type GetAnalyzerType(this NewCollationAnalyzerTask.Implementation impl)
        {
            switch (impl)
            {
                case NewCollationAnalyzerTask.Implementation.DotNet:
                    return typeof(Lucene.Net.Collation.CollationKeyAnalyzer);

                case NewCollationAnalyzerTask.Implementation.ICU:
                default:
                    return typeof(Lucene.Net.Collation.ICUCollationKeyAnalyzer);
            }
        }

        public static Type GetCollatorType(this NewCollationAnalyzerTask.Implementation impl)
        {
            switch (impl)
            {
                case NewCollationAnalyzerTask.Implementation.DotNet:
                    // LUCENENET: The .NET equivalent of the JDK's java.text.Collator is the
                    // platform collator, System.Globalization.CompareInfo.
                    return typeof(CompareInfo);

                case NewCollationAnalyzerTask.Implementation.ICU:
                default:
                    return typeof(Collator);
            }
        }
    }

    public class NewCollationAnalyzerTask : PerfTask
    {
        /// <summary>
        /// Different Collation implementations: the .NET platform collator
        /// (<see cref="CompareInfo"/>, the equivalent of the JDK's collator) and ICU.
        /// <para/>
        /// See <a href="http://site.icu-project.org/charts/collation-icu4j-sun">Comparison of implementations</a>
        /// </summary>
        public enum Implementation
        {
            // LUCENENET: This value is named JDK in upstream Lucene (the collator comes from the
            // Java Development Kit's java.text.Collator). It has been renamed to DotNet here because the
            // equivalent in .NET is the platform collator (System.Globalization.CompareInfo). The "jdk"
            // (and legacy "bcl") parameter values are still accepted as aliases.

            /// <summary>The .NET platform collator (<see cref="CompareInfo"/>), equivalent to the JDK's <c>java.text.Collator</c>.</summary>
            DotNet,
            ICU
        }

        private Implementation impl = Implementation.DotNet;

        public NewCollationAnalyzerTask(PerfRunData runData)
            : base(runData)
        {
        }

        internal static Analyzer CreateAnalyzer(CultureInfo locale, Implementation impl)
        {
            // LUCENENET specific - senseless to use reflection here, so we construct the collator
            // for the chosen implementation directly. The DotNet implementation (named JDK in upstream
            // Lucene) maps to the .NET platform collator (System.Globalization.CompareInfo); ICU maps
            // to ICU4N's Collator.
            object collator = impl == Implementation.ICU
                ? (object)Collator.GetInstance(locale)
                : CompareInfo.GetCompareInfo(locale.Name);

            Type clazz = impl.GetAnalyzerType();
            return (Analyzer)Activator.CreateInstance(clazz,
#pragma warning disable 612, 618
                LuceneVersion.LUCENE_CURRENT,
#pragma warning restore 612, 618
                collator);
        }

        public override int DoLogic()
        {
            try
            {
                CultureInfo locale = RunData.Locale;
                if (locale is null) throw RuntimeException.Create(
                    "Locale must be set with the NewLocale task!");
                Analyzer analyzer = CreateAnalyzer(locale, impl);
                RunData.Analyzer = analyzer;
                Console.WriteLine("Changed Analyzer to: "
                    + analyzer.GetType().Name + "(" + locale + ")");
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create("Error creating Analyzer: impl=" + impl, e);
            }
            return 1;
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);

            StringTokenizer st = new StringTokenizer(@params, ",");
            while (st.MoveNext())
            {
                string param = st.Current;
                StringTokenizer expr = new StringTokenizer(param, ":");
                string key = expr.MoveNext() ? expr.Current : string.Empty;
                string value = expr.MoveNext() ? expr.Current : string.Empty;
                // for now we only support the "impl" parameter.
                // TODO: add strength, decomposition, etc
                if (key.Equals("impl", StringComparison.Ordinal))
                {
                    if (value.Equals("icu", StringComparison.OrdinalIgnoreCase))
                        impl = Implementation.ICU;
                    // LUCENENET: "dotnet" maps to what upstream Lucene calls "jdk"; we accept "jdk" (and
                    // the legacy "bcl") as aliases so existing Lucene benchmark .alg files keep working.
                    else if (value.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
                        || value.Equals("jdk", StringComparison.OrdinalIgnoreCase)
                        || value.Equals("bcl", StringComparison.OrdinalIgnoreCase))
                        impl = Implementation.DotNet;
                    else
                        throw RuntimeException.Create("Unknown parameter " + param);
                }
                else
                {
                    throw RuntimeException.Create("Unknown parameter " + param);
                }
            }
        }

        public override bool SupportsParams => true;
    }
}
