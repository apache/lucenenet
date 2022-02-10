using J2N.Text;
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
    /// Set a <see cref="CultureInfo"/> for use in benchmarking.
    /// </summary>
    /// <remarks>
    /// Locales can be specified in the following ways:
    /// <list type="bullet">
    ///     <item><description><c>de</c>: Language "de"</description></item>
    ///     <item><description><code>en,US</code>: Language "en", country "US"</description></item>
    ///     <item><description><code>nb-NO</code>: Language "nb" (Bokmål), country "NO"</description></item>
    ///     <item><description><code>ROOT</code>: The <see cref="CultureInfo.InvariantCulture"/></description></item>
    /// </list>
    /// </remarks>
    public class NewLocaleTask : PerfTask
    {
        private string culture;
        //private string language;
        //private string country;
        //private string variant;

        /// <summary>
        /// Create a new <see cref="CultureInfo"/> and set it it in the RunData for
        /// use by all future tasks.
        /// </summary>
        /// <param name="runData"></param>
        public NewLocaleTask(PerfRunData runData)
            : base(runData)
        {
        }

        internal static CultureInfo CreateLocale(string culture /*String language, String country, String variant*/)
        {
            if (culture is null || culture.Length == 0)
                return null;

            string lang = culture;
            if (lang.Equals("ROOT", StringComparison.OrdinalIgnoreCase))
                return CultureInfo.InvariantCulture; // Default culture
                                                     //lang = ""; // empty language is the root locale in the JDK

            return new CultureInfo(lang);
        }

        public override int DoLogic()
        {
            CultureInfo locale = CreateLocale(culture /*language, country, variant*/);
            RunData.Locale = locale;
            Console.WriteLine("Changed Locale to: " +
                (locale is null ? "null" :
                (locale.EnglishName.Length == 0) ? "root locale" : locale.ToString()));
            return 1;
        }

        public override void SetParams(string @params)
        {
            base.SetParams(@params);
            //language = country = variant = "";
            culture = "";
            string _;
            StringTokenizer st = new StringTokenizer(@params, ",");
            if (st.MoveNext())
                //language = st.nextToken();
                culture = st.Current;
            if (st.MoveNext())
                culture += "-" + st.Current;
            if (st.MoveNext())
                _ = st.Current;
        }

        public override bool SupportsParams => true;
    }
}
