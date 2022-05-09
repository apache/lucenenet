using ICU4N.Text;
using J2N.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using System;
using System.Globalization;
using Console = Lucene.Net.Util.SystemConsole;

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
                //case NewCollationAnalyzerTask.Implementation.JDK:
                //    return typeof(Lucene.Net.Collation.CollationKeyAnalyzer);

                case NewCollationAnalyzerTask.Implementation.ICU:
                    return typeof(Lucene.Net.Collation.ICUCollationKeyAnalyzer);
                default:
                    return typeof(Lucene.Net.Collation.ICUCollationKeyAnalyzer);
            }
        }

        public static Type GetCollatorType(this NewCollationAnalyzerTask.Implementation impl)
        {
            switch (impl)
            {
                //case NewCollationAnalyzerTask.Implementation.JDK:
                //    return typeof(Icu.Collation.Collator);

                case NewCollationAnalyzerTask.Implementation.ICU:
                    return typeof(Collator);
                default:
                    return typeof(Collator);
            }
        }
    }

    public class NewCollationAnalyzerTask : PerfTask
    {
        /// <summary>
        /// Different Collation implementations: currently 
        /// limited to what is provided in ICU.
        /// <para/>
        /// See <a href="http://site.icu-project.org/charts/collation-icu4j-sun">Comparison of implementations</a>
        /// </summary>
        public enum Implementation
        {
            //JDK, // LUCENENET: Not supported
            ICU
        }

        private Implementation impl = Implementation.ICU; //Implementation.JDK;

        public NewCollationAnalyzerTask(PerfRunData runData)
            : base(runData)
        {
        }

        internal static Analyzer CreateAnalyzer(CultureInfo locale, Implementation impl)
        {
            // LUCENENET specific - senseless to use reflection here because we only have one
            // collator.
            object collator = Collator.GetInstance(locale);

            // LUCENENET TODO: The .NET equivalent to create a collator like the one in the JDK is:
            //CompareInfo.GetCompareInfo(locale.Name);

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
                    //else if (value.Equals("jdk", StringComparison.OrdinalIgnoreCase))
                    //    impl = Implementation.JDK;
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
